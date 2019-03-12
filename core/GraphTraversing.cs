using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml.XPath;

using SceneGraph.Core.Animations;

namespace SceneGraph.Core
{
    public static class GraphTraversing
    {
        public static IEnumerable<GraphNode> ExpandNodes(this GraphNode node, int limit, bool includeSelf = true, int depth = 0)
        {
            if (node != null && (limit < 0 || depth <= limit))
            {
                if (depth > 0 || includeSelf)
                    yield return node;
                depth++;
                foreach (var c in node.Children)
                    foreach (var n in c.ExpandNodes(limit, depth: depth))
                        yield return n;
            }
        }

        public static IEnumerable<GraphNode> ExpandMeshNodes(this GraphNode node, int limit, int depth = 0)
        {
            if (node != null && (limit < 0 || depth <= limit))
            {
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

        internal static XElement ToXElement(this GraphNode node)
        {
            var element = new XElement(node.Name);
            element.Add(new XAttribute("id", node.ID));
            element.Add(new XAttribute("nodetype", node.Element.Type));

            var me = node.Element as MeshElement;
            if (me != null)
            {
                element.Add(new XAttribute("meshId", me.MeshID));
                element.Add(new XAttribute("materialId", me.MaterialID));

                var textures = node.Material.Textures;
                foreach (var t in textures)
                {
                    var txml = new XElement(t.Intent.ToString());
                    txml.Add(new XAttribute("textureId",t.Index));
                    txml.Add(new XAttribute("path", t.Path));
                    element.Add(txml);
                }
            }

            element.AppendAnimations(node);

            foreach (var c in node.Children)
                element.Add(c.ToXElement());
            return element;
        }

        public static XElement XElement(this GraphNode node)
        {
            if (node.ID == 0)
                return node.Scene.XmlRoot;
            else
                return node.Scene.XmlRoot.XPathSelectElement($"//*[@id='{node.ID}']");
        }

        public static IEnumerable<GraphNode> XPathQuery(this GraphNode node, string query)
        {
            var idQuery = query;
            if (!idQuery.EndsWith("/"))
                idQuery += "/";
            idQuery += "@id";
            var res = node.XElement().XPathEvaluate(idQuery);
            if (res is IEnumerable)
            {
                foreach (var a in (IEnumerable)res)
                {
                    int id = 0;
                    if (int.TryParse((a as XAttribute).Value, out id))
                    {
                        var n = node.GetByID(id);
                        if (n != null)
                            yield return n;
                    }
                }
            }
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
