﻿using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V2;

using SlimDX;
using SceneGraph.Core;
using System.Linq;
using System.Collections.Generic;

namespace VVVV.SceneGraph
{
    public abstract class ModifyColorBase : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins
        #pragma warning disable 0649
        [Config("Enable Ambient")]
        IDiffSpread<bool> FTogAmbient;

        [Config("Enable Diffuse", DefaultBoolean = true)]
        IDiffSpread<bool> FTogDiffuse;

        [Config("Enable Specular")]
        IDiffSpread<bool> FTogSpecular;

        [Config("Enable Specular Power")]
        IDiffSpread<bool> FTogSpecPower;


        [Input("GraphNode")]
        IDiffSpread<GraphNode> FInput;

        IIOContainer<IDiffSpread<Color4>> FAmbientContainer;
        IIOContainer<IDiffSpread<Vector4>> FAAmountContainer;
        IIOContainer<IDiffSpread<MaterialModification.BlendMode>> FAModeContainer;

        IIOContainer<IDiffSpread<Color4>> FDiffuseContainer;
        IIOContainer<IDiffSpread<Vector4>> FDAmountContainer;
        IIOContainer<IDiffSpread<MaterialModification.BlendMode>> FDModeContainer;

        IIOContainer<IDiffSpread<Color4>> FSpecularContainer;
        IIOContainer<IDiffSpread<Vector4>> FSAmountContainer;
        IIOContainer<IDiffSpread<MaterialModification.BlendMode>> FSModeContainer;

        IIOContainer<IDiffSpread<float>> FSpecPowerContainer;
        IIOContainer<IDiffSpread<float>> FSPAmountContainer;
        IIOContainer<IDiffSpread<MaterialModification.BlendMode>> FSPModeContainer;


        [Output("GraphNode", AutoFlush = false)]
        ISpread<GraphNode> FOutput;

        [Output("Affected", AutoFlush = false, BinVisibility = PinVisibility.OnlyInspector)]
        ISpread<ISpread<string>> FSelectedName;

        Spread<Spread<GraphNode>> FSelected = new Spread<Spread<GraphNode>>(0);

        [Import]
        IIOFactory FIOFactory;

        Color4 defaultColor = new Color4();
        Vector4 defaultAmount = Vector4.Zero;
        #pragma warning restore
        #endregion fields & pins

        public void OnImportsSatisfied()
        {
            FTogAmbient.Changed += FTogAmbient_Changed;
            FTogDiffuse.Changed += FTogDiffuse_Changed;
            FTogSpecular.Changed += FTogSpecular_Changed;
            FTogSpecPower.Changed += FTogSpecPower_Changed;
            FOutput.SliceCount = 0;
            FOutput.Flush();
            FSelectedName.SliceCount = 0;
            FSelectedName.Flush();
            ImportsSatisfied();
        }

        void FTogAmbient_Changed(IDiffSpread<bool> spread)
        {
            if (spread[0])
            {
                FAmbientContainer = FIOFactory.CreateIOContainer<IDiffSpread<Color4>>(new InputAttribute("Ambient") { Order = 10 });
                FAAmountContainer = FIOFactory.CreateIOContainer<IDiffSpread<Vector4>>(new InputAttribute("Ambient Amount") { Order = 11 });
                FAModeContainer = FIOFactory.CreateIOContainer<IDiffSpread<MaterialModification.BlendMode>>(new InputAttribute("Ambient Blend Mode") { Order = 12 });
            }
            else
            {
                FAmbientContainer?.Dispose();
                FAAmountContainer?.Dispose();
                FAModeContainer?.Dispose();
            }
        }

        bool AmbientIsChanged()
        {
            if (FTogAmbient[0])
                return FAmbientContainer.IOObject.IsChanged || FAAmountContainer.IOObject.IsChanged || FAModeContainer.IOObject.IsChanged;
            else
                return FTogAmbient.IsChanged;
        }

        void FTogDiffuse_Changed(IDiffSpread<bool> spread)
        {
            if (spread[0])
            {
                FDiffuseContainer = FIOFactory.CreateIOContainer<IDiffSpread<Color4>>(new InputAttribute("Diffuse") { Order = 20 });
                FDAmountContainer = FIOFactory.CreateIOContainer<IDiffSpread<Vector4>>(new InputAttribute("Diffuse Amount") { Order = 21 });
                FDModeContainer = FIOFactory.CreateIOContainer<IDiffSpread<MaterialModification.BlendMode>>(new InputAttribute("Diffuse Blend Mode") { Order = 22 });
            }
            else
            {
                FDiffuseContainer?.Dispose();
                FDAmountContainer?.Dispose();
                FDModeContainer?.Dispose();
            }
        }

        bool DiffuseIsChanged()
        {
            if (FTogDiffuse[0])
                return FDiffuseContainer.IOObject.IsChanged || FDAmountContainer.IOObject.IsChanged || FDModeContainer.IOObject.IsChanged;
            else
                return FTogDiffuse.IsChanged;
        }

