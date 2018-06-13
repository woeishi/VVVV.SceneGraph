using System.Linq;
using SceneGraph.Core;
using SceneGraph.DX11;
using AssimpNet;


namespace SceneGraph.Adaptors
{
    public class SceneAssimp : SceneDX11<AssimpScene>
    {
        public SceneAssimp(string path) : base(path)
        {
            Source = new AssimpScene();
        }

        public SceneAssimp(string path, string assetPath) : base(path, assetPath)
        {
            Source = new AssimpScene(path, true, false);

            MeshInfos = new MeshInfo[Source.Meshes.Count];
            for (int i = 0; i < MeshInfos.Length; i++)
                MeshInfos[i] = Source.Meshes[i].ToMeshInfo(i);

            MaterialInfos = new MaterialInfo[Source.Materials.Count];
            for (int i = 0; i < MaterialInfos.Length; i++)
                MaterialInfos[i] = Source.Materials[i].ToMaterialInfo(i);

            InitializeDX11Resources((i, ctx) => {
                var m = Source.Meshes[i];
                return DX11Utils.LoadMesh(ctx, 
                                          m.VerticesCount, 
                                          m.Vertices, 
                                          m.Indices.ToArray(), 
                                          m.GetInputElements().ToArray(), 
                                          m.CalculateVertexSize(), 
                                          m.BoundingBox);
            });
            CreateGraph();
        }

        protected override void CreateGraph()
        {
            int counter = 0;
            Root = new GraphNode(Source.RootNode.ToElement(0), Source.RootNode.RelativeTransform, Source.RootNode.LocalTransform, null, this);
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

                id = i; //assimp child id and graphnode child id differ with meshcontainers
                if (an.MeshCount > 0) //attach meshes as children before further nodes
                {
                    n.Children[i] = new GraphNode(an.ToMesh(counter, this, an.MeshIndices[i]), an.RelativeTransform, n, this);
                    n.Children[i].LastDescendantID = counter;
                    id -= an.MeshCount;
                }

                if (id >= 0) //prepended meshes in children list
                {
                    Element element;
                    var cam = Source.Cameras.Where(c => c.Name == an.Children[id].Name).FirstOrDefault();
                    if (cam != null)
                        element = an.Children[id].ToCamera(counter, cam);
                    else
                        element = an.Children[id].ToElement(counter);

                    n.Children[i] = new GraphNode(element, an.Children[id].RelativeTransform, an.Children[id].LocalTransform, n, this);
                    TraverseGraph(n.Children[i], an.Children[id], ref counter);
                    n.Children[i].LastDescendantID = counter;
                }
            }
        }
    }
}
