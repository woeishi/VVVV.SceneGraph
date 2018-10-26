using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V2;

using SceneGraph.Core;

namespace VVVV.SceneGraph
{
    [PluginInfo(Name = "Animation", Category = "Transform", Version = "SceneGraph XPath",
                Help = "Applies transformation on selected nodes sampled from animation information of the given time.", Tags = "matrix, query, xpath",
                Author = "woei")]
    public class AnimationXPathNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins
        #pragma warning disable 0649
        [Input("GraphNode")]
        IDiffSpread<GraphNode> FInput;

        [Input("Time")]
        IDiffSpread<float> FTime;

        [Input("Normalize Time")]
        IDiffSpread<bool> FNormalize;

        [Input("Animation Index", Visibility = PinVisibility.OnlyInspector)]
        IDiffSpread<int> FIndex;

        [Input("XPath", DefaultString = ".//*")]
        IDiffSpread<string> FQuery;

        [Output("GraphNode", AutoFlush = false)]
        ISpread<GraphNode> FOutput;

        [Output("Affected", AutoFlush = false, BinVisibility = PinVisibility.OnlyInspector)]
        ISpread<ISpread<string>> FSelectedName;

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
                            foreach (var n in FOutput[i].XPathQuery(FQuery[i].Trim()))
                            {
                                if (n.Tracks.Count > 0)
                                {
                                    var selected = n;
                                    selected.ForkAnimation();
                                    FSelected[i].Add(selected);
                                    FSelectedName[i].Add(selected.Name);
                                }
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
                FOutput.Flush();
            }

            if (FTime.IsChanged || FNormalize.IsChanged || FIndex.IsChanged || graphChanged)
                for (int i = 0; i < FSelected.SliceCount; i++)
                    foreach (var node in FSelected[i])
                        node?.Animate(FTime[i], FNormalize[i], FIndex[i]);
        }
    }
}
