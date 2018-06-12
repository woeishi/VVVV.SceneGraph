using System;
using SceneGraph.Core;

using FeralTic.DX11;
using FeralTic.DX11.Resources;

namespace SceneGraph.DX11
{
    internal class DX11ResourceHandler<T> : ResourceHandler<DX11RenderContext, T> where T : IDX11Resource
    {
        internal DX11ResourceHandler(Func<DX11RenderContext, T> createFunc) : base(createFunc) { }
    }
}
