using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using SlimDX;

using SceneGraph.Core;
using SceneGraph.Core.Animations;

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
            name = name.Replace("$", "_");
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

        internal static Animation ToAnimation(this AssimpAnimation animation)
        {
            var channels = animation.Channels.SelectMany(c => c.ToChannels());
            return Animation.Create(animation.Name, (float)animation.Duration, (float)animation.TicksPerSecond, channels);
        }

        internal static IEnumerable<IChannel> ToChannels(this AssimpAnimationChannel channel)
        {
            var scales = channel.ScalingKeys.Select(ak => new Marker<Vector3>((float)ak.Time, ak.Value)).ToList();
            var rotations = channel.RotationKeys.Select((ak, i) => {
                var val = ak.Value;
                if (val == Zero)
                    if (scales.Count > i && scales[i].Value == Vector3.Zero)
                        val = Quaternion.Identity;
                return new Marker<Quaternion>((float)ak.Time, val);
            });
            yield return scales.ToScale();
            yield return rotations.ToRotation();
            yield return channel.PositionKeys.Select(ak => new Marker<Vector3>((float)ak.Time, ak.Value)).ToTranslation();
        }
    }
}
