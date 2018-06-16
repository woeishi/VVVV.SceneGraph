using System;
using SceneGraph.Core;
using FeralTic.DX11;
using FeralTic.DX11.Resources;

namespace SceneGraph.DX11
{
    internal class DX11ResourceManager : ResourceManager<DX11RenderContext, DX11IndexedGeometry, DX11Texture2D>
    {
        public DX11ResourceManager(
            int meshCount, 
            Func<int, DX11RenderContext, DX11IndexedGeometry> meshCreate, 
            int textureCount, 
            Func<int, DX11RenderContext, DX11Texture2D> texCreate) : base(meshCount, meshCreate, textureCount, texCreate, (c) => c.DefaultTextures.WhiteTexture)
        { }
    }
}
