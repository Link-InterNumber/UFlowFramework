using System.Collections;
using UnityEngine;

namespace PowerCellStudio
{
    internal class AddToLoadingUnit : IAssetsLoadOperationUnit
    {
        public IAssetsLoadOperationUnit next { get; set; }

        public LoaderYieldInstruction<T> Operation<T>(LoaderYieldInstruction<T> handler, NewAssetBundleManager manager,
            string assetPath, string bundleName,
            bool releaseBundleOnTime) where T : Object
        {
            if (handler == null) handler = AssetUtils.GetLoadHandler<T>(assetPath);
            var isFirst = manager.loadingAssets.AddLoadingHandle(assetPath, handler as LoaderYieldInstruction<Object>);
            if (!isFirst) return handler;
            return next.Operation(handler, manager, assetPath, bundleName, releaseBundleOnTime);
        }
    }
}