using System;

namespace SceneGraph.Core
{
    internal interface IResourceManager : IDisposable
    {
        Type KeyType { get; }

        dynamic GetGeometry(int meshId, string nodePath, dynamic context);
        void ReleaseGeometry(int meshId, string nodePath, dynamic context);
        void PurgeGeometry(int meshId);

        dynamic GetTexture(int textureId, string nodePath, dynamic context);
        void ReleaseTexture(int textureId, string nodePath, dynamic context);
        void PurgeTextures(int textureId);
    }
}