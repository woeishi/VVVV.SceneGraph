using System;
using System.Collections.Generic;

using SlimDX;

namespace SceneGraph.Core
{
    public class GraphNode
    {
        public Scene Scene { get; internal set; }
        public Element Element { get; private set; }
        public GraphNode Parent { get; private set; }
        public GraphNode[] Children { get; internal set; }
        internal int LastDescendantID { get; set; }

        public int ID => Element.ID;
        public string Name => Element.Name;

        List<Transform> Transforms;

        //only in use when initally parsing the scene
        internal GraphNode(Element element, GraphNode parent = null, Scene scene = null)
        {
            Scene = scene;
            Element = element;
            Parent = parent;
            Children = new GraphNode[Element.Node.Children.Count];

            Transforms = new List<Transform>();
            Transforms.Add(new Transform(Element.Node.LocalTransform, Element.Node.LocalTransform, this));

            LocalCache = Element.Node.LocalTransform;
        }

        public static GraphNode CloneGraph(GraphNode original)
        {
            var graph = original.Root();
            graph = RecurseCloneGraph(graph, null);
            
            return graph.GetByID(original.ID) ?? graph;
        }
        static GraphNode RecurseCloneGraph(GraphNode original, GraphNode parent)
        {
            var n = new GraphNode(original, parent);
            for (int i = 0; i < n.Children.Length; i++)
                n.Children[i] = RecurseCloneGraph(original.Children[i], n);
            return n;
        }
        GraphNode(GraphNode other, GraphNode parent = null)
        {
            Scene = other.Scene;
            Element = other.Element;
            Parent = parent ?? other.Parent;
            Children = new GraphNode[other.Children.Length];
            LastDescendantID = other.LastDescendantID;

            Transforms = other.Transforms;
            LocalCache = other.LocalCache;
        }

        public void Fork()
        {
            var tmp = Transforms;
            Transforms = new List<Transform>();
            Transforms.Add(new Transform(tmp[0], this));
            Transforms.AddRange(tmp);
        }
        public GraphNode Merge(GraphNode part)
        {
            var pointer = this.ID;
            var whole = this.GetByID(part.ID);
            MergeTransforms(whole, part);

            return whole;
        }
        void MergeTransforms(GraphNode left, GraphNode right)
        {
            var li = left.Transforms.Count - 1;
            var ri = right.Transforms.Count - 1;
            while (li>=0 && ri>=0)
            {
                if (left.Transforms[li].Local != right.Transforms[ri].Local)
                    break;
                li--;
                ri--;
            }

            var tmp = left.Transforms;
            left.Transforms = new List<Transform>(right.Transforms.GetRange(0, ri + 1));
            left.Transforms.AddRange(tmp);

            for (int i = 0; i < left.Children.Length; i++)
                MergeTransforms(left.Children[i], right.Children[i]);
        }

        Matrix LocalCache = Matrix.Identity;
        public Matrix Local
        {
            get { return LocalCache; }
        }
        public Matrix Accumulated
        {
            get { return Transforms[0].Accumulated; }
            private set { Transforms[0].Accumulated = value; }
        }

        public void Transform(Matrix transient)
        {
            //TO-DO graph is dirty mechanism
            //LocalTransient = transient;
            Transforms[0].Local = transient;
            UpdateTransformGraph();
        }

        public void UpdateTransformGraph()
        {
            LocalCache = Transforms[0].Local;
            for (int i = 1; i < Transforms.Count; i++)
                LocalCache *= Transforms[i].Local;

            if (Parent != null)
                Accumulated = LocalCache * Parent.Accumulated;
            else
                Accumulated = LocalCache;

            foreach (var c in this.Children)
                c.UpdateTransformGraph();
        }
    }
}
