#region usings
using System;
using System.Linq;
using System.Collections.Generic;

using System.ComponentModel.Composition;
using VVVV.PluginInterfaces.V2;

using SceneGraph.Core;
using SceneGraph.Adaptors;
using SlimDX;
using FeralTic.DX11;
using FeralTic.DX11.Resources;
using VVVV.DX11;
#endregion usings

namespace VVVV.SceneGraph
{
    [PluginInfo(Name = "Mesh", Category = "SceneGraph", Version = "DX11",
                Help = "Returns the meshes and additional geometry related data.", Tags = "geometry",
                Author = "woei")]
    public class MeshNode : IPluginEvaluate, IPartImportsSatisfiedNotification, IDisposable, IDX11ResourceHost, INestedNode
    {
        #region fields & pins
        #pragma warning disable 0649
        [Input("GraphNode")]
        IDiffSpread<GraphNode> FGraphNode;
        [Input("GraphNodeInternal", Visibility = PinVisibility.False)]
        ISpread<GraphNode> FGraphNodeInternal;

        [Output("Mesh ID")]
        ISpread<int> FMeshID;

        [Output("Geometry")]
        ISpread<DX11Resource<DX11IndexedGeometry>> FGeometry;
        bool FInvalidate;

        [Output("Bin Size", Order = 500)]
        ISpread<int> FBinSize;

        [Import]
        IPluginHost2 FHost;
        string FNodePath = string.Empty;

        Spread<GraphNode> FSelected = new Spread<GraphNode>();

        [Import]
        IIOFactory FIOFactory;
        bool IsNested;
        public bool GraphChanged { get; set; }
        bool PinsChanged;

        public int PinStartIndex = 10;
        IIOContainer<ISpread<Vector3>> FMin;
        IIOContainer<ISpread<Vector3>> FMax;
        IIOContainer<ISpread<ISpread<string>>> FBoneNames;
        IIOContainer<ISpread<ISpread<Matrix>>> FBones;
        IIOContainer<ISpread<int>> FColorChannelCount;
        IIOContainer<ISpread<int>> FUvChannelCount;
        IIOContainer<ISpread<int>> FVerticesCount;
        #pragma warning restore
        #endregion fields & pins

        public void OnImportsSatisfied()
        {
            FSelected.SliceCount = 0;
            FMeshID.SliceCount = 0;
            FGeometry.SliceCount = 0;

            IsNested = !FGraphNodeInternal.GetType().Namespace.Contains("VVVV.Hosting");

            if (IsNested)
                FHost.GetPin("Bin Size").Visibility = PinVisibility.False;
            
            CreatePinToggles(FIOFactory);
        }

        #region pinToggles
        void CreatePinToggles(IIOFactory factory)
        {
            var attr = new ConfigAttribute($"Enable Bounds") { DefaultBoolean = !IsNested };
            var toggle = factory.CreateDiffSpread<bool>(attr);
            var boundsOrder = PinStartIndex;
            toggle.Changed += (s) => ToggleBounds(s[0], boundsOrder);

            attr = new ConfigAttribute($"Enable Bones") { DefaultBoolean = !IsNested };
            toggle = factory.CreateDiffSpread<bool>(attr);
            var bonesOrder = boundsOrder + 2;
            toggle.Changed += (s) => ToggleBones(s[0], bonesOrder);

            attr = new ConfigAttribute($"Enable Channel Counts") { DefaultBoolean = !IsNested };
            toggle = factory.CreateDiffSpread<bool>(attr);
            var channelOrder = bonesOrder + 4;
            toggle.Changed += (s) => ToggleChannelCounts(s[0], channelOrder);

            attr = new ConfigAttribute($"Enable Vertex Count") { DefaultBoolean = !IsNested };
            toggle = factory.CreateDiffSpread<bool>(attr);
            var vCountOrder = channelOrder + 2;
            toggle.Changed += (s) => ToggleVertexCount(s[0], vCountOrder);
        }

        void ToggleBounds(bool toggle, int order)
        {
            if (toggle & FMin == null)
            {
                FMin = FIOFactory.CreateIOContainer<ISpread<Vector3>>(new OutputAttribute($"Bounds Min") { Order = order });
                FMax = FIOFactory.CreateIOContainer<ISpread<Vector3>>(new OutputAttribute($"Bounds Max") { Order = order + 1 });
                PinsChanged = true;
            }
            else if (FMin != null)
            {
                FMin.Dispose();
                FMin = null;
                FMax.Dispose();
                FMax = null;
                PinsChanged = true;
            }
        }

        void ToggleBones(bool toggle, int order)
        {
            if (toggle & FBones == null)
            {
                FBoneNames = FIOFactory.CreateIOContainer<ISpread<ISpread<string>>>(new OutputAttribute($"Bone Names") { Order = order, BinOrder = order + 1 });
                FBones = FIOFactory.CreateIOContainer<ISpread<ISpread<Matrix>>>(new OutputAttribute($"Bones") { Order = order + 2, BinOrder = order + 3});
                PinsChanged = true;
            }
            else if (FMin != null)
            {
                FBoneNames.Dispose();
                FBoneNames = null;
                FBones.Dispose();
                FBones = null;
                PinsChanged = true;
            }
        }

