using System;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.EX9;
using SceneGraph.Core;
using SlimDX.Direct3D9;
using System.Linq;
using System.Collections.Generic;

namespace VVVV.SceneGraph
{
    public class Info
    {
        public GraphNode Node { get; }
        public TextureInfo TextureInfo { get; }
        public ResourceToken Token { get; set; }
        public Info(GraphNode node, string intent)
        {
            Node = node;
            TextureInfo = (node.Element as MeshElement).Material.Textures.First(t => t.Intent.ToString() == intent);
        }
    }
    [PluginInfo(Name = "Material", Category = "SceneGraph", Version = "EX9",
                Help = "Returns colors and all available textures of the input GraphNode. Defaults to a white texture if not available.",
                Tags = "color, texture",
                Author = "woei")]
    public class MaterialEX9Node : MaterialNode<TextureResource<Info>, Device>
    {
        protected override TextureResource<Info> CreateTextureSlice(GraphNode node, string intent) => new TextureResource<Info>(
            new Info(node, intent), 
            CreateTexture, 
            null, 
            DestroyTexture);


        public Texture CreateTexture(Info info, Device device)
        {
            if (!FTokens.ContainsKey(device))
                FTokens.Add(device, new List<ResourceToken>());
            dynamic t;
            info.Token = info.Node.GetTexture(info.TextureInfo, device, out t);
            FTokens[device].Add(info.Token);
            return t;
        }

        public void DestroyTexture(Info info, Texture texture, DestroyReason args)
        {
            info.Token.Dispose();
        }
    }
}
