using System.IO;
using UnityEditor;
using UnityEngine;
// using UnityEngine.Build.Pipeline;

namespace PowerCellStudio.Editor
{
    public class BundleReferenceManifest
    {
        internal static string bundleDirectory;
        internal static IBundleReferenceManifest manifest;

        internal static void PrepareManifest(string bundleDi, string manifestName)
        {
            ClearManifest();

            if (string.IsNullOrWhiteSpace(bundleDi) || string.IsNullOrWhiteSpace(manifestName))
            {
                EditorUtility.DisplayDialog("参数不完整", "Bundle 目录和 Manifest 名称不能为空.", "OK");
                return;
            }

            bundleDi = Path.GetFullPath(bundleDi);
            if (!Directory.Exists(bundleDi))
            {
                EditorUtility.DisplayDialog("目标文件夹不存在", $"文件夹 {bundleDi} 不存在.", "OK");
                return;
            }

            var manifestPath = Path.Combine(bundleDi, $"{manifestName}");
            var manifestBundle = AssetBundle.LoadFromFile(manifestPath);
            if (manifestBundle == null)
            {
                EditorUtility.DisplayDialog("AssetBundleManifest 不存在", $"AssetBundleManifest分包 {manifestName} 不存在.", "OK");
                return;
            }
            try
            {
                // var compatibilityManifest = manifestBundle.LoadAsset<CompatibilityAssetBundleManifest>(manifestBundle.GetAllAssetNames()[0]);
                // if (compatibilityManifest == null)
                // {
                //     EditorUtility.DisplayDialog("CompatibilityAssetBundleManifest 读取失败", "目标分包中不存在 CompatibilityAssetBundleManifest 资源.", "OK");
                //     return;
                // }
                //
                // if (compatibilityManifest != null)
                // {
                //     manifest = new CompatibilityBundleReferenceManifest(compatibilityManifest, bundleDi);
                //     bundleDirectory = bundleDi;
                //     return;
                // }

                var unityManifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                if (unityManifest == null)
                {
                    EditorUtility.DisplayDialog("AssetBundleManifest 读取失败", "目标分包中不存在 AssetBundleManifest 资源.", "OK");
                    return;
                }
                manifest = new UnityBundleReferenceManifest(unityManifest, bundleDi);
                bundleDirectory = bundleDi;
            }
            finally
            {
                manifestBundle.Unload(false);
            }
        }
        
        internal static void ClearManifest()
        {
            if (manifest != null)
                manifest.UnloadAsset();
            manifest = null;
            bundleDirectory = null;
            var loadedBundles = AssetBundle.GetAllLoadedAssetBundles();
            foreach (var loadedBundle in loadedBundles)
            {
                loadedBundle.Unload(true);
            }
            Resources.UnloadUnusedAssets();
        }
    }
}