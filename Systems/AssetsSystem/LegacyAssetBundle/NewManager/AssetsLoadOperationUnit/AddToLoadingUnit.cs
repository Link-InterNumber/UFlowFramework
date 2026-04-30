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
            if (manager.loadingAssets.TryGetLoadingHandle(assetPath, out var currentHandler))
            {
                currentHandler
                return handler;
            }
            return next.Operation(handler, manager, assetPath, bundleName, releaseBundleOnTime);
        }
    }
}