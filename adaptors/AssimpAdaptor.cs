using System;
using SceneGraph.Core;
using SlimDX;
using AssimpNet;
using FeralTic.DX11;
using FeralTic.DX11.Resources;

namespace SceneGraph.Adaptors
{
    internal static class AssimpAdaptor
    {
        internal static Element ToElement(this AssimpNode node, int id)
        {
            var bytes = System.Text.Encoding.Default.GetBytes(node.Name);
            var name = System.Text.Encoding.UTF8.GetString(bytes);
            return new Element(id, name, node.MeshCount + node.Children.Count);
        }

        internal static CameraElement ToCamera(this AssimpNode node, int id, AssimpCamera camera)
        {
            var bytes = System.Text.Encoding.Default.GetBytes(node.Name);
            var name = System.Text.Encoding.UTF8.GetString(bytes);
            var view = Matrix.LookAtLH(camera.Position, camera.LookAt, camera.UpVector);
            return new CameraElement(id, name, view, camera.NearPlane, camera.FarPlane, camera.AspectRatio);
        }

        internal static MeshElement ToMesh(this AssimpNode node, int id, IScene scene, int meshId)
        {
            var meshInfo = scene.MeshInfos[meshId];
            return new MeshElement(id, scene, meshInfo, scene.MaterialInfos[meshInfo.MaterialIndex]);
        }

        internal static MeshInfo ToMeshInfo(this AssimpMesh mesh, int id)
        {
            return new MeshInfo()
            {
                Index = id,
                BoneMatrices = mesh.BoneMatrices,
                BoneNames = mesh.BoneNames,
                BoundingBox = mesh.BoundingBox,
                ColorChannelCount = mesh.ColorChannelCount,
                HasNormals = mesh.HasNormals,
                MaterialIndex = mesh.MaterialIndex,
                MaxBonePerVertex = mesh.MaxBonePerVertex,
                UvChannelCount = mesh.UvChannelCount,
                VerticesCount = mesh.VerticesCount
            };
        }

        internal static MaterialInfo ToMaterialInfo(this AssimpMaterial material, int id)
        {
            var mat = new MaterialInfo()
            {
                Index = id,
                AmbientColor = material.AmbientColor,
                DiffuseColor = material.DiffuseColor,
                SpecularColor = material.SpecularColor,
                SpecularPower = material.SpecularPower,
                Textures = new TextureInfo[material.TexturePath.Count]
            };
            for (int i = 0; i < mat.Textures.Length; i++)
            {
                mat.Textures[i] = new TextureInfo()
                {
                    Path = material.TexturePath[i],
                    Intent = (TextureIntent)Enum.Parse(typeof(TextureIntent), material.TextureType[i].ToString())
                };
            }
            return mat;
        }

        internal static DX11IndexedGeometry GetGeometry(this GraphNode node, DX11RenderContext context, string nodePath)
        {
            var me = (node.Element as MeshElement);
            return (node.Scene as SceneAssimp).MeshHandlers[me.MeshID].Get(context, nodePath);
        }

        internal static void ReleaseGeometry(this GraphNode node, string nodePath, DX11RenderContext context = null)
        {
            var me = (node.Element as MeshElement);
            (node.Scene as SceneAssimp).MeshHandlers[me.MeshID].Release(nodePath, context);
        }

        internal static void PurgeGeometry(this GraphNode node)
        {
            var me = (node.Element as MeshElement);
            (node.Scene as SceneAssimp).MeshHandlers[me.MeshID].Purge();
        }

        internal static DX11Texture2D GetTexture(this GraphNode node, DX11RenderContext context, int textureSlot, string nodePath)
        {
            var me = (node.Element as MeshElement);
            if (textureSlot < me.Material.Textures.Length)
                return (node.Scene as SceneAssimp).TextureHandlers[me.MaterialID][textureSlot].Get(context, nodePath);
            else
                return context.DefaultTextures.WhiteTexture;

        }

        internal static void ReleaseTexture(this GraphNode node, string nodePath, DX11RenderContext context = null, int textureSlot = -1)
        {
            var me = (node.Element as MeshElement);
            if (textureSlot < 0)
            {
                foreach (var th in (node.Scene as SceneAssimp).TextureHandlers[me.MaterialID])
                    th.Release(nodePath, context);
            }
            else if (textureSlot < me.Material.Textures.Length)
                (node.Scene as SceneAssimp).TextureHandlers[me.MaterialID][textureSlot].Release(nodePath, context);
        }

        internal static void PurgeTextures(this GraphNode node)
        {
            var me = (node.Element as MeshElement);
            foreach (var th in (node.Scene as SceneAssimp).TextureHandlers[me.MaterialID])
                th.Purge();
        }
    }
}
