#region usings
using System;
using System.Linq;
using System.Collections.Generic;

using System.ComponentModel.Composition;
using VVVV.PluginInterfaces.V2;

using SceneGraph.Core;

using SlimDX;
#endregion usings

namespace VVVV.SceneGraph
{
	[PluginInfo(Name = "Camera", Category = "SceneGraph", 
	            Help = "Returns a View Matrix and Projection properties of a Camera type GraphNode",
	            Author = "woei")]
	public class CameraNode : IPluginEvaluate
    {
        #region fields & pins
        #pragma warning disable 0649
        [Input("GraphNode")]
        IDiffSpread<GraphNode> FGraphNode;

        [Output("Name", Order = 0)]
        ISpread<string> FName;

        [Output("View", Order = 1)]
        ISpread<Matrix> FView;
       
        [Output("Near Plane", Order = 2)]
        ISpread<float> FNear;

        [Output("Far Plane", Order = 3)]
        ISpread<float> FFar;

        [Output("Aspect", Order = 4)]
        ISpread<float> FAspect;

        [Output("Bin Size", Order = 5)]
        ISpread<int> FBinSize;

        Spread<GraphNode> FSelected = new Spread<GraphNode>();
        #pragma warning restore
        #endregion fields & pins

        public void Evaluate(int spreadMax)
		{
            if (FGraphNode.IsChanged)
            {
                FSelected.SliceCount = 0;
                FName.SliceCount = 0;
                FView.SliceCount = 0;
                FNear.SliceCount = 0;
                FFar.SliceCount = 0;
                FAspect.SliceCount = 0;
                FBinSize.SliceCount = FGraphNode.SliceCount;
                for (int i = 0; i < FGraphNode.SliceCount; i++)
                {
                    if (FGraphNode[i] != null && (FGraphNode[i].Element is CameraElement))
                    {
                        FSelected.Add(FGraphNode[i]);
                        var camera = (CameraElement)FGraphNode[i].Element;
                        FName.Add(FGraphNode[i].Name);
                        //FView.Add(camera.View);
                        FNear.Add(camera.NearPlane);
                        FFar.Add(camera.FarPlane);
                        FAspect.Add(camera.AspectRatio);
                        FBinSize[i] = 1;
                    }
                    else
                        FBinSize[i] = 0;
                }
                FView.SliceCount = FSelected.SliceCount;
            }

            for (int i = 0; i < FSelected.SliceCount; i++)
                FView[i] = FSelected[i].Accumulated;
        }
    }
}
