using System;
using SlimDX;
using SlimDX.Direct3D9;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SceneGraph.EX9
{
    internal static class EX9Utils
    {
        internal static Texture CreateTexture(Device device, string path)
        {
            return Texture.FromFile(device, path);
        }

        internal static Texture DefaultWhite(Device device)
        {
            var pool = Pool.Managed;
            var usage = Usage.None;
            if (device is DeviceEx)
            {
                pool = Pool.Default;
                usage = Usage.Dynamic;
            }
            var t = new Texture(device, 2, 2, 1, usage, Format.A8R8G8B8, pool);
            var dr = t.LockRectangle(0, LockFlags.None);
            var l = dr.Data.Length;
            for (int i = 0; i < l; i++)
                dr.Data.WriteByte(255);
            t.UnlockRectangle(0);
            return t;
        }   

        static DeclarationType ToDeclaration(this SlimDX.DXGI.Format fmt)
        {
            var format = fmt.ToString();
            Regex r = new Regex("([R|G|B|A|X])([0-9]+)");
            var m = r.Matches(format);
            if (0 < m.Count && m.Count <= 4)
                return (DeclarationType)(m.Count - 1);
            else
                throw new NotSupportedException($"no matching DeclarationType for {fmt}");
        }

        static DeclarationUsage Parse(string name)
        {
            if (name.Contains("TEXCOORD"))
                name = "TextureCoordinate";
            try
            {
                return (DeclarationUsage)Enum.Parse(typeof(DeclarationUsage), name, true);
            }
            catch
            {
                throw new NotSupportedException("no matching DeclarationUsage for "+name);
            }
        }

        internal static Mesh LoadMesh(Device device, int vertexCount, DataStream vStream, int[] indices, List<SlimDX.Direct3D11.InputElement> layout)
        {
            if (indices.Length > 0 && vertexCount > 0)
            {
                VertexElement[] elements = new VertexElement[layout.Count+1];
                for (int i = 0; i < layout.Count; i++)
                {
                    elements[i] = new VertexElement();
                    elements[i].Offset = (short)(layout[i].AlignedByteOffset);
                    elements[i].Type = layout[i].Format.ToDeclaration();
                    elements[i].Usage = Parse(layout[i].SemanticName);
                }
                elements[elements.Length - 1] = VertexElement.VertexDeclarationEnd;
              
                Mesh mesh = null;
                if (device is DeviceEx)
                    mesh = new Mesh(device, indices.Length / 3, vertexCount, MeshFlags.Dynamic | MeshFlags.Use32Bit, elements);
                else
                    mesh = new Mesh(device, indices.Length / 3, vertexCount, MeshFlags.Managed | MeshFlags.Use32Bit, elements);
                
                var vertices = mesh.LockVertexBuffer(LockFlags.None);
                vStream.Position = 0;
                vertices.WriteRange(vStream.DataPointer, vStream.Length);
                mesh.UnlockVertexBuffer();

                var ids = mesh.LockIndexBuffer(LockFlags.None);
                ids.WriteRange(indices);
                mesh.UnlockIndexBuffer();

                return mesh;
            }
            else
                return null;
        }
    }
}