        void ToggleChannelCounts(bool toggle, int order)
        {
            if (toggle & FColorChannelCount == null)
            {
                FColorChannelCount = FIOFactory.CreateIOContainer<ISpread<int>>(new OutputAttribute($"Color Channel Count") { Order = order });
                FUvChannelCount = FIOFactory.CreateIOContainer<ISpread<int>>(new OutputAttribute($"UV Channel Count") { Order = order+1 });
                PinsChanged = true;
            }
            else if (FColorChannelCount != null)
            {
                FColorChannelCount.Dispose();
                FColorChannelCount = null;
                FUvChannelCount.Dispose();
                FUvChannelCount = null;
                PinsChanged = true;
            }
        }

        void ToggleVertexCount(bool toggle, int order)
        {
            if (toggle & FVerticesCount == null)
            {
                FVerticesCount = FIOFactory.CreateIOContainer<ISpread<int>>(new OutputAttribute($"Vertex Count") { Order = order });
                PinsChanged = true;
            }
            else if (FVerticesCount != null)
            {
                FVerticesCount.Dispose();
                FVerticesCount = null;
                PinsChanged = true;
            }
        }
        #endregion pinToggles

        public void Evaluate(int spreadMax)
        {
            if (string.IsNullOrEmpty(FNodePath))
                FNodePath = FHost.GetNodePath(false);

            FInvalidate = false;
            if (FGraphNode.IsChanged || (IsNested && GraphChanged))
            {
                FInvalidate = true;
                
                var input = IsNested ? FGraphNodeInternal : FGraphNode;

                var trash = new Spread<GraphNode>(FSelected.SliceCount);
                trash.AssignFrom(FSelected);

                FSelected.SliceCount = 0;
                FMeshID.SliceCount = 0;
                FBinSize.SliceCount = input.SliceCount;
                for (int i = 0; i < input.SliceCount; i++)
                {
                    if (input[i] != null && (input[i].Element is MeshElement))
                    {
                        FSelected.Add(input[i]);
                        FMeshID.Add((input[i].Element as MeshElement).MeshID);
                        trash.RemoveAll(t => t.ID == input[i].ID);
                        FBinSize[i] = 1;
                    }
                    else
                        FBinSize[i] = 0;
                }
                
                foreach (var t in trash)
                {
                    t.ReleaseGeometry(FNodePath);
                    t.PurgeGeometry();
                }

                FGeometry.ResizeAndDismiss(FSelected.SliceCount, () => new DX11Resource<DX11IndexedGeometry>());
            }

            if (PinsChanged || FInvalidate)
            {
                if (FMin != null)
                {
                    FMin.IOObject.SliceCount = FGeometry.SliceCount;
                    FMax.IOObject.SliceCount = FGeometry.SliceCount;
                }

                if (FBones != null)
                {
                    FBones.IOObject.SliceCount = FGeometry.SliceCount;
                    FBoneNames.IOObject.SliceCount = FGeometry.SliceCount;
                }

                if (FColorChannelCount != null)
                {
                    FColorChannelCount.IOObject.SliceCount = FGeometry.SliceCount;
                    FUvChannelCount.IOObject.SliceCount = FGeometry.SliceCount;
                }

                if (FVerticesCount != null)
                    FVerticesCount.IOObject.SliceCount = FGeometry.SliceCount;

                for (int i = 0; i < FSelected.SliceCount; i++)
                {
                    var mesh = (FSelected[i].Element as MeshElement).Mesh;
                    if (FMin != null)
                    {
                        FMin.IOObject[i] = mesh.BoundingBox.Minimum;
                        FMax.IOObject[i] = mesh.BoundingBox.Maximum;
                    }
                    if (FBones != null)
                    {
                        FBones.IOObject[i].AssignFrom(mesh.BoneMatrices);
                        FBoneNames.IOObject[i].AssignFrom(mesh.BoneNames);
                    }
                    if (FColorChannelCount != null)
                    {
                        FColorChannelCount.IOObject[i] = mesh.ColorChannelCount;
                        FUvChannelCount.IOObject[i] = mesh.UvChannelCount;
                    }
                    if (FVerticesCount != null)
                        FVerticesCount.IOObject[i] = mesh.VerticesCount;
                }
                PinsChanged = false;
            }
        }

        public void Dispose()
        {
            foreach (var n in FSelected)
            {
                n.ReleaseGeometry(FNodePath);
                n.PurgeGeometry();
            }
        }

        public void Update(DX11RenderContext context)
        {
            if (FInvalidate || (!FGeometry.All(r => r.Contains(context))))
            {
                int incr = 0;
                foreach (var n in FSelected)
                {
                    FGeometry[incr][context] = n.GetGeometry(context, FNodePath);
                    incr++;
                }
            }
        }

        public void Destroy(DX11RenderContext context, bool force)
        {
            //remove context from all slices
            foreach (var slice in FGeometry)
                slice.Remove(context);

            //set resource as released by this node
            foreach (var n in FSelected)
            {
                n.ReleaseGeometry(FNodePath, context);
                n.PurgeGeometry();
            }
        }
    }
}