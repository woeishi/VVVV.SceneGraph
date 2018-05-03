using System;
using System.Collections.Generic;

using SlimDX;

namespace SceneGraph.Core
{
    public class GraphNode
    {
        public Scene Scene { get; }
        public Element Element { get; }
        public GraphNode Parent { get; private set; }
        public GraphNode[] Children { get; internal set; }
        internal int LastDescendantID { get; set; }

        public int ID => Element.ID;
        public string Name => Element.Name;

        List<Transform> Transforms;
        Transform AccumulatedTransform;
        
        public Matrix Local { get; private set; }
        public Matrix Accumulated => AccumulatedTransform.Matrix;

        //only in use when initally parsing the scene
        internal GraphNode(Element element, GraphNode parent = null, Scene scene = null)
        {
            Scene = scene;
            Element = element;
            Parent = parent;
            Children = new GraphNode[Element.ChildCount];

            Transforms = new List<Transform>();
            Transforms.Add(new Transform(Element.Local, this));
            AccumulatedTransform = new Transform(Element.Accumulated, this);

            Local = Element.Local;
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
            Local = other.Local;
            AccumulatedTransform = other.AccumulatedTransform;
        }

        public void Fork()
        {
            var tmp = Transforms;
            Transforms = new List<Transform>();
            Transforms.Add(new Transform(tmp[0], this));
            Transforms.AddRange(tmp);
            //from the fork onward all nodes have to hold their own accumulated transform
            CloneAccumulated(this);
        }
        void CloneAccumulated(GraphNode node)
        {
            var m = node.Accumulated;
            node.AccumulatedTransform = new Transform(m, node);
            foreach (var c in node.Children)
                CloneAccumulated(c);
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
                if (left.Transforms[li].Matrix != right.Transforms[ri].Matrix)
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

        public void Transform(Matrix transient)
        {
            //TO-DO graph is dirty mechanism
            //LocalTransient = transient;
            Transforms[0].Matrix = transient;
            UpdateTransformGraph();
        }

        public void UpdateTransformGraph()
        {
            Local = Transforms[0].Matrix;
            for (int i = 1; i < Transforms.Count; i++)
                Local *= Transforms[i].Matrix;

            if (Parent != null)
                AccumulatedTransform.Matrix = Local * Parent.Accumulated;
            else
                AccumulatedTransform.Matrix = Local;

            foreach (var c in this.Children)
                c.UpdateTransformGraph();
        }
    }
}