        void FTogSpecular_Changed(IDiffSpread<bool> spread)
        {
            if (spread[0])
            {
                FSpecularContainer = FIOFactory.CreateIOContainer<IDiffSpread<Color4>>(new InputAttribute("Specular") { Order = 30 });
                FSAmountContainer = FIOFactory.CreateIOContainer<IDiffSpread<Vector4>>(new InputAttribute("Specular Amount") { Order = 31 });
                FSModeContainer = FIOFactory.CreateIOContainer<IDiffSpread<MaterialModification.BlendMode>>(new InputAttribute("Specular Blend Mode") { Order = 32 });
            }
            else
            {
                FSpecularContainer?.Dispose();
                FSAmountContainer?.Dispose();
                FSModeContainer?.Dispose();
            }
        }

        bool SpecularIsChanged()
        {
            if (FTogSpecular[0])
                return FSpecularContainer.IOObject.IsChanged || FSAmountContainer.IOObject.IsChanged || FSModeContainer.IOObject.IsChanged;
            else
                return FTogSpecular.IsChanged;
        }

        void FTogSpecPower_Changed(IDiffSpread<bool> spread)
        {
            if (spread[0])
            {
                FSpecPowerContainer = FIOFactory.CreateIOContainer<IDiffSpread<float>>(new InputAttribute("Specular Power") { Order = 40 });
                FSPAmountContainer = FIOFactory.CreateIOContainer<IDiffSpread<float>>(new InputAttribute("Specular Power Amount") { Order = 41 });
                FSPModeContainer = FIOFactory.CreateIOContainer<IDiffSpread<MaterialModification.BlendMode>>(new InputAttribute("Specular Power Blend Mode") { Order = 42 });
            }
            else
            {
                FSpecPowerContainer?.Dispose();
                FSPAmountContainer?.Dispose();
                FSPModeContainer?.Dispose();
            }
        }

        bool SpecularPowerIsChanged()
        {
            if (FTogSpecPower[0])
                return FSpecPowerContainer.IOObject.IsChanged || FSPAmountContainer.IOObject.IsChanged || FSPModeContainer.IOObject.IsChanged;
            else
                return FTogSpecPower.IsChanged;
        }

        protected virtual void ImportsSatisfied() { }

        protected abstract bool InputChanged();

        protected abstract int SpreadMax();

        protected virtual void SetOutputSliceCount(int spreadMax) { }

        protected virtual bool SliceIsNotValid(int i) => false;

        protected virtual void SetNotValidSlice(int i) { }

        protected abstract IEnumerable<GraphNode> SelectNodes(GraphNode node, int i);

        public void Evaluate(int spreadMax)
        {
            bool graphChanged = false;
            if (FInput.IsChanged || InputChanged())
            {
                graphChanged = true;
                foreach (var n in FOutput)
                    n?.DisposeGraph();

                FOutput.SliceCount = FInput.SliceCount;

                for (int i = 0; i < FInput.SliceCount; i++)
                    FOutput[i] = FInput[i] == null ? FInput[i] : GraphNode.CloneGraph(FInput[i]);

                spreadMax = SpreadMax().CombineWith(FInput);
                FSelectedName.SliceCount = spreadMax;
                FSelected.SliceCount = spreadMax;
                SetOutputSliceCount(spreadMax);
                for (int i = 0; i < spreadMax; i++)
                {
                    FSelected[i] = new Spread<GraphNode>(0);
                    FSelectedName[i] = new Spread<string>(0);
                    if (FInput[i] == null || SliceIsNotValid(i))
                        SetNotValidSlice(i);
                    else
                    {
                        var selected = SelectNodes(FOutput[i], i);
                        FSelected[i].AddRange(selected);
                        FSelectedName[i].AddRange(selected.Select(e => e.Name));
                    }
                }
                FSelectedName.Flush();
                FOutput.Flush();
            }

            if (FInput.IsChanged || graphChanged ||
                AmbientIsChanged() || DiffuseIsChanged() || SpecularIsChanged() || SpecularPowerIsChanged())
            {
                for (int i = 0; i < FSelected.SliceCount; i++)
                {
                    var ac = FTogAmbient[0] ? FAmbientContainer.IOObject[i] : defaultColor;
                    var aa = FTogAmbient[0] ? FAAmountContainer.IOObject[i] : defaultAmount;
                    var am = FTogAmbient[0] ? FAModeContainer.IOObject[i] : MaterialModification.BlendMode.Add;

                    var dc = FTogDiffuse[0] ? FDiffuseContainer.IOObject[i] : defaultColor;
                    var da = FTogDiffuse[0] ? FDAmountContainer.IOObject[i] : defaultAmount;
                    var dm = FTogDiffuse[0] ? FDModeContainer.IOObject[i] : MaterialModification.BlendMode.Add;

                    var sc = FTogSpecular[0] ? FSpecularContainer.IOObject[i] : defaultColor;
                    var sa = FTogSpecular[0] ? FSAmountContainer.IOObject[i] : defaultAmount;
                    var sm = FTogSpecular[0] ? FSModeContainer.IOObject[i] : MaterialModification.BlendMode.Add;

                    var sp = FTogSpecPower[0] ? FSpecPowerContainer.IOObject[i] : 0.0f;
                    var spa = FTogSpecPower[0] ? FSPAmountContainer.IOObject[i] : 0.0f;
                    var spm = FTogSpecPower[0] ? FSPModeContainer.IOObject[i] : MaterialModification.BlendMode.Add;

                    foreach (var node in FSelected[i])
                        node?.Modify(ac, aa, am, dc, da, dm, sc, sa, sm, sp, spa, spm);
                }
                FOutput.Flush(true);
            }
        }
    }
}
