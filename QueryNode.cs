using System;
using System.ComponentModel.Composition;
using VVVV.PluginInterfaces.V2;

using SceneGraph.Core;
using System.Collections.Generic;
using System.Linq;

namespace VVVV.SceneGraph
{
    [PluginInfo(Name = "Query", Category = "SceneGraph",
                Help = "Returns GraphNodes through an XPath query.", Tags = "get, filter, xpath",
                Author = "woei")]
    public class QueryNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins
        #pragma warning disable 0649
        [Input("Graph In")]
        IDiffSpread<GraphNode> FInput;

        [Input("XPath", DefaultString = ".")]
        IDiffSpread<string> FSelector;

        [Output("Graph Out", AutoFlush = false, BinVisibility = PinVisibility.OnlyInspector)]
        ISpread<ISpread<GraphNode>> FOutput;

        [Output("Selected", AutoFlush = false, BinVisibility = PinVisibility.OnlyInspector)]
        ISpread<ISpread<string>> FSelectedName;

        [Output("Error Message")]
        ISpread<string> FError;

        [Output("Success")]
        ISpread<bool> FSuccess;
        #pragma warning restore
        #endregion fields & pins

        public void OnImportsSatisfied()
        {
            FOutput.SliceCount = 0;
            FOutput.Flush();
            FSelectedName.SliceCount = 0;
            FSelectedName.Flush();
        }

        public void Evaluate(int spreadMax)
        {
            if (FInput.IsChanged || FSelector.IsChanged)
            {
                FOutput.SliceCount = spreadMax;
                FSelectedName.ResizeAndDismiss(spreadMax, () => new Spread<string>(0));
                FError.SliceCount = spreadMax;
                FSuccess.SliceCount = spreadMax;
                for (int i = 0; i < spreadMax; i++)
                {
                    if (FInput[i] == null)
                    {
                        FOutput[i].SliceCount = 0;
                        FSelectedName[i].SliceCount = 0;
                        FError[i] = "Input is null";
                        FSuccess[i] = false;
                    }
                    else 
                    {
                        try
                        {
                            var selected = FInput[i]?.XPathQuery(FSelector[i].Trim());
                            
                            FOutput[i].AssignFrom(selected);
                            FSelectedName[i].AssignFrom(selected.Select(e => e.Name));
                            
                            FError[i] = string.Empty;
                            FSuccess[i] = true;
                        }
                        catch (Exception e)
                        {
                            FOutput[i].SliceCount = 0;
                            FSelectedName[i].SliceCount = 0;
                            FError[i] = e.Message;
                            FSuccess[i] = false;
                        }
                    }
                }
                FOutput.Flush();
                FSelectedName.Flush();
            }
        }
    }
}
