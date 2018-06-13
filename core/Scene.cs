using System;
using System.IO;
using System.Xml.Linq;

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
        internal ResourceManager<U, V, W> ResourceManager { get; private set; }

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
            ResourceManager = new ResourceManager<U, V, W>(MeshInfos, MaterialInfos);

            Func<int, int, U, W> tc = (i, j, ctx) => {
                var texPath = MaterialInfos[i].Textures[j].Path;
                if (!Path.IsPathRooted(texPath))
                    texPath = Path.Combine(AssetRoot, texPath);
                return textureCreate(texPath, ctx);
            };
            ResourceManager.Initialize(meshCreate, tc);
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

        internal W GetTexture(GraphNode node, U context, int textureSlot, string nodePath)
        {
            var me = (node.Element as MeshElement);
            return ResourceManager.GetTexture(me.MaterialID, textureSlot, context, nodePath);
        }

        internal void ReleaseTexture(GraphNode node, U context, string nodePath, int textureSlot = -1)
        {
            var me = (node.Element as MeshElement);
            ResourceManager.ReleaseTexture(me.MaterialID, textureSlot, context, nodePath);
        }

        internal void PurgeTextures(GraphNode node)
        {
            var me = (node.Element as MeshElement);
            ResourceManager.PurgeTextures(me.MaterialID);
        }
    }
}
