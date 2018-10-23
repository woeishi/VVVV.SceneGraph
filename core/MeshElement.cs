
namespace SceneGraph.Core
{
    public class MeshElement : Element
    {
        public override string Type => "Mesh";
        public int MeshID => Mesh.Index;
        public MeshInfo Mesh { get; }

        public int MaterialID => Material.Index;
        internal MaterialInfo Material { get; }

        internal MeshElement(int id, IScene scene, MeshInfo meshInfo, MaterialInfo materialInfo) : base(id, $"Mesh_{meshInfo.Index}", 0)
        {
            Mesh = meshInfo;
            Material = materialInfo;
        }
    }
}
