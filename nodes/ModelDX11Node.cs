using System;
using System.Linq;

using System.ComponentModel.Composition;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Hosting.IO;

using SceneGraph.Core;
using FeralTic.DX11;
using VVVV.DX11;

namespace VVVV.SceneGraph
{
    [PluginInfo(Name = "Model", Category = "SceneGraph", Version = "DX11",
                Help = "Returns meshes contained in the input together with the corresponding transformations, material properties and textures.",
                Tags = "geometry, mesh, material",
                Author = "woei")]
    public class ModelDX11Node : IPluginEvaluate, IPartImportsSatisfiedNotification, IDisposable, IDX11ResourceHost
    {
        #region fields & pins
        #pragma warning disable 0649
        [Input("GraphNode")]
        IDiffSpread<GraphNode> FGraphNode;

        [Input("Recursion Depth", DefaultValue = -1)]
        IDiffSpread<int> FRecLimit;

        [Output("Name", Order = -10)]
        ISpread<string> FName;

        Spread<GraphNode> FSelected = new Spread<GraphNode>(0);

        [Import]
        IOFactory FIOFactory;
        PluginContainer FMesh;
        PluginContainer FTransform;
        PluginContainer FMaterial;
        #pragma warning restore
        #endregion fields & pins

        public void OnImportsSatisfied()
        {
            FName.SliceCount = 0;

            FSelected.SliceCount = 0;

            var nodeInfo = FIOFactory.NodeInfos.First(n => n.Name == "Mesh" && n.Category == "SceneGraph" && n.Version == "DX11");
            FMesh = FIOFactory.CreatePlugin(nodeInfo, c => c.IOAttribute.Name == "GraphNodeInternal", c => FSelected);
            FIOFactory.Configuring += (o, e) => ((IPlugin)FMesh).Configurate((IPluginConfig)e.PluginConfig);

            nodeInfo = FIOFactory.NodeInfos.First(n => n.Name == "Transform" && n.Category == "SceneGraph");
            FTransform = FIOFactory.CreatePlugin(nodeInfo, c => c.IOAttribute.Name == "GraphNodeInternal", c => FSelected);
            FIOFactory.Configuring += (o, e) => ((IPlugin)FTransform).Configurate((IPluginConfig)e.PluginConfig);

            nodeInfo = FIOFactory.NodeInfos.First(n => n.Name == "Material" && n.Category == "SceneGraph" && n.Version == "DX11");
            FMaterial = FIOFactory.CreatePlugin(nodeInfo, c => c.IOAttribute.Name == "GraphNodeInternal", c => FSelected);
            FIOFactory.Configuring += (o, e) => ((IPlugin)FMaterial).Configurate((IPluginConfig)e.PluginConfig);
        }

        public void Evaluate(int spreadMax)
        {
            bool graphChanged = false;
            if (FGraphNode.IsChanged || FRecLimit.IsChanged)
            {
                FSelected.SliceCount = 0;
                FName.SliceCount = 0;
                for (int i = 0; i < spreadMax; i++)
                {
                    if (FGraphNode[i] != null)
                    {
                        foreach (var n in FGraphNode[i].ExpandMeshNodes(FRecLimit[i]))
                        {
                            FName.Add(n.Name);
                            FSelected.Add(n);
                        }
                    }
                }
                graphChanged = true;
            }

            (FMesh.PluginBase as INestedNode).GraphChanged = graphChanged;
            FMesh.Evaluate(spreadMax);
            (FTransform.PluginBase as INestedNode).GraphChanged = graphChanged;
            FTransform.Evaluate(spreadMax);
            (FMaterial.PluginBase as INestedNode).GraphChanged = graphChanged;
            FMaterial.Evaluate(spreadMax);
        }

        public void Dispose()
        {
            (FMesh.PluginBase as INestedNode).SafeDispose();
            (FTransform.PluginBase as INestedNode).SafeDispose();
            (FMaterial.PluginBase as INestedNode).SafeDispose();

            FMesh.Dispose();
            FTransform.Dispose();
            FMaterial.Dispose();
        }

        public void Update(DX11RenderContext context)
        {
            (FMesh.PluginBase as IDX11ResourceHost).Update(context);
            (FMaterial.PluginBase as IDX11ResourceHost).Update(context);
        }

        public void Destroy(DX11RenderContext context, bool force)
        {
            (FMesh.PluginBase as IDX11ResourceHost).Destroy(context, force);
            (FMaterial.PluginBase as IDX11ResourceHost).Destroy(context, force);
        }
    }
}