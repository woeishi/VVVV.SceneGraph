using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using SceneGraph.Core.Animations;

namespace SceneGraph.Core
{
    public class Scene<T> : IScene
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

        public Animation[] Animations { get; protected set; }

        public Scene(string path)
        {
            Filename = path;
            AssetRoot = Path.GetDirectoryName(Filename);
            ResourceManager = new List<IResourceManager>();
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

        protected void InitializeAnimations(int animationCount, Func<int, Animation> animationCreate)
        {
            Animations = new Animation[animationCount];
            for (int i = 0; i < animationCount; i++)
                Animations[i] = animationCreate(i);
        }

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

        public ResourceToken GetGeometry(GraphNode node, dynamic context, out dynamic geometry)
        {
            var rm = ResourceManager.Where(m => m.KeyType.IsAssignableFrom(context.GetType())).First();
            var me = (node.Element as MeshElement);
            return rm.GetGeometry(me.MeshID, context, out geometry);
        }

        public ResourceToken GetTexture(TextureInfo textureInfo, dynamic context, out dynamic texture)
        {
            var rm = ResourceManager.Where(m => m.KeyType.IsAssignableFrom(context.GetType())).First();
            if (textureInfo != null)
                return rm.GetTexture(textureInfo.Index, context, out texture);
            else
                return rm.GetDefaultTexture(context, out texture);
        }

        public void PurgeResources()
        {
            foreach (var rm in ResourceManager)
                rm.Purge();
        }
    }
}
