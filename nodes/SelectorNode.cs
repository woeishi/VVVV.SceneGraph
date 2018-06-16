using System;
using System.Linq;
using System.Collections.Generic;

using System.ComponentModel.Composition;
using VVVV.PluginInterfaces.V2;

using SceneGraph.Core;

namespace VVVV.SceneGraph
{
    [PluginInfo(Name = "Selector", Category = "SceneGraph",
                Help = "Returns GraphNodes of which the names match the selector string.", Tags = "get, filter, sift",
                Author = "woei")]
    public class SelectorNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins
        #pragma warning disable 0649
        [Input("Graph In")]
        IDiffSpread<GraphNode> FInput;

        [Input("Recursion Depth", DefaultValue = 0)]
        IDiffSpread<int> FRecLimit;

        [Input("Include Self", DefaultBoolean = true)]
        IDiffSpread<bool> FIncludeSelf;

        [Input("Matches | Contains")]
        IDiffSpread<bool> FToggleContains;

        [Input("Selector")]
        IDiffSpread<string> FSelector;
        
        [Output("Graph Out", AutoFlush = false, BinVisibility = PinVisibility.OnlyInspector)]
        ISpread<ISpread<GraphNode>> FOutput;

        [Output("Selected", AutoFlush = false, BinVisibility = PinVisibility.OnlyInspector)]
        ISpread<ISpread<string>> FSelectedName;

        [Output("Success")]
        ISpread<bool> FSuccess;
        #pragma warning restore
        #endregion fields & pins

        public void OnImportsSatisfied()
        {
            FOutput.SliceCount = 0;
            FOutput.Flush();
        }

        public void Evaluate(int spreadMax)
        {
            if (FInput.IsChanged || FRecLimit.IsChanged || FIncludeSelf.IsChanged || FSelector.IsChanged || FToggleContains.IsChanged)
            {
                FOutput.SliceCount = spreadMax;
                FSelectedName.SliceCount = spreadMax;
                FSuccess.SliceCount = spreadMax;
                for (int i = 0; i < spreadMax; i++)
                {
                    FSelectedName[i] = new Spread<string>(0);
                    if (!string.IsNullOrWhiteSpace(FSelector[i]))
                    {
                        var selected = FInput[i]?.FindNodes(FSelector[i].Trim(), FToggleContains[i]);
                        FOutput[i].SliceCount = 0;
                        if (selected != null)
                        {
                            foreach (var n in selected)
                            {
                                var exp = n.ExpandNodes(FRecLimit[i]);
                                FOutput[i].AddRange(exp);
                                FSelectedName[i].AddRange(exp.Select(e => e.Name));
                            }
                        }
                        FSuccess[i] = FOutput[i].SliceCount > 0;
                    }
                    else
                    {
                        if (FInput[i] != null)
                        {
                            var exp = FInput[i].ExpandNodes(FRecLimit[i]);
                            FOutput[i].AssignFrom(exp);
                            FSelectedName[i].AddRange(exp.Select(e => e.Name));
                        }
                        else
                            FOutput[i].SliceCount = 0;
                        FSuccess[i] = true;
                    }
                }
                FOutput.Flush();
                FSelectedName.Flush();
            }
        }
    }
}
