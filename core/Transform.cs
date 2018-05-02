using SlimDX;

namespace SceneGraph.Core
{
    internal class Transform
    {
        internal GraphNode Owner { get; }
        internal Matrix LocalSpace { get; set; }

        internal Transform(Matrix local, GraphNode creator)
        {
            Owner = creator;
            LocalSpace = local;
        }

        internal Transform(Transform other, GraphNode creator)
        {
            Owner = creator;
            LocalSpace = other.LocalSpace;
        }
    }
}
