using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V2;

using SlimDX;
using SceneGraph.Core;

namespace VVVV.SceneGraph
{
    [PluginInfo(Name = "Multiply", Category = "Transform", Version = "SceneGraph",
                Help = "Multiplies the given transformation on the selected nodes in the graph.", Tags = "matrix",
                Author = "woei")]
    public class MultiplyTransformNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins
        #pragma warning disable 0649
        [Input("Transform")]
        IDiffSpread<Matrix> FTransform;

        [Input("GraphNode")]
        IDiffSpread<GraphNode> FInput;

        [Input("Matches | Contains")]
        IDiffSpread<bool> FToggleContains;

        [Input("Selector")]
        IDiffSpread<string> FSelector;

        [Output("GraphNode", AutoFlush = false)]
        ISpread<GraphNode> FOutput;

        [Output("Affected", AutoFlush = false, BinVisibility = PinVisibility.OnlyInspector)]
        ISpread<ISpread<string>> FSelectedName;

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
            if (FInput.IsChanged || FSelector.IsChanged || FToggleContains.IsChanged)
            {
                FOutput.SliceCount = FInput.SliceCount;
                for (int i = 0; i < FInput.SliceCount; i++)
                    FOutput[i] = FInput[i]==null?FInput[i]:GraphNode.CloneGraph(FInput[i]);

                spreadMax = FInput.CombineWith(FSelector);
                FSelected.SliceCount = spreadMax;
                FSelectedName.SliceCount = spreadMax;
                for (int i = 0; i < spreadMax; i++)
                {
                    FSelected[i] = new Spread<GraphNode>(0);
                    FSelectedName[i] = new Spread<string>(0);
                    if (FInput[i] != null)
                    {
                        GraphNode selected = null;
                        if (!string.IsNullOrWhiteSpace(FSelector[i]))
                        {
                            foreach (var n in FOutput[i].FindNodes(FSelector[i], FToggleContains[i]))
                            {
                                selected = n;
                                selected.Fork();
                                FSelected[i].Add(selected);
                                FSelectedName[i].Add(selected.Name);
                            }
                        }
                        else
                        {
                            selected = FOutput[i];
                            selected.Fork();
                            FSelected[i].Add(selected);
                            FSelectedName[i].Add(selected.Name);
                        }
                    }
                }
                FSelectedName.Flush();
                FOutput.Flush();
            }
            
            for (int i = 0; i < FSelected.SliceCount; i++)
                foreach (var node in FSelected[i])
                    node?.Transform(FTransform[i]);
        }
    }
}
