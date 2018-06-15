using System;
using SceneGraph.Core;
using FeralTic.DX11;
using FeralTic.DX11.Resources;

namespace SceneGraph.DX11
{
    public class SceneDX11<T> : Scene<T, DX11RenderContext, DX11IndexedGeometry, DX11Texture2D>
    {
        public SceneDX11(string path) : base(path) { }

        public SceneDX11(string path, string assetPath) : base(path, assetPath) { }
        
        protected void InitializeDX11Resources(Func<int, DX11RenderContext, DX11IndexedGeometry> meshCreate)
        {
            base.InitializeResources(meshCreate, (s, ctx) => { return DX11Texture2D.FromFile(ctx, s); });
        }
    }

    internal static class GraphNodeExtension
    {
        internal static DX11IndexedGeometry GetGeometry(this GraphNode node, DX11RenderContext context, string nodePath)
        {
            dynamic scene = node.Scene;
            return scene.GetGeometry(node, context, nodePath);
        }

        internal static void ReleaseGeometry(this GraphNode node, string nodePath, DX11RenderContext context = null)
        {
            dynamic scene = node.Scene;
            scene.ReleaseGeometry(node, context, nodePath);
        }

        internal static void PurgeGeometry(this GraphNode node)
        {
            dynamic scene = node.Scene;
            scene.PurgeGeometry(node);
        }

        internal static DX11Texture2D GetTexture(this GraphNode node, TextureInfo textureInfo, string nodePath, DX11RenderContext context)
        {
            dynamic scene = node.Scene;
            return scene.GetTexture(textureInfo, nodePath, context)??context.DefaultTextures.WhiteTexture;
        }

        internal static void ReleaseTexture(this GraphNode node, string nodePath, TextureInfo textureInfo = null, DX11RenderContext context = null)
        {
            dynamic scene = node.Scene;
            scene.ReleaseTexture(node, textureInfo, nodePath, context);
        }

        internal static void PurgeTextures(this GraphNode node)
        {
            dynamic scene = node.Scene;
            scene.PurgeTextures(node);
        }
    }
}     