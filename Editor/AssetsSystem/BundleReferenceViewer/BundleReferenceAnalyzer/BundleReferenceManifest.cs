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
            if (!Directory.Exists(bundleDi))
            {
                EditorUtility.DisplayDialog("目标文件夹不存在", $"文件夹 {bundleDi} 不存在.", "OK");
                return;
            }

            bundleDirectory = bundleDi;
            var manifestBundle = AssetBundle.LoadFromFile($"{bundleDi}/{manifestName}.bundle");
            if (manifestBundle == null)
            {
                EditorUtility.DisplayDialog("AssetBundleManifest 不存在", $"AssetBundleManifest分包 {manifestName} 不存在.", "OK");
                return;
            }
            manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            manifestBundle.Unload(false);
        }
        
        internal static void ClearManifest()
        {
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