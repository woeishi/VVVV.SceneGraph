using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;

namespace SceneGraph.Core
{
    public class Scene<T, U, V, W> : IScene, IDisposable
        where V : IDisposable
        where W : IDisposable
    {
        internal T Source { get; set; }

        public string Filename { get; }
        public string AssetRoot { get; set; }
        public GraphNode Root { get; protected set; }

        public XElement XmlRoot { get; private set; }

        public MeshInfo[] MeshInfos { get; protected set; }
        public MaterialInfo[] MaterialInfos { get; protected set; }
        public TextureInfo[] TextureInfos { get; protected set; }
        ResourceManager<U, V, W> ResourceManager { get; set; }

        public Scene(string path)
        {
            Filename = path;
            AssetRoot = Path.GetDirectoryName(Filename);
        }

        public Scene(string path, string assetPath)
        {
            Filename = path;
            AssetRoot = string.IsNullOrWhiteSpace(assetPath)?Path.GetDirectoryName(Filename):assetPath;
        }

        protected void InitializeResources(Func<int, U, V> meshCreate, Func<string, U, W> textureCreate)
        {
            var texHash = new Dictionary<TextureInfo,TextureInfo>();
            for (int i = 0; i < MaterialInfos.Length; i++)
            {
                for (int j = 0; j < MaterialInfos[i].Textures.Length; j++)
                {

                    if (texHash.ContainsKey(MaterialInfos[i].Textures[j]))
                        MaterialInfos[i].Textures[j] = texHash[MaterialInfos[i].Textures[j]];
                    else
                        texHash.Add(MaterialInfos[i].Textures[j], MaterialInfos[i].Textures[j]);
                }
            }
            TextureInfos = texHash.Values.Select((t, index) => { t.Index = index; return t; } ).ToArray();

            ResourceManager = new ResourceManager<U, V, W>(MeshInfos, TextureInfos);
            Func<int, U, W> ttc = (i, ctx) => {
                var texPath = TextureInfos[i].Path;
                if (!Path.IsPathRooted(texPath))
                    texPath = Path.Combine(AssetRoot, texPath);
                return textureCreate(texPath, ctx);
            };

            ResourceManager.Initialize(meshCreate, ttc);
        }

        public void Dispose()
        {
            ResourceManager.Dispose();
            (Source as IDisposable)?.Dispose();
        }

        protected virtual void CreateGraph()
        {
            XmlRoot = Root.ToXElement();
        }

        internal V GetGeometry(GraphNode node, U context, string nodePath)
        {
            var me = (node.Element as MeshElement);
            return ResourceManager.GetGeometry(me.MeshID, context, nodePath);
        }

        internal void ReleaseGeometry(GraphNode node, U context, string nodePath)
        {
            var me = (node.Element as MeshElement);
            ResourceManager.ReleaseGeometry(me.MeshID, nodePath, context);
        }

        internal void PurgeGeometry(GraphNode node)
        {
            var me = (node.Element as MeshElement);
            ResourceManager.PurgeGeometry(me.MeshID);
        }

        internal W GetTexture(TextureInfo textureInfo, string nodePath, U context)
        {
            if (textureInfo != null)
                return ResourceManager.GetTexture(textureInfo.Index, nodePath, context);
            else
                return default(W);
        }

        internal void ReleaseTexture(GraphNode node, TextureInfo textureInfo, string nodePath, U context)
        {
            var me = (node.Element as MeshElement);
            if (textureInfo == null)
                foreach (var t in me.Material.Textures)
                    ResourceManager.ReleaseTexture(t.Index, nodePath, context);
            else 
                ResourceManager.ReleaseTexture(textureInfo.Index, nodePath, context);
        }

        internal void PurgeTextures(GraphNode node)
        {
            var me = (node.Element as MeshElement);
            foreach (var t in me.Material.Textures)
                ResourceManager.PurgeTextures(t.Index);
        }
    }
}
