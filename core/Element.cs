using System;
using System.Text;

using AssimpNet;
using SlimDX;

namespace SceneGraph.Core
{
    public class Element
    {
        internal AssimpNode Node { get; }
        public int ID { get; }
        public virtual string Type => "Container";
        public virtual string Name { get; internal set; }
        public virtual int ChildCount { get; internal set; }

        public virtual Matrix Local { get; internal set; }
        public virtual Matrix Accumulated { get; internal set; }

        internal Element(Scene scene, AssimpNode self, int id)
        {
            Node = self;
            ID = id;

            //fixes c4d -> fbx
            var bytes = Encoding.Default.GetBytes(self.Name);
            Name = Encoding.UTF8.GetString(bytes);

            ChildCount = self.Children.Count + self.MeshCount;

            Local = self.LocalTransform;
            Accumulated = self.RelativeTransform;
        }
    }
}
