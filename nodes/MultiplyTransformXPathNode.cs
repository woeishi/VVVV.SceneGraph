using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V2;

using SlimDX;
using SceneGraph.Core;

namespace VVVV.SceneGraph
{
    [PluginInfo(Name = "Multiply", Category = "Transform", Version = "SceneGraph XPath",
                Help = "Multiplies the given transformation on the selected nodes in the graph.", Tags = "matrix, query, xpath",
                Author = "woei")]
    public class MultiplyTransformXPathNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins
        #pragma warning disable 0649
        [Input("Transform")]
        IDiffSpread<Matrix> FTransform;

        [Input("GraphNode")]
        IDiffSpread<GraphNode> FInput;

        [Input("XPath", DefaultString = ".")]
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
                foreach (var n in FOutput)
                    n?.DisposeGraph();

                FOutput.SliceCount = FInput.SliceCount;
                for (int i = 0; i < FInput.SliceCount; i++)
                    FOutput[i] = FInput[i]==null?FInput[i]:GraphNode.CloneGraph(FInput[i]);

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
                        FError[i] = "Input is invalid";
                        FSuccess[i] = false;
                    }
                    else
                    {
                        try
                        {
                            foreach (var n in FOutput[i].XPathQuery(FQuery[i]))
                            {
                                var selected = n;
                                selected.Fork();
                                FSelected[i].Add(selected);
                                FSelectedName[i].Add(selected.Name);
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
            
            if (FTransform.IsChanged || graphChanged)
                for (int i = 0; i < FSelected.SliceCount; i++)
                    foreach(var node in FSelected[i])
                        node?.Transform(FTransform[i]);
        }
    }
}
