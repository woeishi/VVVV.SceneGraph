using System;
using System.Text.RegularExpressions;
using SceneGraph.Core;
using SlimDX;
using AssimpNet;
using SceneGraph.Core.Animation;

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

        internal static Matrix Validate(this Matrix m)
        {
            bool faulted = m.M11 == 0 && m.M12 == 0 && m.M13 == 0;
            faulted &= m.M21 == 0 && m.M22 == 0 && m.M23 == 0;
            faulted &= m.M31 == 0 && m.M32 == 0 && m.M33 == 0;
            return faulted ? Matrix.Identity : m;
        }

        static Quaternion Zero = new Quaternion(0, 0, 1, 0);

        internal static Channel ToTrack(this AssimpAnimationChannel channel, AssimpAnimation stack)
        {
            var result = new Channel(stack.Name, (float)stack.Duration, (float)stack.TicksPerSecond);
            foreach (var p in channel.PositionKeys)
                result.AppendPosition((float)p.Time, p.Value);
            foreach (var s in channel.ScalingKeys)
                result.AppendScale((float)s.Time, s.Value);
            int i = 0;
            foreach (var r in channel.RotationKeys)
            {
                var val = r.Value;
                if (val == Zero)
                    if (result.Scale.Count > i && result.Scale[i] == Vector3.Zero)
                        val = Quaternion.Identity;
                result.AppendRoatation((float)r.Time, val);
                i++;
            }
            return result;
        }
    }
}
