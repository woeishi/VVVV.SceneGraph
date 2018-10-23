#region usings
using System;
using System.Linq;
using System.Collections.Generic;

using System.ComponentModel.Composition;
using VVVV.PluginInterfaces.V2;

using SceneGraph.Core;

using SlimDX;
#endregion usings

namespace VVVV.SceneGraph
{
	public abstract class MaterialNode<T, U> : IPluginEvaluate, IPartImportsSatisfiedNotification, IDisposable, INestedNode
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

        protected bool FInvalidate;

        [Output("Bin Size", Order = 500)]
        ISpread<int> FBinSize;

        [Import]
        IPluginHost2 FHost;
        protected string FNodePath = string.Empty;

        protected Spread<GraphNode> FSelected = new Spread<GraphNode>();
        protected Dictionary<U, List<ResourceToken>> FTokens = new Dictionary<U, List<ResourceToken>>();

        [Import]
        IIOFactory FIOFactory;
        bool IsNested;
        public bool GraphChanged { get; set; }
        public int PinStartIndex = 100;
        protected Dictionary<string, IIOContainer<ISpread<T>>> FTexPins;
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
            FTexPins = new Dictionary<string, IIOContainer<ISpread<T>>>();
            FPathPins = new Dictionary<string, IIOContainer<ISpread<string>>>();
            int incr = PinStartIndex;
            foreach (var e in Enum.GetNames(typeof(TextureIntent)))
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
                FTexPins[key] = FIOFactory.CreateIOContainer<ISpread<T>>(new OutputAttribute($"{key} Texture") { Order = order });
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

        protected abstract T CreateTextureSlice(GraphNode node, string intent);

        public void Evaluate(int spreadMax)
		{
            if (string.IsNullOrEmpty(FNodePath))
                FNodePath = FHost.GetNodePath(false);

            FInvalidate = false;
            if (FGraphNode.IsChanged || (IsNested && GraphChanged))
            {
                FInvalidate = true;

                var input = IsNested ? FGraphNodeInternal : FGraphNode;

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

                        var material = FSelected[i].Material;
                        FMatID.Add(material.Index);
                        FAmbient.Add(material.AmbientColor);
                        FDiffuse.Add(material.DiffuseColor);
                        FSpecular.Add(material.SpecularColor);
                        FSpecPow.Add(material.SpecularPower);
                        FAvailableTex.Add(material.Textures.Select(e => e.Intent.ToString()).ToSpread());
                        FBinSize[i] = 1;
                    }
                    else
                        FBinSize[i] = 0;
                }
            }

            if (PinsChanged || FInvalidate)
            {
                PinsChanged = false;
                FInvalidate = true;

                foreach (var l in FTokens.Values)
                    foreach (var t in l)
                        t.Dispose();
                FTokens.Clear();

                foreach (var io in FTexPins)
                {
                    if (io.Value != null)
                        io.Value.IOObject.SliceCount = 0;
                    io.Value?.IOObject.ResizeAndDismiss(FSelected.SliceCount, (i) => CreateTextureSlice(FSelected[i], io.Key));
                }
                foreach (var io in FPathPins.Values)
                    io?.IOObject.ResizeAndDismiss(FSelected.SliceCount, () => string.Empty);

                foreach (var key in FPathPins.Keys)
                {
                    if (FPathPins[key] != null)
                    {
                        for (int i = 0; i < FSelected.SliceCount; i++)
                        {
                            var mat = FSelected[i].Material;
                            bool hit = false;
                            for (int t = 0; t < mat.Textures.Length; t++)
                            {
                                if (mat.Textures[t].Intent.ToString() == key)
                                {
                                    FPathPins[key].IOObject[i] = mat.Textures[t].FullPath;
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

        public void Dispose() => SafeDispose();

        public void SafeDispose()
        {
            foreach (var l in FTokens.Values)
                foreach (var t in l)
                    t.Dispose();
            FTokens.Clear();
        }
    }
}
