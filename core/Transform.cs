using SlimDX;

namespace SceneGraph.Core
{
    internal class Transform
    {
        internal GraphNode Owner { get; }
        internal Matrix Matrix { get; set; }

        internal Transform(Matrix local, GraphNode creator)
        {
            Owner = creator;
            Matrix = local;
        }

        internal Transform(Transform other, GraphNode creator)
        {
            Owner = creator;
            Matrix = other.Matrix;
        }
    }
}
