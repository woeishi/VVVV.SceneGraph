using SlimDX;

namespace SceneGraph.Core
{
    public class MaterialInfo
    {
        public int Index { get; internal set; }
        public Color4 AmbientColor { get; internal set; }
        public Color4 DiffuseColor { get; internal set; }
        public Color4 SpecularColor { get; internal set; }
        public float SpecularPower { get; internal set; }
        //public List<eAssimpTextureMapMode> TextureMapMode { get; }
        //public List<eAssimpTextureOp> TextureOperation { get; }
        public TextureInfo[] Textures {get; internal set; }

        public MaterialInfo() {}

        internal MaterialInfo(MaterialInfo other)
        {
            Index = other.Index;
            AmbientColor = other.AmbientColor;
            DiffuseColor = other.DiffuseColor;
            SpecularColor = other.SpecularColor;
            SpecularPower = other.SpecularPower;
            Textures = other.Textures;
        }
    }

    public enum TextureIntent
    {
        Diffuse = 1,
        Specular = 2,
        Ambient = 3,
        Emissive = 4,
        Height = 5,
        Normals = 6,
        Shininesss = 7,
        Opacity = 8,
        Displacement = 9,
        LightMap = 10,
        Reflection = 11,
        Unknown = 12
    }

    public class TextureInfo
    {
        public int Index { get; internal set; }
        public string Path { get; internal set; }
        public string FullPath { get; internal set; }
        public TextureIntent Intent { get; internal set; }
        //MapMode /sampler
        //blend operation
    }
}
