using UnityEngine;

namespace PowerCellStudio.Editor
{
    public class UnityBundleReferenceManifest : IBundleReferenceManifest
    {
        private AssetBundleManifest _manifest;
        private string _bundleDirectory;

        public UnityBundleReferenceManifest(AssetBundleManifest manifest, string bundleDirectory)
        {
            _manifest = manifest;
            _bundleDirectory = bundleDirectory;
        }
        
        public string[] GetAllAssetBundles()
        {
            return _manifest.GetAllAssetBundles();
        }

        public string[] GetDirectDependencies(string assetBundleName)
        {
            return _manifest.GetDirectDependencies(assetBundleName);
        }

        public string[] GetAllDependencies(string assetBundleName)
        {
            return _manifest.GetAllDependencies(assetBundleName);
        }

        public void UnloadAsset()
        {
            Resources.UnloadAsset(_manifest);
        }

        public string GetBundlePath(string assetBundleName)
        {
            return System.IO.Path.Combine(_bundleDirectory, assetBundleName);
        }
    }
}