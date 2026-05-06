
using System.IO;
using UnityEngine;

namespace PowerCellStudio
{
    public class NewAssetBundleManager : SingletonBase<NewAssetBundleManager>
    {
        private AssetLoadingHolder<Object> _loadingAssets;
        internal AssetLoadingHolder<Object> loadingAssets => _loadingAssets;

        private BundleLoadingHolder _loadingBundles;
        internal BundleLoadingHolder loadingBundles => _loadingBundles;

        private LoadedCache<Object> _cachedAsset;
        internal LoadedCache<Object> cachedAsset => _cachedAsset;
        
        private LoadedCache<AssetBundle> _cachedBundle;
        internal LoadedCache<AssetBundle> cachedBundle => _cachedBundle;

        private AssetBundleIndex _indexer;
        internal AssetBundleIndex indexer => _indexer;
        
        private BundleDependenceMap _dependenceMap;
        internal BundleDependenceMap dependenceMap => _dependenceMap;
        
        private RemovedAssetHolder _removedAssetHolder;

        private RemoteBundleManifest _remoteBundleManifest;
        internal RemoteBundleManifest remoteBundleManifest => _remoteBundleManifest;

#region 引用计数
        public void TryAddAssetRef(string assetPath, int addValue)
        {
            _cachedAsset.AddRef(assetPath, addValue);
        }

        public void TryDelAssetRef(string assetPath, int delValue)
        {
            if (!_cachedAsset.TryDelRef(assetPath, delValue, out var asset))
                return;
            _cachedAsset.RemoveCache(assetPath);
            // 直接卸载资源
            Resources.UnloadAsset(asset);
            var bundleName = _indexer.GetBundleNameByAsset(assetPath);
            if (!string.IsNullOrEmpty(bundleName))
            {
                TryDelBundleRef(bundleName, 1);
            }
        }

        public void TryAddBundleRef(string bundleName, int addValue)
        {
            _cachedBundle.AddRef(bundleName, addValue);
            var dependencies = _dependenceMap.GetBundleDependencies(bundleName);
            for (var i = 0; i < dependencies.Length; i++)
            {
                var dependency = dependencies[i];
                _cachedBundle.AddRef(dependency, addValue);
            }
        }

        public void TryDelBundleRef(string bundleName, int delValue)
        {
            if (_cachedBundle.TryDelRef(bundleName, delValue, out var bundle))
            {
                _cachedBundle.RemoveCache(bundleName);
                _removedAssetHolder.Push(null, bundle, 10f);
            }
            var dependencies = _dependenceMap.GetBundleDependencies(bundleName);
            for (var i = dependencies.Length - 1; i >= 0; i--)
            {
                var dependency = dependencies[i];
                if (_cachedBundle.TryDelRef(dependency, delValue, out var ab))
                {
                    _cachedBundle.RemoveCache(dependency);
                    _removedAssetHolder.Push(null, ab, 10f);
                }
            }
        }
#endregion

#region 操作已经加载的资源
        public void SetAssetLoaded(string assetPath, Object asset)
        {
            if (asset == null) return;
            _cachedAsset.AddCache(assetPath, asset);
            _loadingAssets.SetLoaded(assetPath, asset);
        }

        public void SetBundleLoaded(string bundleName, AssetBundle bundle)
        {
            if (bundle == null) return;
            _cachedBundle.AddCache(bundleName, bundle);
            _loadingBundles.SetLoaded(bundleName, bundle, out var loadPlan);
            TryAddBundleRef(bundleName, loadPlan.Count);
            foreach (var (assetPath, releaseBundleOnTime) in loadPlan)
            {
                
            }
        }

        public bool TryGetLoadedBundle(string bundleName, out AssetBundle bundle)
        {
            _cachedBundle.TryGetCache(bundleName, out bundle);
            if (bundle == null && _removedAssetHolder.IsHolding(bundleName))
            {
                _removedAssetHolder.TryGetBundle(bundleName, out bundle);
                var dependencies = _dependenceMap.GetBundleDependencies(bundleName);
                for (var i = 0; i < dependencies.Length; i++)
                {
                    var dependency = dependencies[i];
                    if (_removedAssetHolder.TryGetBundle(dependency, out var removedAb))
                    {
                        _cachedBundle.AddCache(dependency, removedAb);
                        break;
                    }
                }
                _cachedBundle.AddCache(bundleName, bundle);
                return bundle != null;
            }
            return bundle != null;
        }
#endregion

        public string GetBundlePath(string bundleName)
        {
            if (remoteBundleManifest.IsBundleRemote(bundleName))
                return Path.Combine(Application.persistentDataPath, "_bundleFoldName", bundleName);
            return Path.Combine(Application.streamingAssetsPath, "_bundleFoldName", bundleName);
        }
    }
}