using System;
using System.IO;
using System.Xml.Linq;

namespace SceneGraph.Core
{
    public class Scene<T, U, V, W> : IScene, IDisposable
        where U : class
        where V : IDisposable
        where W : IDisposable
    {
        internal T Source { get; set; }

        public string Filename { get; }
        public string AssetRoot { get; set; }
        public GraphNode Root { get; protected set; }

        public XElement XmlRoot { get; private set; }

        public MeshInfo[] MeshInfos { get; protected set; }
        internal ResourceHandler<U, V>[] MeshHandlers { get; set; }
        public MaterialInfo[] MaterialInfos { get; protected set; }
        internal ResourceHandler<U, W>[][] TextureHandlers { get; set; }

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

        public void Dispose()
        {
            foreach (var mh in MeshHandlers)
                mh.Dispose();
            foreach (var tha in TextureHandlers)
                foreach (var th in tha)
                    th.Dispose();
            (Source as IDisposable)?.Dispose();
        }

        protected virtual void CreateGraph()
        {
            XmlRoot = Root.ToXElement();
        }
    }
}
