using System.Xml.Linq;

namespace SceneGraph.Core
{
    public interface IScene
    {
        string AssetRoot { get; }
        string Filename { get; }
        GraphNode Root { get; }
        XElement XmlRoot { get; }

        MeshInfo[] MeshInfos { get; }
        MaterialInfo[] MaterialInfos { get; }
        TextureInfo[] TextureInfos { get; }

        dynamic GetGeometry(GraphNode node, string nodePath, dynamic context);
        void ReleaseGeometry(GraphNode node, string nodePath, dynamic context);
        void PurgeGeometry(GraphNode node);

        dynamic GetTexture(TextureInfo textureInfo, string nodePath, dynamic context);
        void ReleaseTexture(GraphNode node, TextureInfo textureInfo, string nodePath, dynamic context);
        void PurgeTextures(GraphNode node);
    }
}