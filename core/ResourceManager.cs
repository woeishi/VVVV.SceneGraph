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

        internal ResourceManager(int meshCount, Func<int,T,U> meshCreate, int textureCount, Func<int, T, V> textureCreate)
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
        }

        public void Dispose()
        {
            foreach (var mh in MeshHandlers)
                mh.Dispose();
            foreach (var th in TextureHandlers)
                th.Dispose();
        }

        public dynamic GetGeometry(int meshId, string nodePath, dynamic context) => MeshHandlers[meshId].Get(nodePath, context);
        public void ReleaseGeometry(int meshId, string nodePath, dynamic context) => MeshHandlers[meshId].Release(nodePath, context);
        public void PurgeGeometry(int meshId) => MeshHandlers[meshId].Purge();

        public dynamic GetTexture(int textureId, string nodePath, dynamic context) => TextureHandlers[textureId].Get(nodePath, context);
        public void ReleaseTexture(int textureId, string nodePath, dynamic context) => TextureHandlers[textureId].Release(nodePath, context);
        public void PurgeTextures(int textureId) => TextureHandlers[textureId].Purge();
    }
}
