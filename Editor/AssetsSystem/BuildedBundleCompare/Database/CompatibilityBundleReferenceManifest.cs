// using UnityEngine;
// using UnityEngine.Build.Pipeline;
//
// namespace PowerCellStudio.Editor
// {
//     /// <summary>
//     /// 对自定义 CompatibilityAssetBundleManifest 的引用关系适配。
//     /// Adapts the custom CompatibilityAssetBundleManifest to the bundle reference interface.
//     /// </summary>
//     public sealed class CompatibilityBundleReferenceManifest : IBundleReferenceManifest
//     {
//         private CompatibilityAssetBundleManifest _manifest;
//         private string _bundleDirectory;
//
//         public CompatibilityBundleReferenceManifest(CompatibilityAssetBundleManifest manifest, string bundleDirectory)
//         {
//             _manifest = manifest;
//             _bundleDirectory = bundleDirectory;
//         }
//
//         public string[] GetAllAssetBundles()
//         {
//             return _manifest?.GetAllAssetBundles();
//         }
//
//         public string[] GetDirectDependencies(string assetBundleName)
//         {
//             return _manifest?.GetDirectDependencies(assetBundleName);
//         }
//
//         public string[] GetAllDependencies(string assetBundleName)
//         {
//             return _manifest?.GetAllDependencies(assetBundleName);
//         }
//
//         public void UnloadAsset()
//         {
//             if (_manifest == null)
//                 return;
//
//             Resources.UnloadAsset(_manifest);
//             _manifest = null;
//         }
//
//         public string GetBundlePath(string assetBundleName)
//         {
//             return System.IO.Path.Combine(_bundleDirectory, assetBundleName);
//         }
//     }
// }