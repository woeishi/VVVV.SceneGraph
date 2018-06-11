#region usings
using System;
using System.Linq;
using System.Collections.Generic;

using System.ComponentModel.Composition;
using VVVV.PluginInterfaces.V2;

using SceneGraph.Core;
using AssimpNet;
using SlimDX;

using FeralTic.DX11;
using FeralTic.DX11.Resources;
using VVVV.DX11;
#endregion usings


namespace VVVV.SceneGraph
{
	[PluginInfo(Name = "Material", Category = "SceneGraph", Version = "DX11",
	            Help = "Returns colors and all available textures of the input GraphNode. Defaults to a white texture if not available.",
                Tags = "color, texture",
	            Author = "woei")]
	public class MaterialNode : IPluginEvaluate, IPartImportsSatisfiedNotification, IDisposable, IDX11ResourceHost, INestedNode
    {
        #region fields & pins
        #pragma warning disable 0649
        [Input("GraphNode")]
        IDiffSpread<GraphNode> FGraphNode;
        [Input("GraphNodeInternal", Visibility = PinVisibility.False)]
        ISpread<GraphNode> FGraphNodeInternal;

        [Output("Material ID", Order = 93)]
        ISpread<int> FMatID;

        [Output("Ambient", Order = 94)]
        ISpread<Color4> FAmbient;
        [Output("Diffuse", Order = 95)]
        ISpread<Color4> FDiffuse;
        [Output("Specular", Order = 96)]
        ISpread<Color4> FSpecular;
        [Output("Specular Power", Order = 97)]
        ISpread<float> FSpecPow;
        [Output("Available Textures", Order = 98, BinOrder = 99)]
        ISpread<ISpread<string>> FAvailableTex;

        bool FInvalidate;

        [Output("Bin Size", Order = 500)]
        ISpread<int> FBinSize;

        [Import]
        IPluginHost2 FHost;
        string FNodePath = string.Empty;

        Spread<GraphNode> FSelected = new Spread<GraphNode>();

        [Import]
        IIOFactory FIOFactory;
        bool IsNested;
        public bool GraphChanged { get; set; }
        public int PinStartIndex = 100;
        Dictionary<string, IIOContainer<ISpread<DX11Resource<DX11Texture2D>>>> FTexPins;
        Dictionary<string, IIOContainer<ISpread<string>>> FPathPins;
        bool PinsChanged = false;
        #pragma warning restore
        #endregion fields & pins

        public void OnImportsSatisfied()
        {
            FSelected.SliceCount = 0;
            FAvailableTex.SliceCount = 0;
            IsNested = !FGraphNodeInternal.GetType().Namespace.Contains("VVVV.Hosting");

            if (IsNested)
                FHost.GetPin("Bin Size").Visibility = PinVisibility.False;

            CreatePinToggles(FIOFactory);
        }

        void CreatePinToggles(IIOFactory factory)
        {
            FTexPins = new Dictionary<string, IIOContainer<ISpread<DX11Resource<DX11Texture2D>>>>();
            FPathPins = new Dictionary<string, IIOContainer<ISpread<string>>>();
            int incr = PinStartIndex;
            foreach (var e in Enum.GetNames(typeof(eAssimpTextureType)))
            {
                FTexPins.Add(e, null);
                FPathPins.Add(e, null);

                var attr = new ConfigAttribute($"Enable {e} Texture");
                if (e == "Diffuse" && (!IsNested))
                    attr.DefaultBoolean = true;

                var toggle = factory.CreateDiffSpread<bool>(attr);
                var order = incr;
                toggle.Changed += (s) => ConfigToggled(s[0], e, order);

                incr += 2;
            }
        }

        private void ConfigToggled(bool toggle, string key, int order)
        {
            if (toggle && FTexPins[key] == null)
            {
                FTexPins[key] = FIOFactory.CreateIOContainer<ISpread<DX11Resource<DX11Texture2D>>>(new OutputAttribute($"{key} Texture") { Order = order });
                FTexPins[key].IOObject.SliceCount = 0;
                FPathPins[key] = FIOFactory.CreateIOContainer<ISpread<string>>(new OutputAttribute($"{key} Texture Path") { Order = order + 1 });
                FPathPins[key].IOObject.SliceCount = 0;
            }
            else if (FTexPins[key] != null)
            {
                FTexPins[key].Dispose();
                FTexPins[key] = null;
                FPathPins[key].Dispose();
                FPathPins[key] = null;
            }
            PinsChanged = true;
        }

