using System.Collections.Generic;
using SlimDX;

namespace SceneGraph.Core
{
    public class MeshInfo
    {
        public int Index { get; internal set; }
        public List<Matrix> BoneMatrices { get; internal set; }
        public List<string> BoneNames { get; internal set; }
        public BoundingBox BoundingBox { get; internal set; }
        public int ColorChannelCount { get; internal set; }
        public bool HasNormals { get; internal set; }
        public int MaterialIndex { get; internal set; }
        public int MaxBonePerVertex { get; internal set; }
        public int UvChannelCount { get; internal set; }
        public int VerticesCount { get; internal set; }
    }
}
