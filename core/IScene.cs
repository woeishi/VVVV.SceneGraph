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
    }
}