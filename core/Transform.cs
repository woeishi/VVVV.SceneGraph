using SlimDX;

namespace SceneGraph.Core
{
    internal class Transform
    {
        internal GraphNode Owner { get; }
        internal Matrix Local { get; set; }
        internal Matrix Accumulated { get; set; }

        internal Transform(Matrix local, Matrix accumulated, GraphNode creator)
        {
            Owner = creator;
            Local = local;
            Accumulated = accumulated;
        }

        internal Transform(Transform other, GraphNode creator)
        {
            Owner = creator;
            Local = other.Local;
            Accumulated = other.Accumulated;
        }
    }
}
