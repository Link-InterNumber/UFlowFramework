using UnityEngine;

namespace PowerCellStudio
{
    internal class GetLoadedAssetUnit : IAssetsLoadOperationUnit
    {
        public IAssetsLoadOperationUnit next { get; set; }

        public LoaderYieldInstruction<T> Operation<T>(LoaderYieldInstruction<T> handler, NewAssetBundleManager manager, string assetPath, string bundleName,
            bool releaseBundleOnTime) where T : Object
        {
            handler = AssetUtils.GetLoadHandler<T>(assetPath);
            if (manager.cachedAsset.TryGetCache(assetPath, out var asset))
            {
                handler.SetAsset(asset as T);
                return handler;
            }
            return next.Operation(handler, manager, assetPath, bundleName, releaseBundleOnTime);
        }
    }
}