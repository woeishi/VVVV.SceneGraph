namespace SceneGraph.Core
{
    internal static class GraphNodeExtension
    {
        internal static dynamic GetGeometry(this GraphNode node, string nodePath, dynamic context)
        {
            return node.Scene.GetGeometry(node, nodePath, context);
        }

        internal static void ReleaseGeometry(this GraphNode node, string nodePath, dynamic context = null)
        {
            node.Scene.ReleaseGeometry(node, nodePath, context);
        }

        internal static void PurgeGeometry(this GraphNode node)
        {
            node.Scene.PurgeGeometry(node);
        }

        internal static dynamic GetTexture(this GraphNode node, TextureInfo textureInfo, string nodePath, dynamic context)
        {
            return node.Scene.GetTexture(textureInfo, nodePath, context) ?? context.DefaultTextures.WhiteTexture;
        }

        internal static void ReleaseTexture(this GraphNode node, string nodePath, TextureInfo textureInfo = null, dynamic context = null)
        {
            node.Scene.ReleaseTexture(node, textureInfo, nodePath, context);
        }

        internal static void PurgeTextures(this GraphNode node)
        {
            node.Scene.PurgeTextures(node);
        }
    }
}
