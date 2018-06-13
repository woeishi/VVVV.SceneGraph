using System;
using System.Collections.Generic;

using SlimDX;
using SlimDX.Direct3D11;
using FeralTic.DX11;
using FeralTic.DX11.Resources;

namespace SceneGraph.DX11
{
    internal static class DX11Utils
    {
        internal static DX11IndexedGeometry LoadMesh(DX11RenderContext context, int vertexCount, DataStream vStream, int[] indices, InputElement[] inputLayout, int vertexSize, BoundingBox bounds)
        {
            if (indices.Length > 0 && vertexCount > 0)
            {
                DataStream vS = vStream;
                vS.Position = 0;

                var vertices = new SlimDX.Direct3D11.Buffer(context.Device, vS, new BufferDescription()
                {
                    BindFlags = BindFlags.VertexBuffer,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None,
                    SizeInBytes = (int)vS.Length,
                    Usage = ResourceUsage.Default
                });


                DX11IndexedGeometry geom = new DX11IndexedGeometry(context);
                geom.VertexBuffer = vertices;
                using (var indexstream = new DataStream(indices.Length * 4, true, true))
                {
                    indexstream.WriteRange(indices);
                    indexstream.Position = 0;
                    geom.IndexBuffer = new DX11IndexBuffer(context, indexstream, false, true);
                }

                geom.InputLayout = inputLayout;
                geom.Topology = PrimitiveTopology.TriangleList;
                geom.VerticesCount = vertexCount;
                geom.VertexSize = vertexSize;
                geom.HasBoundingBox = true;
                geom.BoundingBox = bounds;

                return geom;
            }
            else
                return new DX11IndexedGeometry(context);
        }
    }
}
