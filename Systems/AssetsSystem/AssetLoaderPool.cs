using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace PowerCellStudio
{
    public class AssetLoaderPool
    {
        private ObjectPool<IAssetLoader> _pool;
        private Dictionary<int, IAssetLoader> _activeLoader;

        private Func<IAssetLoader> _spawnFunc;

        public AssetLoaderPool(Func<IAssetLoader> spawnFunc, int initialSize = 10)
        {
            _spawnFunc = spawnFunc;
            _activeLoader = new Dictionary<int, IAssetLoader>();
            _pool = new ObjectPool<IAssetLoader>(_spawnFunc,
                loader => loader.Init(),
                loader => loader.Deinit(),
                loader => loader.Deinit(), true, initialSize, 30);
        }

        public IAssetLoader Spawn(string tag = "")  
        {
            var loader = _pool.Get();
            loader.tag = tag;
            _activeLoader.Add(loader.index, loader);
            return loader;
        }

        public void DeSpawn(IAssetLoader assetLoader)
        {
            if(assetLoader == null) return;
            if (assetLoader.IsAnyLoading())
            {
                // 等待加载完成再释放
                AsyncManager.Run(WaitForLoadComplete(assetLoader));
                return;
            }
            _activeLoader.Remove(assetLoader.index);
            if(!assetLoader.spawned)
            {
                assetLoader.Deinit();
                return;
            }
            _pool.Release(assetLoader);
        }

        private IEnumerator WaitForLoadComplete(IAssetLoader assetLoader)
        {
            while (assetLoader != null && assetLoader.spawned && assetLoader.IsAnyLoading())
            {
                yield return null;
            }

            if (assetLoader == null)
            {
                yield break;
            }

            DeSpawn(assetLoader);
        }

        public void DeSpawnAllLoader()
        {
            var loaders = ListPool<IAssetLoader>.Get();
            loaders.AddRange(_activeLoader.Values);
            foreach (var loader in loaders)
            {
                DeSpawn(loader);
            }
            ListPool<IAssetLoader>.Release(loaders);
            _activeLoader.Clear();
        }

        public void DeSpawnLoaderByTag(string tag)
        {
            var loaders = ListPool<IAssetLoader>.Get();
            foreach (var activeLoaderValue in _activeLoader.Values)
            {
                if (activeLoaderValue != null && activeLoaderValue.tag == tag)
                {
                    loaders.Add(activeLoaderValue);
                }
            }
            foreach (var loader in loaders)
            {
                DeSpawn(loader);
            }
            ListPool<IAssetLoader>.Release(loaders);
        }

        public IEnumerable<IAssetLoader> GetAllActiveLoaders()
        {
            foreach (var loader in _activeLoader.Values)
            {
                yield return loader;
            }
        }
    }
}