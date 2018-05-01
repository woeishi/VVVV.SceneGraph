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
        public string Name { get; }

        internal Element(Scene scene, AssimpNode self, int id)
        {
            Node = self;
            ID = id;

            //fixes c4d -> fbx
            var bytes = Encoding.Default.GetBytes(self.Name);
            Name = Encoding.UTF8.GetString(bytes);
        }
    }
}
