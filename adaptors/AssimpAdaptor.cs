using System;
using System.Text.RegularExpressions;
using SceneGraph.Core;
using SlimDX;
using AssimpNet;

namespace SceneGraph.Adaptors
{
    internal static class AssimpAdaptor
    {
        static string ParseName(this string nodeName)
        {
            var bytes = System.Text.Encoding.Default.GetBytes(nodeName);
            var name = System.Text.Encoding.UTF8.GetString(bytes);
            if (Regex.IsMatch(name, "^\\d"))
                name = "_" + name;
            return name;
        }

        internal static Element ToElement(this AssimpNode node, int id)
        {
            var name = node.Name.ParseName();
            return new Element(id, name, node.MeshCount + node.Children.Count);
        }

        internal static CameraElement ToCamera(this AssimpNode node, int id, AssimpCamera camera)
        {
            var name = node.Name.ParseName();
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
    }
}
