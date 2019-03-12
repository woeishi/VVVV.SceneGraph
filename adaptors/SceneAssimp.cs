using System.Linq;
using SceneGraph.Core;
using SceneGraph.Core.Animations;
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

            InitializeAnimations(Source.Animations.Count, i => Source.Animations[i].ToAnimation());

            CreateGraph();
        }

        protected override void CreateGraph()
        {
            int counter = 0;
            var local = Source.RootNode.LocalTransform.Validate();
            var element = Source.RootNode.ToElement(0);
            var animationInfo = LoadAnimationInfo(Source.RootNode.Name);
            Root = new GraphNode(element, Source.RootNode.RelativeTransform, local, animationInfo, null, this);
            TraverseGraph(Root, Source.RootNode, ref counter);
            Root.LastDescendantID = counter;

            base.CreateGraph();
        }

        AnimationInfo LoadAnimationInfo(string key)
        {
            var animations = Source.Animations
                .Select((a, i) => new Animation(Animations[i], a.Channels
                    .Select((c, j) => new { c, j })
                    .Where(e => e.c.Name == key)
                    .SelectMany(e => new[] { Animations[i].Channels[e.j * 3], Animations[i].Channels[e.j * 3 + 1], Animations[i].Channels[e.j * 3 + 2] })))
                .Where(a => a.Channels.Length > 0);
            return AnimationInfo.Create(animations.ToArray());
            
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
                    n.Children[i] = new GraphNode(an.ToMesh(counter, this, an.MeshIndices[i]), an.RelativeTransform, null, n, this);
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

                    var animationInfo = LoadAnimationInfo(an.Children[id].Name);
                    n.Children[i] = new GraphNode(element, an.Children[id].RelativeTransform, local, animationInfo, n, this);
                    TraverseGraph(n.Children[i], an.Children[id], ref counter);
                    n.Children[i].LastDescendantID = counter;
                }
            }
        }
    }
}