        public void Evaluate(int spreadMax)
		{
            if (string.IsNullOrEmpty(FNodePath))
                FNodePath = FHost.GetNodePath(false);

            FInvalidate = false;
            if (FGraphNode.IsChanged || (IsNested && GraphChanged))
            {
                FInvalidate = true;

                var input = IsNested ? FGraphNodeInternal : FGraphNode;

                var trash = new Spread<GraphNode>(FSelected.SliceCount);
                trash.AssignFrom(FSelected);
                FSelected.SliceCount = 0;
                FMatID.SliceCount = 0;
                FAmbient.SliceCount = 0;
                FDiffuse.SliceCount = 0;
                FSpecular.SliceCount = 0;
                FSpecPow.SliceCount = 0;
                FAvailableTex.SliceCount = 0;
                FBinSize.SliceCount = input.SliceCount;
                for (int i = 0; i < input.SliceCount; i++)
                {
                    if (input[i] != null && (input[i].Element is MeshElement))
                    {
                        FSelected.Add(input[i]);
                        trash.RemoveAll(t => t.ID == input[i].ID);

                        var element = (MeshElement)FSelected[i].Element;
                        FMatID.Add(element.MaterialID);
                        FAmbient.Add(element.Material.AmbientColor);
                        FDiffuse.Add(element.Material.DiffuseColor);
                        FSpecular.Add(element.Material.SpecularColor);
                        FSpecPow.Add(element.Material.SpecularPower);
                        FAvailableTex.Add(element.Material.TextureType.Select(e => e.ToString()).ToSpread());
                        FBinSize[i] = 1;
                    }
                    else
                        FBinSize[i] = 0;
                }
                foreach (var t in trash)
                {
                    (t.Element as MeshElement).ReleaseTexture(FNodePath);
                    (t.Element as MeshElement).PurgeTextures();
                }
            }

            if (PinsChanged || FInvalidate)
            {
                PinsChanged = false;
                FInvalidate = true;

                foreach (var io in FTexPins.Values)
                    io?.IOObject.ResizeAndDismiss(FAmbient.SliceCount, () => new DX11Resource<DX11Texture2D>());
                foreach (var io in FPathPins.Values)
                    io?.IOObject.ResizeAndDismiss(FAmbient.SliceCount, () => string.Empty);

                
                for (int i = 0; i < FSelected.SliceCount; i++)
                {
                    if (FSelected[i].Element is MeshElement)
                    {
                        var mat = (FSelected[i].Element as MeshElement).Material;
                        foreach (var key in FPathPins.Keys)
                        {
                            if (FPathPins[key] != null)
                            {
                                bool hit = false;
                                for (int t = 0; t < mat.TextureType.Count; t++)
                                {
                                    if (mat.TextureType[t].ToString() == key)
                                    {
                                        FPathPins[key].IOObject[i] = System.IO.Path.Combine(FSelected[i].Scene.AssetRoot,mat.TexturePath[t]);
                                        hit = true;
                                        break;
                                    }
                                }
                                if (!hit)
                                    FPathPins[key].IOObject[i] = string.Empty;
                            }
                        }
                        
                    }
                }
            }
        }

        public void Dispose()
        {
            foreach (var n in FSelected)
            {
                (n.Element as MeshElement).ReleaseTexture(FNodePath);
                (n.Element as MeshElement).PurgeTextures();
            }
        }

        public void Update(DX11RenderContext context)
        {
            FInvalidate |= (!FTexPins.Values.Where(p => p != null).SelectMany(p => p.IOObject).All(io => io.Contains(context)));
            if (FInvalidate) // || (!FDiffuseTexture.All(r => r.Contains(context))))
            {
                int incr = 0;
                foreach (var n in FSelected)
                {
                    try
                    {
                        var me = n.Element as MeshElement;
                        foreach (var key in FTexPins.Keys)
                        {
                            if (FTexPins[key] != null)
                            {
                                var slot = 1000;
                                for (int t = 0; t < me.Material.TextureType.Count; t++)
                                {
                                    if (me.Material.TextureType[t].ToString() == key)
                                    {
                                        slot = t;
                                        break;
                                    }
                                }
                                FTexPins[key].IOObject[incr][context] = me.GetTexture(context, slot, FNodePath);
                            }
                        }
                        incr++;
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine(e);
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

            foreach (var n in FSelected)
            {
                (n.Element as MeshElement).ReleaseTexture(FNodePath, context);
                (n.Element as MeshElement).PurgeTextures();
            }
        }
    }
}
