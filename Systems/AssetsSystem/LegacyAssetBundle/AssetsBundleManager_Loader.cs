using System.Collections.Generic;
using System.Linq;
using UnityEngine.Pool;

namespace PowerCellStudio
{
    public partial class AssetsBundleManager
    {
        private ObjectPool<BundleAssetLoader> _pool;
        private Dictionary<long, BundleAssetLoader> _activeLoader;
        
        public IAssetLoader SpawnLoader(string tag= "")
        {
            var loader = _pool.Get();
            loader.tag = tag;
            _activeLoader.Add(loader.index, loader);
            return loader;
        }

        public void DeSpawnLoader(IAssetLoader assetLoader)
        {
            if (assetLoader == null) return;
            var assetBundleLoader = assetLoader as BundleAssetLoader;
            if (assetBundleLoader == null) return;
            _activeLoader.Remove(assetBundleLoader.index);
            if (!assetBundleLoader.spawned)
            {
                assetBundleLoader.Deinit();
                return;
            }
            _pool.Release(assetBundleLoader);
        }

        public void DeSpawnAllLoader()
        {
            while (_activeLoader.Count > 0)
            {
                var loader = _activeLoader.First().Value;
                _activeLoader.Remove(loader.index);
                if(!loader.spawned)
                {
                    loader.Deinit();
                    continue;
                }
                _pool.Release(loader);
            }
        }

        public void DeSpawnLoaderByTag(string tag)
        {
            var loaders = _activeLoader.Where(o => o.Value.tag.Equals(tag)).ToArray();
            if(loaders.Length == 0) return;
            foreach (var addressableLoader in loaders)
            {
                DeSpawnLoader(addressableLoader.Value);
            }
        }
    }
}