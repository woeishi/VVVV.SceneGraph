using System;
using System.Collections.Generic;

using SlimDX;
using SceneGraph.Core.Animation;

namespace SceneGraph.Core
{
    public class GraphNode
    {
        public IScene Scene { get; }
        public Element Element { get; }
        public GraphNode Parent { get; private set; }
        public GraphNode[] Children { get; internal set; }
        internal int LastDescendantID { get; set; }

        public int ID => Element.ID;
        public string Name => Element.Name;

        public override string ToString() => $"{Element.Name} [{Element.Type}]";

        List<Transform> Transforms;
        Transform AccumulatedTransform;
        
        public Matrix Local { get; private set; }
        public Matrix Accumulated => AccumulatedTransform.Matrix;
        public List<Channel> Tracks { get; }

        public MaterialInfo Material
        {
            get
            {
                if (!(Element is MeshElement))
                    return null;
                else
                    return MaterialProxy??MaterialReference;
            }
        }
        internal MaterialInfo MaterialReference;
        internal MaterialInfo MaterialProxy;

        //only in use when initally parsing the scene
        internal GraphNode(Element element, Matrix accumulated, Matrix local, List<Channel> tracks, GraphNode parent = null, IScene scene = null)
        {
            Scene = scene;
            Element = element;
            Parent = parent;
            Children = new GraphNode[Element.ChildCount];

            Transforms = new List<Transform>();
            Transforms.Add(new Transform(local, this));
            Transforms[0].Changed += MarkBranchChanged;
            AccumulatedTransform = new Transform(accumulated, this);
            
            Local = local;

            Tracks = tracks;
        }

        internal GraphNode(MeshElement element, Matrix accumulated, GraphNode parent = null, IScene scene = null) : this(element as Element, accumulated, Matrix.Identity, new List<Channel>(), parent, scene)
        {
            MaterialReference = element.Material;
            Children = new GraphNode[0];
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

            Transforms = new List<Transform>(other.Transforms);
            Local = other.Local;
            foreach (var t in Transforms)
                t.Changed += MarkBranchChanged;
            AccumulatedTransform = other.AccumulatedTransform;

            Tracks = new List<Channel>();
            foreach (var t in other.Tracks)
                Tracks.Add(new Channel(t));

            MaterialReference = other.Material;
        }

        public void DisposeGraph()
        {
            foreach (var t in Transforms)
                t.Changed -= MarkBranchChanged;
            foreach (var c in Children)
                c.DisposeGraph();
        }

        public void Fork()
        {
            Transforms[0].Changed -= MarkBranchChanged;
            Transforms.Insert(0,new Transform(Transforms[0], this));
            //from the fork onward all nodes have to hold their own accumulated transform
            Transforms[0].Changed += MarkBranchChanged;
            CloneAccumulated(this);
        }
        void CloneAccumulated(GraphNode node)
        {
            node.AccumulatedTransform = new Transform(node.Accumulated, node);
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
                //if (left.Transforms[li].Matrix != right.Transforms[ri].Matrix)
                if (left.Transforms[li].Owner != right.Transforms[ri].Owner)
                    break;
                li--;
                ri--;
            }

            var tmp = right.Transforms.GetRange(0, ri + 1);
            left.Transforms.InsertRange(0, tmp);
            foreach (var t in tmp)
                t.Changed += MarkBranchChanged;

            for (int i = 0; i < left.Children.Length; i++)
                MergeTransforms(left.Children[i], right.Children[i]);
        }

        public void Transform(Matrix transient)
        {
            Transforms[0].Matrix = transient;
            Transforms[0].MarkChanged();
        }

        void MarkBranchChanged(object sender, EventArgs e)
        {
            this.AccumulatedTransform.IsDirty = true;
            foreach (var c in this.Children)
                c.MarkBranchChanged(sender, e);
        }

        

        public void ResolveTransformGraph()
        {
            if (this.AccumulatedTransform.IsDirty)
            {
                Stack<GraphNode> nodes = new Stack<GraphNode>();
                nodes.Push(this);
                var node = this;
                while (node.Parent != null && node.Parent.AccumulatedTransform.IsDirty)
                {
                    node = node.Parent;
                    nodes.Push(node);
                }

                while (nodes.Count > 0)
                {
                    var n = nodes.Pop();
                    n.ResolveNodeTransform();
                }
            }
        }

        void ResolveNodeTransform()
        {
            Local = Transforms[0].Matrix;
            for (int i = 1; i < Transforms.Count; i++)
                Local *= Transforms[i].Matrix;

            if (Parent != null)
                AccumulatedTransform.Matrix = Local * Parent.Accumulated;
            else
                AccumulatedTransform.Matrix = Local;

            AccumulatedTransform.IsDirty = false;
        }

        public void ForkAnimation()
        {
            if (Tracks.Count > 0)
            {
                Transforms[Transforms.Count - 1].Changed -= MarkBranchChanged;
                Tracks[0].Original = Transforms[Transforms.Count - 1];
                var t = new Transform(Transforms[Transforms.Count - 1].Matrix, this);
                Transforms[Transforms.Count - 1] = t;
                t.Changed += MarkBranchChanged;
            }
            CloneAccumulated(this);
        }
        
        public void Animate(float time, bool normalize, int index = 0)
        {
            if (Tracks.Count > 0)
            {
                Transforms[Transforms.Count - 1].Matrix = Tracks[index].GetMatrix(time, normalize);
                Transforms[Transforms.Count - 1].MarkChanged();
            }
        }

        public void ResetAnimation()
        {
            if (Tracks[0].Original != null)
            {
                Transforms[Transforms.Count - 1].Changed -= MarkBranchChanged;
                Transforms[Transforms.Count - 1] = Tracks[0].Original;
                Transforms[Transforms.Count - 1].Changed += MarkBranchChanged;
                Transforms[Transforms.Count - 1].MarkChanged();
            }
        }
    }
}
