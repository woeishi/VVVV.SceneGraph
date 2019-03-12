using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace SceneGraph.Core.Animations
{
    public static class AnimationExtensions
    {
        public static void AppendAnimations(this XElement element, GraphNode node)
        {
            if (node.AnimationsReference != null)
            {
                foreach (var a in node.AnimationsReference.Animations)
                {
                    if (a.Channels.Length > 0)
                    {
                        var animation = new XElement("Animation");
                        animation.Add(new XAttribute("animationName", a.Name));
                        animation.Add(new XAttribute("duration", a.Duration));
                        animation.Add(new XAttribute("ticksPerSecond", a.TicksPerSecond));
                        foreach (var c in a.Channels)
                        {
                            var channel = new XElement("Channel");
                            channel.Add(new XAttribute("type", c.Type));
                            channel.Add(new XAttribute("markerCount", c.Markers.Length));
                            animation.Add(channel);
                        }
                        element.Add(animation);
                    }
                }
            }
        }

        public static IEnumerable<GraphNode> FindAnimation(this GraphNode node, string text, bool contains = false, int limit = -1)
        {
            object res = null;
            if (string.IsNullOrWhiteSpace(text))
                res = new[] { node.XElement() };
            else
            {
                var query = $"descendant-or-self::*[(name()='{text}') or (@animationName='{text}')]";
                if (contains)
                    query = $"descendant-or-self::*[(contains(name(),'{text}')) or (contains(@animationName,'{text}'))]";
                res = node.XElement().XPathEvaluate(query);
            }
            
            if (res is IEnumerable)
            {
                foreach (var n in (IEnumerable)res)
                {
                    if (n is XElement)
                    {
                        var element = n as XElement;
                        if (element.Name == "Animation")
                        {
                            int aId = element.ElementsBeforeSelf("Animation").Count();
                            int id = 0;
                            if (int.TryParse(element.Parent.Attribute("id").Value, out id))
                            {
                                var gn = node.GetByID(id);
                                gn.AnimationsProxy = new AnimationInfo(new[] { gn.AnimationsReference.Animations[aId] });
                                yield return gn;
                            }
                        }
                        else
                        {
                            int id = 0;
                            if (int.TryParse(element.Attribute("id").Value, out id))
                            {
                                var gn = node.GetByID(id);
                                foreach (var hit in gn.DescendantsWithAnimation(limit))
                                    yield return hit;
                            }
                        }
                    }
                }
            }
        }

        static IEnumerable<GraphNode> DescendantsWithAnimation(this GraphNode node, int limit = -1, int depth = 0)
        {
            if (limit < 0 || limit>=depth)
            {
                if (node.AnimationsReference != null)
                {
                    node.AnimationsProxy = new AnimationInfo(node.AnimationsReference.Animations);
                    yield return node;
                }
                depth++;
                foreach (var c in node.Children)
                    foreach (var n in c.DescendantsWithAnimation(limit, depth))
                        yield return n;
            }
        }

        public static IEnumerable<GraphNode> XPathQueryAnimation(this GraphNode node, string query)
        {
            // a query can result in multiple selections for one graphnode
            // thus a gather step is required instead of direct yielding
            var res = node.XElement().XPathEvaluate(query);
            List<Tuple<GraphNode, Animation, IChannel>> collection = new List<Tuple<GraphNode, Animation, IChannel>>();
            if (res is IEnumerable)
            {
                foreach (var n in (IEnumerable)res)
                {
                    XElement element = null;
                    XAttribute attr = null;
                    if (n is XElement)
                        element = n as XElement;
                    else if (n is XAttribute)
                    {
                        attr = n as XAttribute;
                        element = attr.Parent;
                    }
                    if (element != null)
                    {
                        int cId = -1;
                        int aId = -1;
                        if (element.Name == "Channel")
                        {
                            cId = element.ElementsBeforeSelf("Channel").Count();
                            aId = element.Parent.ElementsBeforeSelf("Animation").Count();
                            element = element.Parent.Parent;
                        }
                        else if (element.Name == "Animation")
                        {
                            aId = element.ElementsBeforeSelf("Animation").Count();
                            element = element.Parent;
                        }

                        if (element.Elements("Animation").Any())
                        {
                            int id = 0;
                            if (int.TryParse(element.Attribute("id").Value, out id))
                            {
                                var gn = collection.Where(t => t.Item1.ID == id).Select(t => t.Item1).FirstOrDefault();
                                if (gn == null)
                                    gn = node.GetByID(id);
                                if (gn != null)
                                {
                                    var a = (aId == -1) ? null : gn.AnimationsReference.Animations[aId];
                                    var c = (cId == -1) ? null : a.Channels[cId];
                                    collection.Add(new Tuple<GraphNode, Animation, IChannel>(gn, a, c));
                                }
                            }
                        }
                    }
                }
            }

            foreach (var group in collection.GroupBy(t => t.Item1))
            {
                var gn = group.Key;
                var anims = new List<Animation>();
                var chanDict = new Dictionary<Animation, List<IChannel>>();
                foreach (var tup in group)
                {
                    if (tup.Item2 == null) //animation
                    {
                        foreach (var a in gn.AnimationsReference.Animations)
                        {
                            if (!chanDict.ContainsKey(a))
                            {
                                anims.Add(a);
                                chanDict.Add(a, new List<IChannel>());
                            }
                            if (tup.Item3 == null)
                            {
                                foreach (var c in a.Channels)
                                    if (!chanDict[a].Contains(c))
                                        chanDict[a].Add(c);
                            }
                            else if (!chanDict[a].Contains(tup.Item3))
                                chanDict[a].Add(tup.Item3);
                                
                        }
                    }
                    else
                    {
                        var a = tup.Item2;
                        if (!chanDict.ContainsKey(a))
                        {
                            anims.Add(a);
                            chanDict.Add(a, new List<IChannel>());
                        }
                        if (tup.Item3 == null)
                        {
                            foreach (var c in a.Channels)
                                if (!chanDict[a].Contains(c))
                                    chanDict[a].Add(c);
                        }
                        else if (!chanDict[a].Contains(tup.Item3))
                            chanDict[a].Add(tup.Item3);
                    }
                }
                gn.AnimationsProxy = new AnimationInfo(anims.Select(a => new Animation(a, chanDict[a])).ToArray());
                yield return gn;
            }
        }
    }
    
}
