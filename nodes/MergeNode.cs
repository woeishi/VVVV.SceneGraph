#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V2;

using SceneGraph.Core;
#endregion usings


namespace VVVV.SceneGraph
{
    [PluginInfo(Name = "Merge", Category = "SceneGraph",
                Help = "Applies a transformation subgraph back onto it's scenegraph.", Tags = "join",
                Author = "woei")]
    public class MergeNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins
        #pragma warning disable 0649
        [Input("GraphNode")]
        IDiffSpread<GraphNode> FInput;

        [Input("GraphNode Part")]
        IDiffSpread<GraphNode> FPart;

        [Output("GraphNode", AutoFlush = false)]
        ISpread<GraphNode> FOutput;

        [Output("Error")]
        ISpread<string> FError;

        Spread<GraphNode> FSelected = new Spread<GraphNode>(0);
        #pragma warning restore
        #endregion fields & pins

        public void OnImportsSatisfied()
        {
            FOutput.Flush();
        }

        public void Evaluate(int spreadMax)
        {
            if (FInput.IsChanged || FPart.IsChanged)
            {
                foreach (var n in FOutput)
                    n?.DisposeGraph();

                FOutput.SliceCount = FInput.SliceCount;
                FError.SliceCount = spreadMax;
                FSelected.SliceCount = 0;
                for (int i = 0; i < spreadMax; i++)
                {
                    if (i < FInput.SliceCount)
                        FOutput[i] = FInput[i]??FPart[i];
                    if (FInput[i] != null && FPart[i] != null)
                    {
                        if (FOutput[i].Scene != FPart[i].Scene)
                            FError[i] = "inputs from different scenes, use cons";
                        else if (FOutput[i].ID > FPart[i].ID)
                            FError[i] = "part is not a subtree of input";
                        else
                        {
                            if (i < FInput.SliceCount)
                                FOutput[i] = GraphNode.CloneGraph(FInput[i]);
                            var mergePoint = FOutput[i].Merge(FPart[i]);
                            FSelected.Add(mergePoint);
                            FError[i] = string.Empty;
                        }
                    }
                    else
                        FError[i] = "some invalid graph input";
                }
                FOutput.Flush();
            }
        }
    }
}