using System;

namespace SceneGraph.Core
{
    internal class ResourceManager<T, U, V> : IResourceManager
        where U : IDisposable
        where V : IDisposable
    {
        public Type KeyType { get; }
        internal ResourceHandler<T, U>[] MeshHandlers { get; private set; }
        internal ResourceHandler<T, V>[] TextureHandlers { get; private set; }
        protected ResourceHandler<T, V> DefaultTexture { get; private set; }

        internal ResourceManager(int meshCount, Func<int,T,U> meshCreate, int textureCount, Func<int, T, V> textureCreate, Func<T,V> defaultTextureCreate)
        {
            KeyType = typeof(T);

            MeshHandlers = new ResourceHandler<T, U>[meshCount];
            for (int i = 0; i < MeshHandlers.Length; i++)
            {
                int id = i;
                Func<T, U> mc = (T) => meshCreate(id, T);
                MeshHandlers[i] = new ResourceHandler<T, U>(mc);
            }

            TextureHandlers = new ResourceHandler<T, V>[textureCount];
            for (int i = 0; i < TextureHandlers.Length; i++)
            {
                int id = i;
                Func<T, V> tc = (T) => textureCreate(id, T);
                TextureHandlers[i] = new ResourceHandler<T, V>(tc);
            }

            DefaultTexture = new ResourceHandler<T, V>(defaultTextureCreate);
        }

        public void Dispose()
        {
            foreach (var mh in MeshHandlers)
                mh.Dispose();
            foreach (var th in TextureHandlers)
                th.Dispose();
            DefaultTexture.Dispose();
        }

        public void Purge()
        {
            foreach (var mh in MeshHandlers)
                mh.Purge();
            foreach (var th in TextureHandlers)
                th.Purge();
        }

        public ResourceToken GetGeometry(int meshId, dynamic context, out dynamic geometry)
        {
            U geom;
            var token = MeshHandlers[meshId].Take(context, out geom);
            geometry = geom;
            return token;
        }

        public ResourceToken GetTexture(int textureId, dynamic context, out dynamic texture)
        {
            V tex;
            var token = TextureHandlers[textureId].Take(context, out tex);
            texture = tex;
            return token;
        }
        public ResourceToken GetDefaultTexture(dynamic context, out dynamic texture)
        {
            V tex;
            var token = DefaultTexture.Take(context, out tex);
            texture = tex;
            return token;
        }
    }
}
