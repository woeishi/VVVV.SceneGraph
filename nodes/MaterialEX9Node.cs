using System;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.EX9;
using SceneGraph.Core;
using SlimDX.Direct3D9;

namespace VVVV.SceneGraph
{
    public class Info
    {
        public string Intent { get; }
        public int Index { get; }
        public Info(string intent, int index)
        {
            Intent = intent;
            Index = index;
        }
    }
    [PluginInfo(Name = "Material", Category = "SceneGraph", Version = "EX9",
                Help = "Returns colors and all available textures of the input GraphNode. Defaults to a white texture if not available.",
                Tags = "color, texture",
                Author = "woei")]
    public class MaterialEX9Node : MaterialNode<TextureResource<Info>>
    {
        protected override TextureResource<Info> CreateTextureSlice(string intent, int i) => new TextureResource<Info>(new Info(intent,i), CreateTexture, null, DestroyTexture);

        protected override void DisposeTextureSlice(string intent, int i, TextureResource<Info> slice) => slice.Dispose();

        public Texture CreateTexture(Info info, Device device)
        {
            var material = (FSelected[info.Index].Element as MeshElement).Material;
            
            TextureInfo ti = null;
            for (int t = 0; t < material.Textures.Length; t++)
            {
                if (material.Textures[t].Intent.ToString() == info.Intent)
                {
                    ti = material.Textures[t];
                    break;
                }
            }
            return FSelected[info.Index].GetTexture(ti, FNodePath, info.Index, device);
        }

        public void DestroyTexture(Info info, Texture texture, DestroyReason args)
        {
            var material = (FSelected[info.Index].Element as MeshElement).Material;
            for (int t = 0; t < material.Textures.Length; t++)
            {
                if (material.Textures[t].Intent.ToString() == info.Intent)
                {
                    FSelected[info.Index].ReleaseTexture(FNodePath, info.Index, material.Textures[t], texture.Device);
                    FSelected[info.Index].PurgeTextures();
                    break;
                }
            }
        }
    }
}
