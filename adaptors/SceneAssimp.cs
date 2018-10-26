using System.Linq;
using SceneGraph.Core;
using SceneGraph.DX11;
using SceneGraph.EX9;
using AssimpNet;

namespace SceneGraph.Adaptors
{
    public class SceneAssimp : Scene<AssimpScene>
    {
        public SceneAssimp(string path) : base(path)
        {
            Source = new AssimpScene();
        }

        public SceneAssimp(string path, string assetPath) : base(path, assetPath)
        {
            Source = new AssimpScene(path, true, false);

            InitializeInfos(Source.Meshes.Count, (i) => Source.Meshes[i].ToMeshInfo(i), Source.Materials.Count, (i) => Source.Materials[i].ToMaterialInfo(i));

            var dx11 = new DX11ResourceManager(
                MeshInfos.Length, 
                (i, ctx) => DX11Utils.LoadMesh(ctx,
                                                Source.Meshes[i].VerticesCount,
                                                Source.Meshes[i].Vertices,
                                                Source.Meshes[i].Indices.ToArray(),
                                                Source.Meshes[i].GetInputElements().ToArray(),
                                                Source.Meshes[i].CalculateVertexSize(),
                                                Source.Meshes[i].BoundingBox), 
                TextureInfos.Length, 
                (i, ctx) => DX11Utils.CreateTexture(ctx, TextureInfos[i].FullPath));
            AddResourceManager(dx11);

            var ex9 = new EX9ResourceManager(
                MeshInfos.Length,
                (i, device) => EX9Utils.LoadMesh(device,
                                                Source.Meshes[i].VerticesCount,
                                                Source.Meshes[i].Vertices,
                                                Source.Meshes[i].Indices.ToArray(),
                                                Source.Meshes[i].GetInputElements()),
                TextureInfos.Length,
                (i, device) => EX9Utils.CreateTexture(device, TextureInfos[i].FullPath));
            AddResourceManager(ex9);

            CreateGraph();
        }

        protected override void CreateGraph()
        {
            int counter = 0;
            var local = Source.RootNode.LocalTransform.Validate();
            var tracks = Source.Animations.SelectMany(stack => stack.Channels.Where(c => c.Name == Source.RootNode.Name).Select(c => c.ToTrack(stack))).ToList();
            Root = new GraphNode(Source.RootNode.ToElement(0), Source.RootNode.RelativeTransform, local, tracks, null, this);
            TraverseGraph(Root, Source.RootNode, ref counter);
            Root.LastDescendantID = counter;

            base.CreateGraph();
        }

        void TraverseGraph(GraphNode n, AssimpNode an, ref int counter)
        {
            int id = 0;
            for (int i = 0; i < n.Children.Length; i++)
            {
                counter++;

                id = i-an.MeshCount; //assimp child id and graphnode child id differ with meshcontainers
                if (id < 0) //attach meshes as children before further nodes
                {
                    n.Children[i] = new GraphNode(an.ToMesh(counter, this, an.MeshIndices[i]), an.RelativeTransform, n, this);
                    n.Children[i].LastDescendantID = counter;
                }
                else //prepended meshes in children list, now process other nodes
                {
                    Element element;
                    var cam = Source.Cameras.Where(c => c.Name == an.Children[id].Name).FirstOrDefault();
                    if (cam != null)
                        element = an.Children[id].ToCamera(counter, cam);
                    else
                        element = an.Children[id].ToElement(counter);

                    var local = an.Children[id].LocalTransform.Validate();
                    var tracks = Source.Animations.SelectMany(stack => stack.Channels.Where(c => c.Name == an.Children[id].Name).Select(c => c.ToTrack(stack))).ToList();
                    n.Children[i] = new GraphNode(element, an.Children[id].RelativeTransform, local, tracks, n, this);
                    TraverseGraph(n.Children[i], an.Children[id], ref counter);
                    n.Children[i].LastDescendantID = counter;
                }
            }
        }
    }
}
