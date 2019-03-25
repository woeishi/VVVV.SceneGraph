using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V2;

using SceneGraph.Core;

namespace VVVV.SceneGraph
{
    [PluginInfo(Name = "ModifyColor", Category = "SceneGraph",
                Help = "Applies color parameter changes on the selected mesh instances in the graph.", Tags = "Color, Alpha",
                Author = "woei")]
    public class ModifyColorNode: ModifyColorBase
    {
        #region fields & pins
        #pragma warning disable 0649
        [Input("Recursion Depth", DefaultValue = -1, Visibility = PinVisibility.OnlyInspector, Order = 100)]
        IDiffSpread<int> FRecLimit;

        [Input("Matches | Contains", Order = 101)]
        IDiffSpread<bool> FToggleContains;

        [Input("Selector", Order = 102)]
        IDiffSpread<string> FSelector;
        #pragma warning restore
        #endregion fields & pins

        protected override bool InputChanged() 
            => FRecLimit.IsChanged || FToggleContains.IsChanged || FSelector.IsChanged;

        protected override int SpreadMax()
            => FRecLimit.CombineWith(FToggleContains).CombineWith(FSelector);

        protected override IEnumerable<GraphNode> SelectNodes(GraphNode node, int i)
        {
            var selected = node.FindNodes(FSelector[i].Trim(), FToggleContains[i]);
            if (selected != null)
                foreach (var n in selected)
                    foreach (var m in n.ExpandMeshNodes(FRecLimit[i]))
                        yield return m;
        }
    }
}
