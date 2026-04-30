// using UnityEngine;

// namespace PowerCellStudio
// {
//     internal class GetLoadedAssetUnit : IAssetsLoadOperationUnit
//     {
//         public IAssetsLoadOperationUnit next { get; set; }

//         public LoaderYieldInstruction<T> Operation<T>(LoaderYieldInstruction<T> handler, NewAssetBundleManager manager, string assetPath, string bundleName,
//             bool releaseBundleOnTime) where T : Object
//         {
//             if (manager.assetCache.TryGetAsset<T>(assetPath, out var asset))
//             {
//                 if (handler == null) handler = AssetUtils.GetLoadHandler<T>(assetPath);
//                 handler.SetAsset(asset);
//                 return handler;
//             }
//             return next.Operation(handler, manager, assetPath, bundleName, releaseBundleOnTime);
//         }
//     }
// }