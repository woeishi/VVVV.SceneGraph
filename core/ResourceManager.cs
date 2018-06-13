using System;

namespace SceneGraph.Core
{
    internal class ResourceManager<T, U, V> : IDisposable
        where U : IDisposable
        where V : IDisposable
    {
        internal ResourceHandler<T, U>[] MeshHandlers { get; }
        internal ResourceHandler<T, V>[][] TextureHandlers { get; }

        internal ResourceManager(MeshInfo[] meshInfos, MaterialInfo[] materialInfos)
        {
            MeshHandlers = new ResourceHandler<T, U>[meshInfos.Length];
            TextureHandlers = new ResourceHandler<T, V>[materialInfos.Length][];
            for (int i = 0; i < TextureHandlers.Length; i++)
                TextureHandlers[i] = new ResourceHandler<T, V>[materialInfos[i].Textures.Length];
        }

        internal void Initialize(Func<int,T,U> meshCreate, Func<int,int,T,V> textureCreate)
        {
            for (int i = 0; i < MeshHandlers.Length; i++)
            {
                int id = i;
                Func<T, U> mc = (T) => meshCreate(id, T);
                MeshHandlers[i] = new ResourceHandler<T, U>(mc);
            }

            for (int i = 0; i < TextureHandlers.Length; i++)
            {
                for (int t = 0; t < TextureHandlers[i].Length; t++)
                {
                    int id = i;
                    int id2 = t;
                    Func<T, V> tc = (T) => textureCreate(id, id2, T);
                    TextureHandlers[i][t] = new ResourceHandler<T, V>(tc);
                }
            }
        }

        public void Dispose()
        {
            foreach (var mh in MeshHandlers)
                mh.Dispose();
            foreach (var tha in TextureHandlers)
                foreach (var th in tha)
                    th.Dispose();
        }

        internal U GetGeometry(int meshId, T context, string nodePath) => MeshHandlers[meshId].Get(context, nodePath);
        internal void ReleaseGeometry(int meshId, string nodePath, T context) => MeshHandlers[meshId].Release(nodePath, context);
        internal void PurgeGeometry(int meshId) => MeshHandlers[meshId].Purge();

        internal V GetTexture(int materialId, int textureId, T context, string nodePath)
        {
            if (textureId < TextureHandlers[materialId].Length)
                return TextureHandlers[materialId][textureId].Get(context, nodePath);
            else
                return default(V);
        }
        internal void ReleaseTexture(int materialId, int textureId, T context, string nodePath)
        {
            if (textureId < 0)
            {
                foreach (var th in TextureHandlers[materialId])
                    th.Release(nodePath, context);
            }
            else if (textureId < TextureHandlers[materialId].Length)
                TextureHandlers[materialId][textureId].Release(nodePath, context);
        }

        internal void PurgeTextures(int materialId)
        {
            foreach (var th in TextureHandlers[materialId])
                th.Purge();
        }
    }
}
