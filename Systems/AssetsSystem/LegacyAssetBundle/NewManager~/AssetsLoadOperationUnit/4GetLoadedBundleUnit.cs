using System.Collections;
using UnityEngine;

namespace PowerCellStudio
{
    internal class GetLoadedBundleUnit : IAssetsLoadOperationUnit
    {
        public IAssetsLoadOperationUnit next { get; set; }

        public LoaderYieldInstruction<T> Operation<T>(LoaderYieldInstruction<T> handler, NewAssetBundleManager manager, string assetPath, string bundleName,
            bool releaseBundleOnTime) where T : Object
        {
            manager.TryGetLoadedBundle(bundleName, out var bundle);
            if (bundle == null)
                return next.Operation(handler, manager, assetPath, bundleName, releaseBundleOnTime);

            manager.TryAddBundleRef(bundleName, 1);
            if (AssetUtils.TryGetSubAssetName(assetPath, out var mainPath, out var subAssetName))
            {
                var assetRequest = bundle.LoadAssetWithSubAssetsAsync<T>(mainPath);
                assetRequest.completed += operation =>
                {
                    T asset = null;
                    var operationHandle = operation as AssetBundleRequest;
                    var assets = operationHandle?.allAssets as T[];
                    if (operationHandle != null && assets != null)
                    {
                        foreach (var a in assets)
                        {
                            if (a == null) continue;
                            if (a.name == subAssetName && a is T matched)
                            {
                                asset = matched;
                                break;
                            }
                        }
                    }
                    if (asset == null)
                    {
                        AssetLog.LogError($"Failed to load sub asset: {assetPath} from bundle: {bundleName}");
                    }
                    manager.SetAssetLoaded(assetPath, asset);
                    if (releaseBundleOnTime) manager.TryDelBundleRef(bundleName, 1);
                };
            }
            else
            {
                var assetRequest = bundle.LoadAssetAsync<T>(assetPath);
                assetRequest.completed += (operation) =>
                {
                    var operationHandle = operation as AssetBundleRequest;
                    var asset = operationHandle?.asset as T;
                    if (asset == null)
                    {
                        AssetLog.LogError($"Failed to load asset: {assetPath} from bundle: {bundleName}");
                    }
                    manager.SetAssetLoaded(assetPath, asset);
                    if (releaseBundleOnTime) manager.TryDelBundleRef(bundleName, 1);
                };
            }
            return handler;
        }
    }
}