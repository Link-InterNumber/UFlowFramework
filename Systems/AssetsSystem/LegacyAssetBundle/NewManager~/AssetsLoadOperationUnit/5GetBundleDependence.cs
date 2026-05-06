using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace PowerCellStudio
{
    internal class GetBundleDependence : IAssetsLoadOperationUnit
    {
        public IAssetsLoadOperationUnit next { get; set; }

        public LoaderYieldInstruction<T> Operation<T>(LoaderYieldInstruction<T> handler, NewAssetBundleManager manager, string assetPath, string bundleName,
            bool releaseBundleOnTime) where T : Object
        {
            ApplicationManager.RunCoroutine(LoadDependencies(bundleName, manager, assetPath, releaseBundleOnTime));
            return next.Operation(handler, manager, assetPath, bundleName, releaseBundleOnTime);
        }

        private IEnumerator LoadDependencies(string bundleName, NewAssetBundleManager manager, string assetPath, bool releaseBundleOnTime)
        {
            var dependencies = manager.dependenceMap.GetBundleDependencies(bundleName);
            if (dependencies != null)
            {
                foreach (var d in dependencies)
                {
                    if (manager.TryGetLoadedBundle(bundleName, out _)) continue;
                    if (!manager.loadingBundles.IsLoading(d))
                    {
                        manager.loadingBundles.AddLoadingHandle(bundleName);
                        yield return AsyncLoadSingleAssetsBundle(d, manager);
                    }
                }
            }

            if (!manager.loadingBundles.IsLoading(bundleName))
            {
                manager.loadingBundles.AddLoadingHandle(bundleName);
                yield return AsyncLoadSingleAssetsBundle(bundleName, manager);
            }
        }

        private IEnumerator AsyncLoadSingleAssetsBundle(string bundleName, NewAssetBundleManager manager)
        {
            AssetBundle bundle = null;
            var path = manager.GetBundlePath(bundleName);
            if (Application.platform == RuntimePlatform.Android)
            {
                if (!manager.remoteBundleManifest.IsBundleNeedLoadFromRemote(bundleName))
                {
                    var webRequest = UnityWebRequestAssetBundle.GetAssetBundle(path);
                    yield return webRequest.SendWebRequest();
                    bundle = DownloadHandlerAssetBundle.GetContent(webRequest);
                    manager.SetBundleLoaded(bundleName, bundle);
                    webRequest.Dispose();
                    yield break;
                }
            }
            else if (File.Exists(path))
            {
                var abcr = AssetBundle.LoadFromFileAsync(path);
                yield return abcr;
                bundle = abcr.assetBundle;
                manager.SetBundleLoaded(bundleName, bundle);
                yield break;
            }
            var waitForLoadFromRemote = new  YieldInstructionCompletionSource<bool>();
            yield return manager.remoteBundleManifest.LoadRemoteBundle(bundleName, waitForLoadFromRemote);
            if (waitForLoadFromRemote.Result)
            {
                manager.remoteBundleManifest.SaveRemoteManifest();
                yield return AsyncLoadSingleAssetsBundle(bundleName, manager);
            }
        }
    }
}