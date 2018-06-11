using System;
using System.Collections.Generic;

using FeralTic.DX11;
using FeralTic.DX11.Resources;

namespace SceneGraph.DX11
{
    internal class DX11ResourceHandler<T> : IDisposable where T : IDX11Resource
    {
        Func<DX11RenderContext, T> create;
        Dictionary<DX11RenderContext, T> resources;
        Dictionary<DX11RenderContext, HashSet<string>> users;

        internal DX11ResourceHandler(Func<DX11RenderContext, T> createFunc)
        {
            resources = new Dictionary<DX11RenderContext, T>();
            users = new Dictionary<DX11RenderContext, HashSet<string>>();
            create = createFunc;
        }

        public void Dispose()
        {
            foreach (var res in resources.Values)
                res.Dispose();
        }

        internal T Get(DX11RenderContext context, string nodePath)
        {
            if (!resources.ContainsKey(context))
            {
                resources.Add(context, create(context));
                var hashSet = new HashSet<string>();
                hashSet.Add(nodePath);
                users.Add(context, hashSet);
            }
            else
            {
                users[context].Add(nodePath);
            }
            return resources[context];
        }

        internal void Release(string nodePath, DX11RenderContext context = null)
        {
            if (context == null)
                foreach (var ctx in resources.Keys)
                    users[ctx].Remove(nodePath);
            else if (users.ContainsKey(context))
                users[context].Remove(nodePath);
        }

        internal void Purge()
        {
            var copy = new List<DX11RenderContext>(resources.Keys);
            foreach (var ctx in copy)
            {
                if (users[ctx].Count == 0)
                {
                    users.Remove(ctx);
                    resources[ctx].Dispose();
                    resources.Remove(ctx);
                }
            }
        }
    }
}
