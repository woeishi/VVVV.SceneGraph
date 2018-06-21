using System;
using System.Xml.Linq;

namespace SceneGraph.Core
{
    public interface IScene : IDisposable
    {
        string AssetRoot { get; }
        string Filename { get; }
        GraphNode Root { get; }
        XElement XmlRoot { get; }

        MeshInfo[] MeshInfos { get; }
        MaterialInfo[] MaterialInfos { get; }
        TextureInfo[] TextureInfos { get; }

        ResourceToken GetGeometry(GraphNode node, dynamic context, out dynamic geometry);
        ResourceToken GetTexture(TextureInfo textureInfo, dynamic context, out dynamic geometry);

        void PurgeResources();
    }
}