using System;

namespace SceneGraph.Core
{
    internal interface IResourceManager : IDisposable
    {
        Type KeyType { get; }

        ResourceToken GetGeometry(int meshId, dynamic context, out dynamic geometry);
        ResourceToken GetTexture(int textureId, dynamic context, out dynamic texture);
        ResourceToken GetDefaultTexture(dynamic context, out dynamic texture);

        void Purge();
    }
}