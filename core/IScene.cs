using System;
using System.Xml.Linq;

using SceneGraph.Core.Animations;

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

        Animation[] Animations { get; }

        ResourceToken GetGeometry(GraphNode node, dynamic context, out dynamic geometry);
        ResourceToken GetTexture(TextureInfo textureInfo, dynamic context, out dynamic geometry);

        void PurgeResources();
    }
}