using System.Collections.Generic;

namespace PowerCellStudio
{
    public class BundleLoadingHolder
    {
        private HashSet<string> _loadingBundles;

        public BundleLoadingHolder()
        {
            _loadingBundles = new HashSet<string>();
        }

        public bool IsLoading(string bundleName)
        {
            return _loadingBundles.Contains(bundleName);
        }

        public void AddLoadingHandle(string bundleName)
        {
            _loadingBundles.Add(bundleName);
        }

        public void SetLoaded(string bundleName)
        {
            _loadingBundles.Remove(bundleName);
        }
    }
}