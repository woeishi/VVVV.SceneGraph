using System;

namespace SceneGraph.Core
{
    internal class ResourceManager<T, U, V> : IDisposable
        where U : IDisposable
        where V : IDisposable
    {
        internal ResourceHandler<T, U>[] MeshHandlers { get; }
        internal ResourceHandler<T, V>[] TextureHandlers { get; }

        internal ResourceManager(MeshInfo[] meshInfos, TextureInfo[] textureInfos)
        {
            MeshHandlers = new ResourceHandler<T, U>[meshInfos.Length];
            TextureHandlers = new ResourceHandler<T, V>[textureInfos.Length];
        }

        internal void Initialize(Func<int,T,U> meshCreate, Func<int, T, V> texCreate)
        {
            for (int i = 0; i < MeshHandlers.Length; i++)
            {
                int id = i;
                Func<T, U> mc = (T) => meshCreate(id, T);
                MeshHandlers[i] = new ResourceHandler<T, U>(mc);
            }

            for (int i = 0; i < TextureHandlers.Length; i++)
            {
                int id = i;
                Func<T, V> ttc = (T) => texCreate(id, T);
                TextureHandlers[i] = new ResourceHandler<T, V>(ttc);
            }
        }

        public void Dispose()
        {
            foreach (var mh in MeshHandlers)
                mh.Dispose();
            foreach (var th in TextureHandlers)
                th.Dispose();
        }

        internal U GetGeometry(int meshId, string nodePath, T context) => MeshHandlers[meshId].Get(nodePath, context);
        internal void ReleaseGeometry(int meshId, string nodePath, T context) => MeshHandlers[meshId].Release(nodePath, context);
        internal void PurgeGeometry(int meshId) => MeshHandlers[meshId].Purge();

        internal V GetTexture(int textureId, string nodePath, T context) => TextureHandlers[textureId].Get(nodePath, context);
        internal void ReleaseTexture(int textureId, string nodePath, T context) => TextureHandlers[textureId].Release(nodePath, context);
        internal void PurgeTextures(int textureId) => TextureHandlers[textureId].Purge();
    }
}
