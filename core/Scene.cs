using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SceneGraph.Core
{
    public class Scene<T> : IScene, IDisposable
    {
        internal T Source { get; set; }

        public string Filename { get; }
        public string AssetRoot { get; set; }
        public GraphNode Root { get; protected set; }

        public XElement XmlRoot { get; private set; }

        public MeshInfo[] MeshInfos { get; protected set; }
        public MaterialInfo[] MaterialInfos { get; protected set; }
        public TextureInfo[] TextureInfos { get; protected set; }
        List<IResourceManager> ResourceManager { get; set; }

        public Scene(string path)
        {
            Filename = path;
            AssetRoot = Path.GetDirectoryName(Filename);
        }

        public Scene(string path, string assetPath)
        {
            Filename = path;
            AssetRoot = string.IsNullOrWhiteSpace(assetPath)?Path.GetDirectoryName(Filename):assetPath;
            ResourceManager = new List<IResourceManager>();
        }

        protected void InitializeInfos(int meshCount, Func<int, MeshInfo> meshInfoCreate, int materialCount, Func<int,MaterialInfo> materialInfoCreate)
        {
            MeshInfos = new MeshInfo[meshCount];
            for (int i = 0; i < meshCount; i++)
            {
                var id = i;
                MeshInfos[i] = meshInfoCreate(id);
            }

            MaterialInfos = new MaterialInfo[materialCount];
            var texHash = new Dictionary<TextureInfo, TextureInfo>();
            for (int i = 0; i < materialCount; i++)
            {
                var id = i;
                MaterialInfos[i] = materialInfoCreate(id);
                for (int j = 0; j < MaterialInfos[i].Textures.Length; j++)
                {
                    if (texHash.ContainsKey(MaterialInfos[i].Textures[j]))
                        MaterialInfos[i].Textures[j] = texHash[MaterialInfos[i].Textures[j]];
                    else
                        texHash.Add(MaterialInfos[i].Textures[j], MaterialInfos[i].Textures[j]);
                }
            }

            TextureInfos = texHash.Values
                .Select((t, index) => 
                {
                    t.Index = index;
                    t.FullPath = Path.IsPathRooted(t.Path) ? t.Path : Path.Combine(AssetRoot, t.Path);
                    return t;
                })
                .ToArray();
        }

        internal void AddResourceManager(IResourceManager mgrs) => ResourceManager.Add(mgrs);

        protected virtual void CreateGraph()
        {
            XmlRoot = Root.ToXElement();
        }

        public void Dispose()
        {
            foreach (var rm in ResourceManager)
                rm.Dispose();
            (Source as IDisposable)?.Dispose();
        }

        public dynamic GetGeometry(GraphNode node, string nodePath, dynamic context)
        {
            var rm = ResourceManager.Where(m => m.KeyType == context.GetType()).First();
            var me = (node.Element as MeshElement);
            return rm.GetGeometry(me.MeshID, nodePath, context);
        }

        public void ReleaseGeometry(GraphNode node, string nodePath, dynamic context)
        {
            IEnumerable<IResourceManager> mgrs = ResourceManager;
            if (context != null)
                mgrs = ResourceManager.Where(m => m.KeyType == context.GetType());
            var me = (node.Element as MeshElement);
            foreach (var rm in mgrs)
                rm.ReleaseGeometry(me.MeshID, nodePath, context);
        }

        public void PurgeGeometry(GraphNode node)
        {
            var me = (node.Element as MeshElement);
            foreach (var rm in ResourceManager)
                rm.PurgeGeometry(me.MeshID);
        }

        public dynamic GetTexture(TextureInfo textureInfo, string nodePath, dynamic context)
        {
            var rm = ResourceManager.Where(m => m.KeyType == context.GetType()).First();
            if (textureInfo != null)
                return rm.GetTexture(textureInfo.Index, nodePath, context);
            else
                return null;
        }

        public void ReleaseTexture(GraphNode node, TextureInfo textureInfo, string nodePath, dynamic context)
        {
            IEnumerable<IResourceManager> mgrs = ResourceManager;
            if (context != null)
                mgrs = ResourceManager.Where(m => m.KeyType == context.GetType());

            var me = (node.Element as MeshElement);
            if (textureInfo == null)
                foreach (var t in me.Material.Textures)
                    foreach (var rm in mgrs)
                        rm.ReleaseTexture(t.Index, nodePath, context);
            else
                foreach (var rm in mgrs)
                    rm.ReleaseTexture(textureInfo.Index, nodePath, context);
        }

        public void PurgeTextures(GraphNode node)
        {
            var me = (node.Element as MeshElement);
            foreach (var t in me.Material.Textures)
                foreach (var rm in ResourceManager)
                    rm.PurgeTextures(t.Index);
        }
    }
}
