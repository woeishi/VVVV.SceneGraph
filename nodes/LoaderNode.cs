#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V2;
using VVVV.Core.Logging;

using SceneGraph.Core;
using SceneGraph.Adaptors;
#endregion usings

namespace VVVV.SceneGraph
{
	[PluginInfo(Name = "Scene", Category = "SceneGraph",
	            Help = "Loads 3d scene and outputs a scenegraph, handling transform hierarchies and resources", Tags = "SceneFile, Loader, Reader, assimp",
	            Author = "woei", Credits = "v1.0 to a huge amount sponsored by decode.io, v2.0 supported by meso.design and wirmachenbunt.de")]
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

        [Output("GraphNode", AutoFlush = false)]
        Pin<GraphNode> FRoot;

        [Output("Source", AutoFlush = false)]
        ISpread<AssimpNet.AssimpScene> FSource;

        [Output("Is Valid")]
        ISpread<bool> FIsValid;

        [Import()]
        ILogger FLogger;

        [Import()]
        IMainLoop FMainloop;

        Spread<IScene> FScene = new Spread<IScene>();
		#pragma warning restore
		#endregion fields & pins
		
        public void OnImportsSatisfied()
        {
            FRoot.SliceCount = 0;
            FScene.SliceCount = 0;

            FRoot.Flush();
            FScene.Flush();

            FMainloop.OnResetCache += FMainloop_OnResetCache;
        }

        void FMainloop_OnResetCache(object sender, EventArgs e)
        {
            foreach (var s in FScene)
                s.PurgeResources();
        }

        public void Dispose()
		{
            FMainloop.OnResetCache -= FMainloop_OnResetCache;
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
                // FScene.SliceCount = spreadMax;
                // on expanding the spread, it will fill the new slices by repeating
                // causes disposal of resources in use
                // ResizeAndDismiss to make sure added slices are null
                // shrinking is already handled above, so no need for ResizeAndDispose
                FScene.ResizeAndDismiss(spreadMax, () => null);
                FRoot.SliceCount = spreadMax;
                FIsValid.SliceCount = spreadMax;
                for (int i = 0; i < spreadMax; i++)
                {
                    var filename = FFilename[i];
                    var assetRoot = FAssetRoot[i];
                    if (string.IsNullOrWhiteSpace(assetRoot))
                        assetRoot = System.IO.Path.GetDirectoryName(filename);
                    if (FScene[i] == null || FScene[i].Filename != filename || FScene[i].AssetRoot != assetRoot || FReload[i])
                    {
                        FScene[i]?.Dispose();
                        if (System.IO.File.Exists(filename))
                        {
                            try
                            {
                                FScene[i] = new SceneAssimp(filename, FAssetRoot[i]);
                                FSource[i] = ((SceneAssimp)FScene[i]).Source;
                                FRoot[i] = FScene[i].Root;
                                FIsValid[i] = true;
                            }
                            catch (Exception e)
                            {
                                FScene[i] = new SceneAssimp(filename);
                                FSource[i] = ((SceneAssimp)FScene[i]).Source;
                                FRoot[i] = null;
                                FIsValid[i] = false;
                                FLogger.Log(LogType.Debug, "cannot load "+filename);
                                FLogger.Log(e);
                            }
                        }
                        else
                        {
                            FScene[i] = new SceneAssimp(filename);
                            FRoot[i] = null;
                            FIsValid[i] = false;
                            FLogger.Log(LogType.Debug, filename + " doesn't exist");
                        }
                        needsFlush = true;
                    }
                }

                if (needsFlush)
                {
                    FRoot.Flush(true);
                    FScene.Flush(true);
                    needsFlush = false;
                }
            }
		}
	}
}