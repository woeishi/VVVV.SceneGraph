using System.Linq;
using VVVV.PluginInterfaces.V2;

using SceneGraph.Core;

using FeralTic.DX11;
using FeralTic.DX11.Resources;
using VVVV.DX11;

using System.Collections.Generic;

namespace VVVV.SceneGraph
{
	[PluginInfo(Name = "Material", Category = "SceneGraph", Version = "DX11",
	            Help = "Returns colors and all available textures of the input GraphNode. Defaults to a white texture if not available.",
                Tags = "color, texture",
	            Author = "woei")]
	public class MaterialDX11Node : MaterialNode<DX11Resource<DX11Texture2D>, DX11RenderContext>, IDX11ResourceHost
    {
        protected override DX11Resource<DX11Texture2D> CreateTextureSlice(GraphNode node, string intent) => new DX11Resource<DX11Texture2D>();

        public void Update(DX11RenderContext context)
        {
            if (FInvalidate || (!FTokens.ContainsKey(context))) 
            {
                if (FTokens.ContainsKey(context))
                {
                    foreach (var t in FTokens[context])
                        t.Dispose();
                    FTokens[context].Clear();
                }
                else
                    FTokens.Add(context, new List<ResourceToken>());

                foreach (var key in FTexPins.Keys)
                {
                    if (FTexPins[key] != null)
                    {
                        for (int i = 0; i < FSelected.SliceCount; i++)
                        {
                            var mat = FSelected[i].Material;
                            TextureInfo ti = null;
                            for (int t = 0; t < mat.Textures.Length; t++)
                            {
                                if (mat.Textures[t].Intent.ToString() == key)
                                {
                                    ti = mat.Textures[t];
                                    break;
                                }
                            }

                            dynamic tex;
                            FTokens[context].Add(FSelected[i].GetTexture(ti, context, out tex));
                            FTexPins[key].IOObject[i][context] = tex;
                        }
                    }
                }
            }
        }

        public void Destroy(DX11RenderContext context, bool force)
        {
            foreach (var pins in FTexPins.Values)
                if (pins?.IOObject != null)
                    foreach (var s in pins.IOObject)
                        s.Remove(context);

            foreach (var t in FTokens[context])
                t.Dispose();
            FTokens.Remove(context);
        }
    }
}
