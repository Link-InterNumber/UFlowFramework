namespace PowerCellStudio.Editor
{
    public interface IBundleReferenceManifest
    {
        public string[] GetAllAssetBundles();

        public string[] GetDirectDependencies(string assetBundleName);

        public string[] GetAllDependencies(string assetBundleName);

        public void UnloadAsset();

        public string GetBundlePath(string assetBundleName);
    }
}