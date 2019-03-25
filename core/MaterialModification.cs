using SlimDX;

namespace SceneGraph.Core
{
    public static class MaterialModification
    {
        public enum BlendMode { Blend, Multiply, Add }

        public static void Modify(this GraphNode node,
            Color4 ambient = new Color4(), Vector4 aAmount = new Vector4(), BlendMode aMode = BlendMode.Blend,
            Color4 diffuse = new Color4(), Vector4 dAmount = new Vector4(), BlendMode dMode = BlendMode.Blend,
            Color4 specular = new Color4(), Vector4 sAmount = new Vector4(), BlendMode sMode = BlendMode.Blend,
            float specularPower = 0, float spAmount = 0, BlendMode spMode = BlendMode.Blend)
        {
            if (node.Element is MeshElement)
            {
                node.MaterialProxy = new MaterialInfo(node.MaterialReference);
                if (aAmount != Vector4.Zero)
                    node.MaterialProxy.AmbientColor = Blend(node.MaterialReference.AmbientColor, ambient, aAmount, aMode);
                if (dAmount != Vector4.Zero)
                    node.MaterialProxy.DiffuseColor = Blend(node.MaterialReference.DiffuseColor, diffuse, dAmount, dMode);
                if (sAmount != Vector4.Zero)
                    node.MaterialProxy.SpecularColor = Blend(node.MaterialReference.SpecularColor, specular, sAmount, sMode);
                if (spAmount != 0)
                    node.MaterialProxy.SpecularPower = Blend(node.MaterialReference.SpecularPower, specularPower, spAmount, spMode);
            }
        }

        static float Blend(float src, float dest, float factor, BlendMode mode)
        {
            switch (mode)
            {
                case BlendMode.Blend:
                    return Lerp(src, dest, factor);
                case BlendMode.Multiply:
                    return src * Lerp(1, dest, factor);
                default:
                    return src + dest*factor;
            }
        }

        static Color4 Blend(Color4 src, Color4 dest, Vector4 factor, BlendMode mode)
        {
            switch (mode)
            {
                case BlendMode.Blend:
                    return Lerp(src, dest, factor);
                case BlendMode.Multiply:
                    return src * Lerp(new Color4(1.0f,1.0f,1.0f), dest, factor);
                default:
                    return src + Scale(dest, factor);
            }
        }

        static float Lerp(float start, float end, float amount) => start * (1 - amount) + end * amount;

        static Color4 Lerp(Color4 start, Color4 end, Vector4 amount)
        {
            start.Red = Lerp(start.Red, end.Red, amount.X);
            start.Green = Lerp(start.Green, end.Green, amount.Y);
            start.Blue = Lerp(start.Blue, end.Blue, amount.Z);
            start.Alpha = Lerp(start.Alpha, end.Alpha, amount.W);
            return start;
        }

        static Color4 Scale(Color4 a, Vector4 b) => new Color4(a.Alpha * b.W, a.Red * b.X, a.Green * b.Y, a.Blue * b.Z);
    }
}
