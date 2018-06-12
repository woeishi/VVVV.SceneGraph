using System;
using System.Collections.Generic;

using AssimpNet;
using SlimDX;
using SlimDX.Direct3D11;
using FeralTic.DX11;
using FeralTic.DX11.Resources;

namespace SceneGraph.DX11
{
    internal static class DX11Utils
    {
        internal static DX11IndexedGeometry LoadMesh(DX11RenderContext context, AssimpMesh mesh)
        {
            List<int> inds = mesh.Indices;

            if (inds.Count > 0 && mesh.VerticesCount > 0)
            {
                DataStream vS = mesh.Vertices;
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
                using (var indexstream = new DataStream(inds.Count * 4, true, true))
                {
                    indexstream.WriteRange(inds.ToArray());
                    indexstream.Position = 0;
                    geom.IndexBuffer = new DX11IndexBuffer(context, indexstream, false, true);
                }
                    
                geom.InputLayout = mesh.GetInputElements().ToArray();
                geom.Topology = PrimitiveTopology.TriangleList;
                geom.VerticesCount = mesh.VerticesCount;
                geom.VertexSize = mesh.CalculateVertexSize();
                geom.HasBoundingBox = true;
                geom.BoundingBox = mesh.BoundingBox;

                return geom;
            }
            else
                return new DX11IndexedGeometry(context);
        }

        internal static IEnumerable<DX11IndexedGeometry> LoadGeometry(AssimpScene scene, List<int> meshIds, DX11RenderContext context)
        {
            foreach (int i in meshIds)
            {
                var assimpmesh = scene.Meshes[i];
                yield return LoadMesh(context, assimpmesh);
            }
        }
    }
}
