namespace SceneGraph.Core
{
    internal static class GraphNodeExtension
    {
        internal static ResourceToken GetGeometry(this GraphNode node, dynamic context, out dynamic geometry)
        {
            return node.Scene.GetGeometry(node, context, out geometry);
        }

        internal static ResourceToken GetTexture(this GraphNode node, TextureInfo textureInfo, dynamic context, out dynamic texture)
        {
            return node.Scene.GetTexture(textureInfo, context, out texture);
        }
    }
}
