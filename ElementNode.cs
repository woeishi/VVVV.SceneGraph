#region usings
using System;
using System.Linq;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V2;

using SceneGraph.Core;
#endregion usings


namespace VVVV.SceneGraph
{
    [PluginInfo(Name = "Element", Category = "SceneGraph",
                Help = "Outputs basic information, parent and children.", Tags = "split",
                Author = "woei")]
    public class ElementNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins
        #pragma warning disable 0649
        [Input("GraphNode")]
        IDiffSpread<GraphNode> FGraphNode;

        [Input("GraphNodeInternal", Visibility = PinVisibility.False)]
        ISpread<GraphNode> FGraphNodeInternal;

        [Output("Name")]
        ISpread<string> FName;

        [Output("Type")]
        ISpread<string> FType;

        [Output("Parent", AutoFlush = false)]
        ISpread<GraphNode> FParent;
        [Output("Parent Name", AutoFlush = false)]
        ISpread<string> FParentName;

        [Output("Children", AutoFlush = false)]
        ISpread<ISpread<GraphNode>> FChildren;
        [Output("Children Names", AutoFlush = false)]
        ISpread<ISpread<string>> FChildrenNames;

        [Output("Scene Filename")]
        ISpread<string> FScenePath;

        bool IsNested;
        #pragma warning restore
        #endregion fields & pins

        public void OnImportsSatisfied()
        {
            FGraphNodeInternal.SliceCount = 0;
            FParent.SliceCount = 0;
            FChildren.SliceCount = 0;

            IsNested = !FGraphNodeInternal.GetType().Namespace.Contains("VVVV.Hosting");

            FParent.Flush();
            FParentName.Flush();
            FChildren.Flush();
            FChildrenNames.Flush();
        }

        public void Evaluate(int spreadMax)
        {
            if (FGraphNode.IsChanged || IsNested)
            {
                var input = IsNested ? FGraphNodeInternal : FGraphNode;
                FName.SliceCount = input.SliceCount;
                FType.SliceCount = input.SliceCount;
                FParent.SliceCount = input.SliceCount;
                FParentName.SliceCount = input.SliceCount;
                FChildren.SliceCount = input.SliceCount;
                FChildrenNames.SliceCount = input.SliceCount;
                FScenePath.SliceCount = input.SliceCount;
                for (int i = 0; i < input.SliceCount; i++)
                {
                    FName[i] = input[i]?.Name??string.Empty;
                    FType[i] = input[i]?.Element.GetType().Name??string.Empty;
                    FParent[i] = input[i]?.Parent ?? input[i];
                    FParentName[i] = input[i]?.Parent?.Name ?? string.Empty;
                    FChildren[i].AssignFrom(input[i]?.Children);
                    FScenePath[i] = input[i]?.Scene.Filename??string.Empty;
                    FChildrenNames[i].AssignFrom(input[i]?.Children.Select(c => c?.Name??string.Empty).ToSpread()??new Spread<string>(0));
                }

                FParent.Flush();
                FParentName.Flush();
                FChildren.Flush();
                FChildrenNames.Flush();
            }
        }
    }
}