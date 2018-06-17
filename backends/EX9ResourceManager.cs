using System;
using SceneGraph.Core;
using SceneGraph.EX9;
using SlimDX.Direct3D9;

namespace SceneGraph.EX9
{
    internal class EX9ResourceManager : ResourceManager<Device, Mesh, Texture>
    {
        public EX9ResourceManager(
            int meshCount,
            Func<int, Device, Mesh> meshCreate,
            int textureCount,
            Func<int, Device, Texture> texCreate) : base(meshCount, meshCreate, textureCount, texCreate, EX9Utils.DefaultWhite) { }
    }
}
