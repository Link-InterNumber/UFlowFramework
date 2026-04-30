
using Mono.Cecil;
using UnityEngine;

namespace PowerCellStudio
{
    public class NewAssetBundleManager : SingletonBase<NewAssetBundleManager>
    {
        private LoadingHolder<Object> _loadingAssets;
        internal LoadingHolder<Object> loadingAssets => _loadingAssets;

        private LoadingHolder<AssetBundle> _loadingBundles;
        internal LoadingHolder<AssetBundle> loadingBundles => _loadingBundles;

        private LoadedCache<Object> _assetCache;
        internal LoadedCache<Object> assetCache => _assetCache;
        
        private LoadedCache<AssetBundle> _bundleCache;
        internal LoadedCache<AssetBundle> bundleCache => _bundleCache;

        private AssetBundleIndex _indexer;
        internal AssetBundleIndex indexer => _indexer;
        
        private BundleDependenceMap _dependenceMap;
        internal BundleDependenceMap dependenceMap => _dependenceMap;
        
        private RemovedAssetHolder _removedAssetHolder;

        public void TryAddAssetRef(string assetPath)
        {
            _assetCache.AddRef(assetPath);
        }

        public void TryDelAssetRef(string assetPath)
        {
            if (!_assetCache.TryDelRef(assetPath, out var asset))
                return;
            _assetCache.RemoveCache(assetPath);
            // 直接卸载资源
            Resources.UnloadAsset(asset);
            var bundleName = _indexer.GetBundleNameByAsset(assetPath);
            if (!string.IsNullOrEmpty(bundleName))
            {
                TryDelBundleRef(bundleName);
            }
        }

        public void TryAddBundleRef(string bundleName)
        {
            _bundleCache.AddRef(bundleName);
            var dependencies = _dependenceMap.GetBundleDependencies(bundleName);
            for (var i = 0; i < dependencies.Length; i++)
            {
                var dependency = dependencies[i];
                _bundleCache.AddRef(dependency);
            }
        }

        public void TryDelBundleRef(string bundleName)
        {
            if (_bundleCache.TryDelRef(bundleName, out var bundle))
            {
                _bundleCache.RemoveCache(bundleName);
                _removedAssetHolder.Push(null, bundle, 10f);
            }
            var dependencies = _dependenceMap.GetBundleDependencies(bundleName);
            for (var i = dependencies.Length - 1; i >= 0; i--)
            {
                var dependency = dependencies[i];
                if (_bundleCache.TryDelRef(dependency, out var ab))
                {
                    _bundleCache.RemoveCache(dependency);
                    _removedAssetHolder.Push(null, ab, 10f);
                }
            }
        }
    }
}