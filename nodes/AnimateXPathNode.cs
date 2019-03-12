using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V2;

using SceneGraph.Core;
using SceneGraph.Core.Animations;

namespace VVVV.SceneGraph
{
    [PluginInfo(Name = "Animate", Category = "Transform", Version = "SceneGraph XPath",
                Help = "Applies the animation selected directly or via its containing node.", Tags = "matrix, query, xpath",
                Author = "woei")]
    public class AnimateXPathNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins
        #pragma warning disable 0649
        [Input("GraphNode")]
        IDiffSpread<GraphNode> FInput;

        [Input("Time")]
        IDiffSpread<float> FTime;

        [Input("Normalize Time")]
        IDiffSpread<bool> FNormalize;

        [Input("XPath", DefaultString = ".//*")]
        IDiffSpread<string> FQuery;

        [Output("GraphNode", AutoFlush = false)]
        ISpread<GraphNode> FOutput;

        [Output("Affected", AutoFlush = false, BinVisibility = PinVisibility.OnlyInspector)]
        ISpread<ISpread<string>> FSelectedName;

        [Output("Animation Names", AutoFlush = false)]
        ISpread<ISpread<string>> FAnimNames;

        [Output("Error Message")]
        ISpread<string> FError;

        [Output("Success")]
        ISpread<bool> FSuccess;

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
            if (FInput.IsChanged || FQuery.IsChanged)
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

                spreadMax = FInput.CombineWith(FQuery);
                FSelectedName.SliceCount = spreadMax;
                FSelected.SliceCount = spreadMax;
                FAnimNames.SliceCount = 0;
                FError.SliceCount = spreadMax;
                FSuccess.SliceCount = spreadMax;
                for (int i = 0; i < spreadMax; i++)
                {
                    FSelected[i] = new Spread<GraphNode>(0);
                    FSelectedName[i] = new Spread<string>(0);
                    if (FInput[i] == null || string.IsNullOrWhiteSpace(FQuery[i]))
                    {
                        FSelected[i].SliceCount = 0;
                        FSelectedName[i].SliceCount = 0;
                        FError[i] = "Input is invalid";
                        FSuccess[i] = false;
                    }
                    else
                    {
                        try
                        {
                            foreach (var n in FOutput[i].XPathQueryAnimation(FQuery[i].Trim()))
                            {
                                var selected = n;
                                selected.ForkAnimation();
                                FSelected[i].Add(selected);
                                FSelectedName[i].Add(selected.Name);
                                FAnimNames.Add(n.AnimationsProxy.AnimationNames.ToSpread());
                            }
                            FError[i] = string.Empty;
                            FSuccess[i] = true;
                        }
                        catch (Exception e)
                        {
                            FSelected[i].SliceCount = 0;
                            FSelectedName[i].SliceCount = 0;
                            FError[i] = e.Message;
                            FSuccess[i] = false;
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
