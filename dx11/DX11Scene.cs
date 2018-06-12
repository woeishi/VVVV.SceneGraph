using SceneGraph.Core;
using FeralTic.DX11;
using FeralTic.DX11.Resources;

namespace SceneGraph.DX11
{
    public class SceneDX11<T> : Scene<T, DX11RenderContext, DX11IndexedGeometry, DX11Texture2D>
    {
        public SceneDX11(string path) : base(path) { }

        public SceneDX11(string path, string assetPath) : base(path, assetPath) { }
    }
}     