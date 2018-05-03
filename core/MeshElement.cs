using System;
using System.Collections.Generic;

using AssimpNet;
using FeralTic.DX11;
using FeralTic.DX11.Resources;
using SceneGraph.DX11;

namespace SceneGraph.Core
{
    public class MeshElement : Element
    {
        public int MeshCount => Node.MeshCount;

        public int[] MeshIDs { get; }
        public AssimpMesh[] Meshes { get; }
        DX11ResourceHandler<DX11IndexedGeometry>[] FMeshHandlers;

        public int[] MaterialIDs { get; }
        public AssimpMaterial[] Materials { get; }
        DX11ResourceHandler<DX11Texture2D>[][] FTextureHandlers;

        internal MeshElement(Scene scene, AssimpNode self, int id) : base(scene, self, id)
        {
            MeshIDs = self.MeshIndices.ToArray();
            Meshes = new AssimpMesh[self.MeshCount];
            FMeshHandlers = new DX11ResourceHandler<DX11IndexedGeometry>[self.MeshCount];

            MaterialIDs = new int[self.MeshCount];
            Materials = new AssimpMaterial[self.MeshCount];
            FTextureHandlers = new DX11ResourceHandler<DX11Texture2D>[self.MeshCount][];
            for (int i = 0; i < self.MeshCount; i++)
            {
                Meshes[i] = scene.Meshes[self.MeshIndices[i]];
                FMeshHandlers[i] = scene.MeshHandlers[self.MeshIndices[i]];

                var matId = Meshes[i].MaterialIndex;
                MaterialIDs[i] = matId;
                Materials[i] = scene.Materials[matId];
                FTextureHandlers[i] = scene.TextureHandlers[matId];
            }
        }

        #region Mesh
        internal DX11IndexedGeometry GetGeometry(DX11RenderContext context, int meshSlot, string nodePath)
        {
            if (meshSlot < FMeshHandlers.Length)
                return FMeshHandlers[meshSlot].Get(context, nodePath);
            else
                return null;
        }

        internal void ReleaseGeometry(string nodePath, DX11RenderContext context = null, int meshSlot = -1)
        {
            if (meshSlot > -1 && meshSlot < FMeshHandlers.Length)
                FMeshHandlers[meshSlot].Release(nodePath, context);
            else if (meshSlot < 0)
                foreach (var mh in FMeshHandlers)
                    mh.Release(nodePath);    
        }
        #endregion Mesh

        #region Texture
        internal DX11Texture2D GetTexture(DX11RenderContext context, int meshSlot, int textureSlot, string nodePath)
        {
            if (meshSlot < FTextureHandlers.Length && textureSlot < FTextureHandlers[meshSlot].Length)
            {
                return FTextureHandlers[meshSlot][textureSlot].Get(context, nodePath);
            }
            else
                return context.DefaultTextures.WhiteTexture;
            
        }

        internal void ReleaseTexture(string nodePath, DX11RenderContext context = null, int meshSlot = -1, int textureSlot = -1)
        {
            if (meshSlot < FTextureHandlers.Length)
            {
                List<DX11ResourceHandler<DX11Texture2D>> collect = new List<DX11ResourceHandler<DX11Texture2D>>();
                if (meshSlot < 0)
                    foreach (var mth in FTextureHandlers)
                        ReleaseByTexSlot(mth, textureSlot, nodePath, context);
                else
                    ReleaseByTexSlot(FTextureHandlers[meshSlot], textureSlot, nodePath, context);
            }
        }
        void ReleaseByTexSlot(DX11ResourceHandler<DX11Texture2D>[] texHandlers, int textureSlot, string nodePath, DX11RenderContext context)
        {
            if (textureSlot < 0)
            {
                foreach (var th in texHandlers)
                    th.Release(nodePath, context);
            }
            else if (textureSlot < texHandlers.Length)
                texHandlers[textureSlot].Release(nodePath, context);
        }
        #endregion Texture
    }
}
