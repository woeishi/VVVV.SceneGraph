using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V2;

using SceneGraph.Core;
using SceneGraph.Core.Animations;

namespace VVVV.SceneGraph
{
    [PluginInfo(Name = "Animate", Category = "Transform", Version = "SceneGraph",
                Help = "Applies the selected animation or all animations of selected nodes or children.", Tags = "animation, matrix",
                Author = "woei")]
    public class AnimateNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins
#pragma warning disable 0649
        [Input("GraphNode")]
        IDiffSpread<GraphNode> FInput;

        [Input("Time")]
        IDiffSpread<float> FTime;

        [Input("Normalize Time")]
        IDiffSpread<bool> FNormalize;

        [Input("Recursion Depth", DefaultValue = -1, Visibility = PinVisibility.OnlyInspector)]
        IDiffSpread<int> FRecLimit;

        [Input("Matches | Contains")]
        IDiffSpread<bool> FToggleContains;

        [Input("Selector")]
        IDiffSpread<string> FSelector;

        [Output("GraphNode", AutoFlush = false)]
        ISpread<GraphNode> FOutput;

        [Output("Affected", AutoFlush = false, BinVisibility = PinVisibility.OnlyInspector)]
        ISpread<ISpread<string>> FSelectedName;

        [Output("Animation Names", AutoFlush = false)]
        ISpread<ISpread<string>> FAnimNames;

        Spread<Spread<GraphNode>> FSelected = new Spread<Spread<GraphNode>>(0);
#pragma warning restore
        #endregion fields & pins

        public void OnImportsSatisfied()
        {
            FOutput.SliceCount = 0;
            FOutput.Flush();
            FSelectedName.SliceCount = 0;
            FSelectedName.Flush();
            FAnimNames.SliceCount = 0;
            FAnimNames.Flush();
        }

        public void Evaluate(int spreadMax)
        {
            bool graphChanged = false;
            if (FInput.IsChanged || FRecLimit.IsChanged || FToggleContains.IsChanged || FSelector.IsChanged)
            {
                graphChanged = true;
                foreach (var s in FSelected)
                    foreach (var n in s)
                        n.ResetAnimation();

                foreach (var n in FOutput)
                    n?.DisposeGraph();

                FOutput.SliceCount = FInput.SliceCount;
                for (int i = 0; i < FInput.SliceCount; i++)
                    FOutput[i] = FInput[i] == null ? FInput[i] : GraphNode.CloneGraph(FInput[i]);

                spreadMax = FInput.CombineWith(FToggleContains).CombineWith(FSelector);
                FSelectedName.SliceCount = spreadMax;
                FSelected.SliceCount = spreadMax;
                FAnimNames.SliceCount = 0;
                for (int i = 0; i < spreadMax; i++)
                {
                    FSelected[i] = new Spread<GraphNode>(0);
                    FSelectedName[i] = new Spread<string>(0);
                    if (FInput[i] != null)
                    {
                        foreach (var n in FOutput[i].FindAnimation(FSelector[i].Trim(), FToggleContains[i], FRecLimit[i]))
                        {
                            var selected = n;
                            selected.ForkAnimation();
                            FSelected[i].Add(selected);
                            FSelectedName[i].Add(selected.Name);
                            FAnimNames.Add(n.AnimationsProxy.AnimationNames.ToSpread());
                        }
                    }
                }
                FSelectedName.Flush();
                FAnimNames.Flush();
                FOutput.Flush();
            }

            if (FTime.IsChanged || FNormalize.IsChanged || graphChanged)
                for (int i = 0; i < FSelected.SliceCount; i++)
                    foreach (var node in FSelected[i])
                        node?.Animate(FTime[i], FNormalize[i]);
        }
    }
}
