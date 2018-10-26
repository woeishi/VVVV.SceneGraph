using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using VVVV.PluginInterfaces.V2;

using SlimDX;
using SceneGraph.Core;


namespace VVVV.SceneGraph
{
    [PluginInfo(Name = "AnimationInfo", Category = "SceneGraph",
                Help = "Outputs animation infos and parameters of the selected nodes.", Tags = "keyframes, query, xpath",
                Author = "woei")]
    public class AnimationInfoNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins
#pragma warning disable 0649
        [Config("Enable Translation")]
        IDiffSpread<bool> FPosToggle;

        [Config("Enable Scale")]
        IDiffSpread<bool> FScaleToggle;

        [Config("Enable Rotation")]
        IDiffSpread<bool> FRotToggle;


        [Input("GraphNode")]
        IDiffSpread<GraphNode> FInput;

        [Input("XPath", DefaultString = ".//*")]
        IDiffSpread<string> FQuery;


        [Output("Name")]
        ISpread<ISpread<string>> FName;

        [Output("Animation Count")]
        ISpread<int> FTrackCount;

        [Output("Animation Name")]
        ISpread<string> FAnimName;

        [Output("Duration")]
        ISpread<float> FDuration;

        [Output("Ticks per Second")]
        ISpread<float> FTPS;

        [Import]
        IIOFactory FIOFactory;

        IIOContainer<ISpread<float>> FPosTime;
        IIOContainer<ISpread<Vector3>> FPosVal;
        IIOContainer<ISpread<int>> FPosBin;

        IIOContainer<ISpread<float>> FScaleTime;
        IIOContainer<ISpread<Vector3>> FScaleVal;
        IIOContainer<ISpread<int>> FScaleBin;

        IIOContainer<ISpread<float>> FRotTime;
        IIOContainer<ISpread<Quaternion>> FRotVal;
        IIOContainer<ISpread<int>> FRotBin;
#pragma warning restore
        #endregion fields & pins

        public void OnImportsSatisfied()
        {
            FPosToggle.Changed += FPosToggle_Changed;
            FScaleToggle.Changed += FScaleToggle_Changed;
            FRotToggle.Changed += FRotToggle_Changed;
        }

        void FPosToggle_Changed(IDiffSpread<bool> spread)
        {
            if (spread[0])
            {
                FPosTime = FIOFactory.CreateIOContainer<ISpread<float>>(new OutputAttribute("Translation Time") { Order = 10 });
                FPosVal = FIOFactory.CreateIOContainer<ISpread<Vector3>>(new OutputAttribute("Translation") { Order = 11 });
                FPosBin = FIOFactory.CreateIOContainer<ISpread<int>>(new OutputAttribute("Translation Bin Size") { Order = 12 });
            }
            else
            {
                FPosTime?.Dispose();
                FPosVal?.Dispose();
                FPosBin?.Dispose();
            }
        }

        void FScaleToggle_Changed(IDiffSpread<bool> spread)
        {
            if (spread[0])
            {
                FScaleTime = FIOFactory.CreateIOContainer<ISpread<float>>(new OutputAttribute("Scale Time") { Order = 20 });
                FScaleVal = FIOFactory.CreateIOContainer<ISpread<Vector3>>(new OutputAttribute("Scale") { Order = 21 });
                FScaleBin = FIOFactory.CreateIOContainer<ISpread<int>>(new OutputAttribute("Scale Bin Size") { Order = 22 });
            }
            else
            {
                FScaleTime?.Dispose();
                FScaleVal?.Dispose();
                FScaleBin?.Dispose();
            }
        }

        void FRotToggle_Changed(IDiffSpread<bool> spread)
        {
            if (spread[0])
            {
                FRotTime = FIOFactory.CreateIOContainer<ISpread<float>>(new OutputAttribute("Rotation Time") { Order = 30 });
                FRotVal = FIOFactory.CreateIOContainer<ISpread<Quaternion>>(new OutputAttribute("Rotation") { Order = 31 });
                FRotBin = FIOFactory.CreateIOContainer<ISpread<int>>(new OutputAttribute("Rotation Bin Size") { Order = 32 });
            }
            else
            {
                FRotTime?.Dispose();
                FRotVal?.Dispose();
                FRotBin?.Dispose();
            }
        }

        public void Evaluate(int spreadMax)
        {
            if (FInput.IsChanged || FQuery.IsChanged
                || (FPosToggle.IsChanged && FPosToggle[0])
                || (FScaleToggle.IsChanged && FScaleToggle[0])
                || (FRotToggle.IsChanged && FRotToggle[0]))
            {
                FName.SliceCount = spreadMax;
                FTrackCount.SliceCount = 0;
                FAnimName.SliceCount = 0;
                FDuration.SliceCount = 0;
                FTPS.SliceCount = 0;

                if (FPosToggle[0])
                {
                    FPosTime.IOObject.SliceCount = 0;
                    FPosVal.IOObject.SliceCount = 0;
                    FPosBin.IOObject.SliceCount = 0;
                }

                if (FScaleToggle[0])
                {
                    FScaleTime.IOObject.SliceCount = 0;
                    FScaleVal.IOObject.SliceCount = 0;
                    FScaleBin.IOObject.SliceCount = 0;
                }

                if (FRotToggle[0])
                {
                    FRotTime.IOObject.SliceCount = 0;
                    FRotVal.IOObject.SliceCount = 0;
                    FRotBin.IOObject.SliceCount = 0;
                }

                for (int i = 0; i < spreadMax; i++)
                {
                    FName[i] = new Spread<string>(0);
                    if (FInput[i] != null && (!string.IsNullOrWhiteSpace(FQuery[i])))
                    {
                        foreach (var n in FInput[i].XPathQuery(FQuery[i].Trim()))
                        {
                            if (n.Tracks.Count > 0)
                            {
                                FName[i].Add(n.Name);
                                FTrackCount.Add(n.Tracks.Count);
                                foreach (var t in n.Tracks)
                                {
                                    FAnimName.Add(t.Name);
                                    FDuration.Add(t.Duration);
                                    FTPS.Add(t.TicksPerSecond);

                                    if (FPosToggle[0])
                                    {
                                        FPosTime.IOObject.AddRange(t.PositionTime);
                                        FPosVal.IOObject.AddRange(t.Position);
                                        FPosBin.IOObject.Add(t.Position.Count);
                                    }

                                    if (FScaleToggle[0])
                                    {
                                        FScaleTime.IOObject.AddRange(t.ScaleTime);
                                        FScaleVal.IOObject.AddRange(t.Scale);
                                        FScaleBin.IOObject.Add(t.Scale.Count);
                                    }

                                    if (FRotToggle[0])
                                    {
                                        FRotTime.IOObject.AddRange(t.RotationTime);
                                        FRotVal.IOObject.AddRange(t.Rotation);
                                        FRotBin.IOObject.Add(t.Rotation.Count);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
