using System;
using System.Collections.Generic;

namespace SceneGraph.Core
{
    internal class ResourceHandler<U, T> : IDisposable 
        where T : IDisposable
        where U : class
    {
        Func<U, T> create;
        Dictionary<U, T> resources;
        Dictionary<U, HashSet<string>> users;

        internal ResourceHandler(Func<U, T> createFunc)
        {
            resources = new Dictionary<U, T>();
            users = new Dictionary<U, HashSet<string>>();
            create = createFunc;
        }

        public void Dispose()
        {
            foreach (var res in resources.Values)
                res.Dispose();
        }

        internal T Get(U key, string nodePath)
        {
            if (!resources.ContainsKey(key))
            {
                resources.Add(key, create(key));
                var hashSet = new HashSet<string>();
                hashSet.Add(nodePath);
                users.Add(key, hashSet);
            }
            else
            {
                users[key].Add(nodePath);
            }
            return resources[key];
        }

        internal void Release(string nodePath, U key = null)
        {
            if (key == null)
                foreach (var k in resources.Keys)
                    users[k].Remove(nodePath);
            else if (users.ContainsKey(key))
                users[key].Remove(nodePath);
        }

        internal void Purge()
        {
            var copy = new List<U>(resources.Keys);
            foreach (var k in copy)
            {
                if (users[k].Count == 0)
                {
                    users.Remove(k);
                    resources[k].Dispose();
                    resources.Remove(k);
                }
            }
        }
    }
}
