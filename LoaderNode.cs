#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V2;
using VVVV.Core.Logging;

using SceneGraph.Core;
#endregion usings

namespace VVVV.SceneGraph
{
	[PluginInfo(Name = "Scene", Category = "SceneGraph",
	            Help = "Loads an Assimp compatible 3d file and outputs a scenegraph", Tags = "SceneFile, Loader, Reader, assimp",
	            Author = "woei", Credits = "v1.0 sponsored by decode.io")]
	public class LoaderSceneGraphNode : IPluginEvaluate, IDisposable, IPartImportsSatisfiedNotification
	{
		#region fields & pins
		#pragma warning disable 0649
        [Input("Filename", StringType=StringType.Filename)]
        IDiffSpread<string> FFilename;

        [Input("Asset Root", DefaultString = "")]
        IDiffSpread<string> FAssetRoot;

        [Input("Reload", IsBang = true)]
        IDiffSpread<bool> FReload;

        //[Input("Preload Data")]
        //IDiffSpread<bool> FPreload;

        [Output("Graph", AutoFlush = false)]
        Pin<GraphNode> FRoot;

        [Output("Scene", AutoFlush = false)]
        ISpread<Scene> FScene;

        [Output("Is Valid")]
        ISpread<bool> FIsValid;

        [Import()]
        ILogger FLogger;
		#pragma warning restore
		#endregion fields & pins
		
        public void OnImportsSatisfied()
        {
            FRoot.SliceCount = 0;
            FScene.SliceCount = 0;

            FRoot.Flush();
            FScene.Flush();

            FRoot.Connected += FRoot_Connected;
        }

        private void FRoot_Connected(object sender, PinConnectionEventArgs args)
        {
            foreach (var n in FRoot)
                n?.UpdateTransformGraph();
        }

        public void Dispose()
		{
            foreach (var s in FScene)
                s?.Dispose();
		}        

		public void Evaluate(int spreadMax)
		{
			if (FFilename.IsChanged || FAssetRoot.IsChanged || FReload.IsChanged)
            {
                bool needsFlush = false;
                if (spreadMax < FScene.SliceCount)
                {
                    for (int i = FScene.SliceCount - 1; i >= spreadMax; i++)
                        FScene[i]?.Dispose();
                    needsFlush = true;
                }
                FScene.SliceCount = spreadMax;
                FRoot.SliceCount = spreadMax;
                FIsValid.SliceCount = spreadMax;
                for (int i = 0; i < spreadMax; i++)
                {
                    var filename = FFilename[i];
                    if (FScene[i] == null || FScene[i].Filename != filename || FReload[i])
                    {
                        FScene[i]?.Dispose();
                        if (System.IO.File.Exists(filename))
                        {
                            try
                            {
                                FScene[i] = new Scene(filename, FAssetRoot[i], true, false);
                                FScene[i].InitGraph();
                                FRoot[i] = FScene[i].Root;
                                FIsValid[i] = true;
                            }
                            catch (Exception e)
                            {
                                FScene[i] = new Scene(filename);
                                FRoot[i] = null;
                                FIsValid[i] = false;
                                FLogger.Log(LogType.Debug, "cannot load "+filename);
                                FLogger.Log(e);
                            }
                        }
                        else
                        {
                            FScene[i] = new Scene(filename);
                            FRoot[i] = null;
                            FIsValid[i] = false;
                            FLogger.Log(LogType.Debug, filename + " doesn't exist");
                        }
                        needsFlush = true;
                    }
                }

                if (needsFlush)
                {
                    FRoot.Flush();
                    FScene.Flush();
                    needsFlush = false;
                }
            }
		}
	}
}