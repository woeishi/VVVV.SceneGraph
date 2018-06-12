using System;

namespace SceneGraph.Core
{
    public class Element
    {
        public int ID { get; }
        public virtual string Type => "Container";
        public virtual string Name { get; internal set; }
        public virtual int ChildCount { get; internal set; }

        internal Element(int id, string name, int childCount)
        {
            ID = id;
            Name = name;
            ChildCount = childCount;
        }
    }
}
