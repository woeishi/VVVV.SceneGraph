using System.Linq;
using VVVV.PluginInterfaces.V2;

using SceneGraph.Core;

using FeralTic.DX11;
using FeralTic.DX11.Resources;
using VVVV.DX11;

namespace VVVV.SceneGraph
{
	[PluginInfo(Name = "Material", Category = "SceneGraph", Version = "DX11",
	            Help = "Returns colors and all available textures of the input GraphNode. Defaults to a white texture if not available.",
                Tags = "color, texture",
	            Author = "woei")]
	public class MaterialDX11Node : MaterialNode<DX11Resource<DX11Texture2D>>, IDX11ResourceHost
    {
        protected override DX11Resource<DX11Texture2D> CreateTextureSlice(string intent, int i) => new DX11Resource<DX11Texture2D>();

        protected override void DisposeTextureSlice(string intent, int i, DX11Resource<DX11Texture2D> slice)
        {
            var mat = (FSelected[i].Element as MeshElement).Material;
            for (int t = 0; t < mat.Textures.Length; t++)
            {
                if (mat.Textures[t].Intent.ToString() == intent)
                {
                    FSelected[i].ReleaseTexture(FNodePath, i, mat.Textures[t]);
                    FSelected[i].PurgeTextures();
                }
            }
        }

        public void Update(DX11RenderContext context)
        {
            FInvalidate |= (!FTexPins.Values.Where(p => p != null).SelectMany(p => p.IOObject).All(io => io.Contains(context)));
            if (FInvalidate) 
            {
                foreach (var key in FTexPins.Keys)
                {
                    if (FTexPins[key] != null)
                    {
                        for (int i = 0; i < FSelected.SliceCount; i++)
                        {
                            var mat = (FSelected[i].Element as MeshElement).Material;
                            TextureInfo ti = null;
                            for (int t = 0; t < mat.Textures.Length; t++)
                            {
                                if (mat.Textures[t].Intent.ToString() == key)
                                {
                                    
                                    ti = mat.Textures[t];
                                    break;
                                }
                            }
                            FTexPins[key].IOObject[i][context] = FSelected[i].GetTexture(ti, FNodePath, i, context);
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

            for (int i = 0; i < FSelected.SliceCount; i++)
            {
                FSelected[i].ReleaseTexture(FNodePath, i, context: context);
                FSelected[i].PurgeTextures();
            }
        }
    }
}
