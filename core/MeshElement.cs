using System;
using System.Collections.Generic;

using AssimpNet;
using FeralTic.DX11;
using FeralTic.DX11.Resources;
using SceneGraph.DX11;

namespace SceneGraph.Core
{
    public class MeshContainerElement : Element
    {
        public int[] MeshIDs { get; }

        internal MeshContainerElement(Scene scene, AssimpNode self, int id) : base(scene, self, id)
        {
            this.ChildCount += self.MeshCount;

            MeshIDs = self.MeshIndices.ToArray();
        }
    }

    public class MeshElement : Element
    {
        public int MeshID { get; }
        public AssimpMesh Mesh { get; }
        DX11ResourceHandler<DX11IndexedGeometry> FMeshHandler;

        public int MaterialID { get; }
        public AssimpMaterial Material { get; }
        DX11ResourceHandler<DX11Texture2D>[] FTextureHandler;

        internal MeshElement(Scene scene, AssimpNode self, int id, int meshId) : base(scene, self, id)
        {
            this.Name = $"Mesh_{meshId}";

            MeshID = meshId;
            for (int i = 0; i < self.MeshCount; i++)
            {
                Mesh = scene.Meshes[MeshID];
                FMeshHandler = scene.MeshHandlers[MeshID];

                MaterialID = Mesh.MaterialIndex;
                Material = scene.Materials[MaterialID];
                FTextureHandler = scene.TextureHandlers[MaterialID];
            }
        }

        #region Mesh
        internal DX11IndexedGeometry GetGeometry(DX11RenderContext context, string nodePath)
        {
            return FMeshHandler.Get(context, nodePath);
        }

        internal void ReleaseGeometry(string nodePath, DX11RenderContext context = null)
        {
            FMeshHandler.Release(nodePath, context);
        }
        #endregion Mesh

        #region Texture
        internal DX11Texture2D GetTexture(DX11RenderContext context, int textureSlot, string nodePath)
        {
            if (textureSlot < FTextureHandler.Length)
                return FTextureHandler[textureSlot].Get(context, nodePath);
            else
                return context.DefaultTextures.WhiteTexture;

        }

        internal void ReleaseTexture(string nodePath, DX11RenderContext context = null, int textureSlot = -1)
        {
            if (textureSlot < 0)
            {
                foreach (var th in FTextureHandler)
                    th.Release(nodePath, context);
            }
            else if (textureSlot < FTextureHandler.Length)
                FTextureHandler[textureSlot].Release(nodePath, context);
        }
        #endregion Texture
    }
}
