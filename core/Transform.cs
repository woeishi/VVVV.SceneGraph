using System;
using SlimDX;

namespace SceneGraph.Core
{
    internal class Transform
    {
        internal GraphNode Owner { get; }
        internal Matrix Matrix { get; set; }
        internal bool IsDirty { get; set; }

        internal event EventHandler Changed;
        internal void MarkChanged() => Changed?.Invoke(Owner, EventArgs.Empty);

        internal Transform(Matrix local, GraphNode creator)
        {
            Owner = creator;
            Matrix = local;
            IsDirty = true;
        }

        internal Transform(Transform other, GraphNode creator)
        {
            Owner = creator;
            Matrix = other.Matrix;
            IsDirty = other.IsDirty;
        }
    }
}
