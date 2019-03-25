using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

using VVVV.PluginInterfaces.V2;

using SceneGraph.Core;

namespace VVVV.SceneGraph
{
    [PluginInfo(Name = "ModifyColor", Category = "SceneGraph", Version = "XPath",
                Help = "Applies color parameter changes on the selected mesh instances in the graph.", Tags = "Color, Alpha",
                Author = "woei")]
    public class ModifyColorXpathNode : ModifyColorBase
    {
        #region fields & pins
        #pragma warning disable 0649
        [Input("XPath", DefaultString = ".//*", Order = 100)]
        IDiffSpread<string> FQuery;

        [Output("Error Message", Order = 100)]
        ISpread<string> FError;

        [Output("Success", Order = 101)]
        ISpread<bool> FSuccess;
        #pragma warning restore
        #endregion fields & pins

        protected override void ImportsSatisfied()
        {
            FError.SliceCount = 0;
            FSuccess.SliceCount = 0;
        }

        protected override bool InputChanged() => FQuery.IsChanged;

        protected override int SpreadMax() => FQuery.SliceCount;

        protected override void SetOutputSliceCount(int spreadMax)
        {
            FError.SliceCount = spreadMax;
            FSuccess.SliceCount = spreadMax;
        }

        protected override bool SliceIsNotValid(int i) => string.IsNullOrWhiteSpace(FQuery[i]);

        protected override void SetNotValidSlice(int i)
        {
            FError[i] = "Input is invalid";
            FSuccess[i] = false;
        }

        protected override IEnumerable<GraphNode> SelectNodes(GraphNode node, int i)
        {
            try
            {
                var selected = node.XPathQuery(FQuery[i].Trim()).Where(n => n.Material != null);
                FError[i] = string.Empty;
                FSuccess[i] = true;
                return selected;
            }
            catch (Exception e)
            {
                FError[i] = e.Message;
                FSuccess[i] = false;
                return new GraphNode[0];
            }
        }
    }
}
