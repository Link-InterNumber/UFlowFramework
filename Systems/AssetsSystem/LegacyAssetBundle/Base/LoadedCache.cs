using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace PowerCellStudio
{
    public class LoadedCache<T> where T : Object
    {
        private Dictionary<string, CacheRef<T>> _cache;

        public LoadedCache()
        {
            _cache = new Dictionary<string, CacheRef<T>>();
        }

        public bool IsLoaded(string assetPath)
        {
            return _cache.ContainsKey(assetPath) && _cache[assetPath].asset;
        }

        public int GetRefCount(string assetPath)
        {
            return _cache.TryGetValue(assetPath, out var assetRef) ? assetRef.refCount : 0;
        }

        public bool TryGetCache(string assetPath, out T asset)
        {
            if (_cache.TryGetValue(assetPath, out var assetRef))
            {
                asset = assetRef.asset;
                return true;
            }
            asset = null;
            return false;
        }

        public void AddCache(string assetPath, T asset)
        {
            if (!asset) return;
            var assetRef =  new CacheRef<T>(asset);
            _cache[assetPath] = assetRef;
        }

        public void RemoveCache(string assetPath)
        {
            _cache.Remove(assetPath);
        }

        public IEnumerable<KeyValuePair<string, CacheRef<T>>> GetAll()
        {
            foreach (var cacheValue in _cache)
            {
                yield return cacheValue;
            }
        }

        public IEnumerable<T> ClearUnusedAssets()
        {
            var keysToRemove = ListPool<string>.Get();
            foreach (var cacheValue in _cache)
            {
                if (!cacheValue.Value.isAlive)
                {
                    keysToRemove.Add(cacheValue.Key);
                    yield return cacheValue.Value.asset;
                }
            }
            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }
            ListPool<string>.Release(keysToRemove);
        }
        
        public void Clear()
        {
            _cache.Clear();
        }

        public void AddRef(string assetPath, int addValue)
        {
            if (_cache.TryGetValue(assetPath, out var assetRef))
            {
                assetRef.AddRef(addValue);
            }
        }

        public bool TryDelRef(string assetPath, int delValue, out T asset)
        {
            if (_cache.TryGetValue(assetPath, out var assetRef))
            {
                asset = assetRef.asset;
                assetRef.DeRef(delValue);
                return !assetRef.isAlive;
            }
            asset = null;
            return false;
        }
    }
}