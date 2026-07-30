namespace PowerCellStudio
{
    public partial class AssetsBundleManager
    {
        public IAssetLoader CreateLoader()
        {
            return new BundleAssetLoader(this);
        }
    }
}