#region usings
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V2;

using SceneGraph.Core;
using SlimDX;
#endregion usings

namespace VVVV.SceneGraph
{
    [PluginInfo(Name = "Transform", Category = "SceneGraph",
                Help = "Returns the current transformation of the GraphNode.", Tags = "matrix",
                Author = "woei")]
    public class TransformNode : IPluginEvaluate, IPartImportsSatisfiedNotification, INestedNode
    {
        #region fields & pins
        #pragma warning disable 0649
        [Input("GraphNode")]
        IDiffSpread<GraphNode> FGraphNode;
        [Input("GraphNodeInternal", Visibility = PinVisibility.False)]
        ISpread<GraphNode> FGraphNodeInternal;

        [Output("Transform", Order = 49)]
        ISpread<Matrix> FTransform;

        [Import]
        IPluginHost2 FHost;
        string FNodePath = string.Empty;

        Spread<GraphNode> FSelected = new Spread<GraphNode>();

        [Import]
        IIOFactory FIOFactory;
        bool IsNested;
        public bool GraphChanged { get; set; }
        bool PinsChanged;

        public int PinStartIndex = 50;
        IIOContainer<ISpread<Matrix>> FLocal;
        IIOContainer<ISpread<Matrix>> FAncestral;
#pragma warning restore
        #endregion fields & pins

        public void SafeDispose() { }

        public void OnImportsSatisfied()
        {
            FSelected.SliceCount = 0;
            IsNested = !FGraphNodeInternal.GetType().Namespace.Contains("VVVV.Hosting");

            var attr = new ConfigAttribute($"Enable Local and Parent") { DefaultBoolean = !IsNested };
            var toggle = FIOFactory.CreateDiffSpread<bool>(attr);
            toggle.Changed += (s) => ToggleHierachical(s[0]);
        }

        void ToggleHierachical(bool toggle)
        {
            if (toggle & FLocal == null)
            {
                FLocal = FIOFactory.CreateIOContainer<ISpread<Matrix>>(new OutputAttribute($"Local") { Order = PinStartIndex });
                FAncestral = FIOFactory.CreateIOContainer<ISpread<Matrix>>(new OutputAttribute($"Ancestral") { Order = PinStartIndex+1 });
                PinsChanged = true;
            }
            else if (FLocal != null)
            {
                FLocal.Dispose();
                FLocal = null;
                FAncestral.Dispose();
                FAncestral = null;
                PinsChanged = true;
            }
        }

        public void Evaluate(int spreadMax)
        {
            if (string.IsNullOrEmpty(FNodePath))
                FNodePath = FHost.GetNodePath(false);

            if (FGraphNode.IsChanged || (IsNested && GraphChanged) || PinsChanged)
            {
                PinsChanged = false;
                var input = IsNested ? FGraphNodeInternal : FGraphNode;

                FSelected.AssignFrom(input);
                FTransform.SliceCount = FSelected.SliceCount;
                if (FLocal != null)
                {
                    FLocal.IOObject.SliceCount = FSelected.SliceCount;
                    FAncestral.IOObject.SliceCount = FSelected.SliceCount;
                }
            }

            for (int i = 0; i < FSelected.SliceCount; i++)
            {
                if (FSelected[i] != null && FSelected[i].Element != null)
                {
                    FSelected[i].ResolveTransformGraph();
                    FTransform[i] = FSelected[i].Accumulated;
                    if (FLocal != null)
                    {
                        FLocal.IOObject[i] = FSelected[i].Local;
                        FAncestral.IOObject[i] = FSelected[i].Parent?.Accumulated ?? Matrix.Identity;
                    }
                }
                else
                {
                    FTransform[i] = Matrix.Identity;
                    if (FLocal != null)
                    {
                        FLocal.IOObject[i] = Matrix.Identity;
                        FAncestral.IOObject[i] = Matrix.Identity;
                    }
                }
            }
        }
    }
}