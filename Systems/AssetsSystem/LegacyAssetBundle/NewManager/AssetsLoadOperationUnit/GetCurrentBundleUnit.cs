// using System.Collections;
// using UnityEngine;

// namespace PowerCellStudio
// {
//     internal class GetBundleNameUnit : IAssetsLoadOperationUnit
//     {
//         public IAssetsLoadOperationUnit next { get; set; }

//         public LoaderYieldInstruction<T> Operation<T>(LoaderYieldInstruction<T> handler, NewAssetBundleManager manager, string assetPath, string bundleName,
//             bool releaseBundleOnTime) where T : Object
//         {
//             if (handler.canceled) return handler;
//             if (manager.bundleCache.TryGetCache(bundleName, out var bundle))
//             {
//                 manager.TryAddBundleRef(bundleName);
//                 if (AssetUtils.TryGetSubAssetName(assetPath, out var mainPath, out var subAssetName))
//                 {
//                     var assetRequest = bundle.LoadAssetWithSubAssetsAsync<T>(mainPath);
//                     assetRequest.completed += operation =>
//                     {
//                         var operationHandle = operation as AssetBundleRequest;
//                         var assets = operationHandle?.allAssets as T[];
//                         if (assets == null)
//                         {
//                             if (manager.loadingAssets.TryGetAssetLoadingHandle(assetPath, out var handlerChain))
//                             {
//                                 foreach (var h in handlerChain)
//                                 {
//                                     h.SetAsset(null);
//                                 }
//                                 manager.loadingAssets.RemoveAssetLoadingHandle(assetPath);
//                             }
//                             return;
//                         }
//                         foreach (var a in assets)
//                         {
//                             if (a == null) continue;
//                             if (a.name == subAssetName && a is T matched)
//                             {
//                                 manager.assetCache.AddCache(assetPath, matched);
                                
//                                 loadAssetRequest.SetAsset(matched);
//                                 return;
//                             }
//                         }

//                         loadAssetRequest.SetAsset(null);
//                     };
//                 }
//                 else
//                 {
//                     var assetRequest = bundle.LoadAssetAsync<T>(assetPath);
//                     assetRequest.completed += (operation) =>
//                     {
//                         var operationHandle = operation as AssetBundleRequest;
//                         if(operationHandle == null)
//                         {
//                             loadAssetRequest.SetAsset(null);
//                             return;
//                         }
//                         var asset = operationHandle.asset as T;
//                         loadAssetRequest.SetAsset(asset);
//                     };
//                 }
//             }
//             return next.Operation(handler, manager, assetPath, bundleName, releaseBundleOnTime);
//         }
//     }
// }