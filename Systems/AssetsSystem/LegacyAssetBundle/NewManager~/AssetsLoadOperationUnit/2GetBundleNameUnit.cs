using System.Collections;
using UnityEditor.VersionControl;
using UnityEngine;

namespace PowerCellStudio
{
    internal class GetBundleNameUnit : IAssetsLoadOperationUnit
    {
        public IAssetsLoadOperationUnit next { get; set; }

        public LoaderYieldInstruction<T> Operation<T>(LoaderYieldInstruction<T> handler, NewAssetBundleManager manager, string assetPath, string bundleName,
            bool releaseBundleOnTime) where T : Object
        {
            bundleName = manager.indexer.GetBundleNameByAsset(assetPath);
            if (string.IsNullOrEmpty(bundleName))
            {
                AssetLog.LogError($"Failed to get bundle name for asset: {assetPath}");
                if (handler == null) handler = AssetUtils.GetLoadHandler<T>(assetPath);
                handler.SetAsset(null);
                return handler;
            }
            return next.Operation(handler, manager, assetPath, bundleName, releaseBundleOnTime);
        }
    }
}