using System.IO;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio.Editor
{
    public class BundleReferenceManifest
    {
        internal static string bundleDirectory;
        internal static AssetBundleManifest manifest;

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
                manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                if (manifest == null)
                {
                    EditorUtility.DisplayDialog("AssetBundleManifest 读取失败", "目标分包中不存在 AssetBundleManifest 资源.", "OK");
                    return;
                }

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
                Resources.UnloadAsset(manifest);
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