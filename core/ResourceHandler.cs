using System;
using System.Collections.Generic;

namespace SceneGraph.Core
{
    public class ResourceToken
    {
        public dynamic Key { get; }
        Action ReleaseAction;
        internal ResourceToken(dynamic key, Action releaseAction)
        {
            Key = key;
            ReleaseAction = releaseAction;
        }

        public void Dispose() => ReleaseAction();
    }


    internal class ResourceHandler<U, T> : IDisposable where T : IDisposable
    {
        Func<U, T> create;
        Dictionary<U, RefCounter<T>> resources;

        internal ResourceHandler(Func<U, T> createFunc)
        {
            resources = new Dictionary<U, RefCounter<T>>();
            create = createFunc;
        }

        public void Dispose()
        {
            foreach (var res in resources.Values)
                res.Dispose();
        }

        internal ResourceToken Take(U key, out T resource)
        {
            if (!resources.ContainsKey(key))
                resources.Add(key, new RefCounter<T>(() => create(key)));
            resource = resources[key].Take();
            var refCnt = resources[key];
            return new ResourceToken(key, () => refCnt.Release());
        }

        internal void Purge()
        {
            foreach (var res in resources.Values)
                res.Purge();
        }
    }
}
