using System.IO;
using System.Linq;

using AssimpNet;
using FeralTic.DX11.Resources;
using SceneGraph.DX11;

namespace SceneGraph.Core
{
    public class Scene : AssimpScene
    {
        public string Filename { get; }
        public string AssetRoot { get; set; }
        public GraphNode Root { get; private set; }

        internal DX11ResourceHandler<DX11IndexedGeometry>[] MeshHandlers { get; }
        internal DX11ResourceHandler<DX11Texture2D>[][] TextureHandlers { get; }

        public Scene(string path) : base()
        {
            Filename = path;
            AssetRoot = Path.GetDirectoryName(Filename);
            Root = null;// new GraphNode(new Element(null, null, 0), null);

            MeshHandlers = new DX11ResourceHandler<DX11IndexedGeometry>[0];
            TextureHandlers = null;
        }

        public Scene(string path, string assetPath, bool preloadmesh, bool threadedload)
            :base(path, preloadmesh, threadedload)
        {
            Filename = path;
            AssetRoot = string.IsNullOrWhiteSpace(assetPath)?Path.GetDirectoryName(Filename):assetPath;

            Root = new GraphNode(new Element(null, base.RootNode, 0), null);
            
            MeshHandlers = new DX11ResourceHandler<DX11IndexedGeometry>[base.Meshes.Count];
            for (int i = 0; i < base.Meshes.Count; i++)
            {
                var mesh = base.Meshes[i];
                MeshHandlers[i] = new DX11ResourceHandler<DX11IndexedGeometry>((ctx) => { return DX11Utils.LoadMesh(ctx, mesh); });
            }

            TextureHandlers = new DX11ResourceHandler<DX11Texture2D>[base.Materials.Count][];
            for (int i = 0; i < base.Materials.Count; i++)
            {
                var mat = base.Materials[i];
                TextureHandlers[i] = new DX11ResourceHandler<DX11Texture2D>[mat.TexturePath.Count];
                for (int t = 0; t < mat.TexturePath.Count; t++)
                {
                    var texPath = mat.TexturePath[t];
                    if (!Path.IsPathRooted(texPath))
                        texPath = Path.Combine(AssetRoot, texPath);
                    TextureHandlers[i][t] = new DX11ResourceHandler<DX11Texture2D>((ctx) => { return DX11Texture2D.FromFile(ctx, texPath); });
                }
            }
        }

        public new void Dispose()
        {
            foreach (var mh in MeshHandlers)
                mh.Dispose();
            foreach (var tha in TextureHandlers)
                foreach (var th in tha)
                    th.Dispose();
            base.Dispose();
        }

        public void InitGraph()
        {
            int counter = 0;
            //Root.Element.Scene = this;
            Root.Scene = this;
            TraverseGraph(Root, ref counter);
            Root.LastDescendantID = counter;
        }

        void TraverseGraph(GraphNode n, ref int counter)
        {
            for (int i = 0; i < n.Children.Length; i++)
            {
                counter++;
                Element element;
                if (n.Element.Node.Children[i].MeshCount > 0)
                    element = new MeshElement(this, n.Element.Node.Children[i], counter);
                else
                {
                    var cam = this.Cameras.Where(c => c.Name == n.Element.Node.Children[i].Name).FirstOrDefault();
                    if (cam != null)
                        element = new CameraElement(this, n.Element.Node.Children[i], counter, cam);
                    else
                        element = new Element(this, n.Element.Node.Children[i], counter);
                }
                n.Children[i] = new GraphNode(element, n, this);
                TraverseGraph(n.Children[i], ref counter);
                n.Children[i].LastDescendantID = counter;
            }
        }

        public void PurgeMeshes()
        {
            foreach (var mh in MeshHandlers)
                mh.Purge();
        }

        public void PurgeTextures()
        {
            foreach (var tha in TextureHandlers)
                foreach (var th in tha)
                    th.Purge();
        }
    }
}
