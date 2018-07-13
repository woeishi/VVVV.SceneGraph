using System;

namespace VVVV.SceneGraph
{
    internal interface INestedNode
    {
        bool GraphChanged { get; set; }

        void SafeDispose();
    }
}
