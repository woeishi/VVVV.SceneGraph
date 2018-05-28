using System;
using System.Collections.Generic;

namespace SceneGraph.Core
{
    public static class GraphTraversing
    {
        public static IEnumerable<GraphNode> ExpandNodes(this GraphNode node, int limit, int depth, bool includeSelf = true)
        {
            if (node != null && (limit < 0 || depth <= limit))
            {
                if (depth > 0 || includeSelf)
                    yield return node;
                depth++;
                foreach (var c in node.Children)
                    foreach (var n in ExpandNodes(c, limit, depth))
                        yield return n;
            }
        }

        public static IEnumerable<GraphNode> ExpandMeshNodes(this GraphNode node, int limit, int depth)
        {
            if (node != null && (limit < 0 || depth <= limit))
            {
                //if ((node.Element as MeshElement)?.MeshCount > 0)
                //    yield return node;
                if (node.Element is MeshElement)
                    yield return node;
                depth++;
                foreach (var c in node.Children)
                    foreach (var n in ExpandMeshNodes(c, limit, depth))
                        yield return n;
            }
        }

        public static IEnumerable<GraphNode> FindNodes(this GraphNode node, string text, bool contains = false)
        {
            return contains ? node.Contains(text) : node.Matches(text);
        }

        internal static IEnumerable<GraphNode> Matches(this GraphNode node, string text)
        {
            if (node.Name == text)
                yield return node;

            foreach (var c in node.Children)
                foreach (var result in c?.Matches(text))
                    yield return result;
            yield break;
        }

        internal static IEnumerable<GraphNode> Contains(this GraphNode node, string text)
        {
            if (node.Name.Contains(text))
                yield return node;

            foreach (var c in node.Children)
                foreach (var result in c?.Contains(text))
                    yield return result;
            yield break;
        }

        internal static GraphNode GetByID(this GraphNode node, int id)
        {
            if (node.ID == id)
                return node;

            //var node = this;
            while ((node.ID >= id) && (node.Parent != null))
                node = node.Parent;

            if (node.ID == id)
                return node;

            foreach (var c in node.Children)
            {
                if (c?.LastDescendantID >= id)
                    return c.GetByID(id);
            }
            return null;
        }

        internal static GraphNode Root(this GraphNode node)
        {
            while (node.Parent != null)
                node = node.Parent;
            return node;
        }
    }
}
