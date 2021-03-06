﻿using System;

namespace SceneGraph.Core
{
    internal class RefCounter<T> : IDisposable where T : IDisposable
    {
        Func<T> Ctor { get; set; }
        T Resource { get; set; }
        int Counter { get; set; }
        internal bool IsValid { get; private set; }

        internal RefCounter(Func<T> createResource) { Ctor = createResource; }

        public void Dispose()
        {
            if (IsValid)
            {
                Resource?.Dispose();
                IsValid = false;
            }
        }

        internal T Take()
        {
            if (!IsValid)
            {
                try
                {
                    Resource = Ctor();
                }
                catch (Exception)
                {
                    Resource = default(T);
                }
                IsValid = true;
            }
            Counter++;
            return Resource;
        }

        internal void Release()
        {
            if (IsValid)
                Counter--;
        }

        internal void Purge()
        {
            if (IsValid && Counter <= 0)
            {
                Resource?.Dispose();
                IsValid = false;
                System.Diagnostics.Debug.Assert(Counter == 0);
            }
        }
    }
}
