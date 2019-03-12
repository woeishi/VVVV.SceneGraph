using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using VVVV.PluginInterfaces.V2;

using SlimDX;
using SceneGraph.Core;
using SceneGraph.Core.Animations;

namespace VVVV.SceneGraph
{
    [PluginInfo(Name = "AnimationInfo", Category = "SceneGraph",
                Help = "Outputs animation infos and parameters of the selected nodes or the loaded scene", Tags = "timings, keyframes",
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

        [Input("Local | Global")]
        IDiffSpread<bool> FToggleGlobal;


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

        [Output("Channel Types", Visibility = PinVisibility.OnlyInspector, BinVisibility = PinVisibility.OnlyInspector)]
        ISpread<ISpread<string>> FChannelTypes;

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
            if (FInput.IsChanged || FToggleGlobal.IsChanged
                || (FPosToggle.IsChanged && FPosToggle[0])
                || (FScaleToggle.IsChanged && FScaleToggle[0])
                || (FRotToggle.IsChanged && FRotToggle[0]))
            {
                
                FTrackCount.SliceCount = 0;
                FAnimName.SliceCount = 0;
                FDuration.SliceCount = 0;
                FTPS.SliceCount = 0;
                FChannelTypes.SliceCount = 0;

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

                if (FToggleGlobal[0])
                {
                    FName.SliceCount = 1;
                    var scenes = new List<IScene>();
                    foreach (var gn in FInput)
                    {
                        if (gn != null && !scenes.Contains(gn.Scene) && gn.Scene.Animations.Length > 0)
                        {
                            var scene = gn.Scene;
                            scenes.Add(scene);

                            FTrackCount.Add(scene.Animations.Length);
                            WriteOutputs(scene.Animations);
                        }
                    }
                    FName[0] = scenes.Select(s => s.Filename).ToSpread();
                }
                else
                {
                    FName.SliceCount = spreadMax;
                    for (int i = 0; i < spreadMax; i++)
                    {
                        FName[i] = new Spread<string>(0);
                        if (FInput[i] != null && FInput[i].AnimationsReference != null)
                        {
                            FName[i].Add(FInput[i].Name);
                            FTrackCount.Add(FInput[i].AnimationsReference.Animations.Length);
                            WriteOutputs(FInput[i].AnimationsReference.Animations);
                        }
                    }
                }
                
            }
        }

        void WriteOutputs(Animation[] animations)
        {
            foreach (var anim in animations)
            {
                FAnimName.Add(anim.Name);
                FDuration.Add(anim.Duration);
                FTPS.Add(anim.TicksPerSecond);
                FChannelTypes.Add(anim.Channels.Select(c => c.Type).ToSpread());

                if (FPosToggle[0] && anim.Channels.Any(c => c.Type == "translation"))
                {
                    var markers = anim.Channels.Where(c => c.Type == "translation").FirstOrDefault().Markers;
                    FPosTime.IOObject.AddRange(markers.Select(m => m.Key));
                    FPosVal.IOObject.AddRange(markers.Select(m => (m as IMarker<Vector3>).Value));
                    FPosBin.IOObject.Add(markers.Length);
                }

                if (FScaleToggle[0] && anim.Channels.Any(c => c.Type == "scale"))
                {
                    var markers = anim.Channels.Where(c => c.Type == "scale").FirstOrDefault().Markers;
                    FScaleTime.IOObject.AddRange(markers.Select(m => m.Key));
                    FScaleVal.IOObject.AddRange(markers.Select(m => (m as IMarker<Vector3>).Value));
                    FScaleBin.IOObject.Add(markers.Length);
                }

                if (FRotToggle[0] && anim.Channels.Any(c => c.Type == "rotation"))
                {
                    var markers = anim.Channels.Where(c => c.Type == "rotation").FirstOrDefault().Markers;
                    FRotTime.IOObject.AddRange(markers.Select(m => m.Key));
                    FRotVal.IOObject.AddRange(markers.Select(m => (m as IMarker<Quaternion>).Value));
                    FRotBin.IOObject.Add(markers.Length);
                }
            }
        }
    }
}
