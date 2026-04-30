using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public class LoadedCache<T> where T : Object
    {
        private Dictionary<string, CacheRef<T>> _cache;

        public LoadedCache()
        {
            _cache = new Dictionary<string, CacheRef<T>>();
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
            var assetRef =  new CacheRef<T>(asset);
            _cache[assetPath] = assetRef;
        }

        public void RemoveCache(string assetPath)
        {
            _cache.Remove(assetPath);
        }

        public void AddRef(string assetPath)
        {
            if (_cache.TryGetValue(assetPath, out var assetRef))
            {
                assetRef.AddRef();
            }
        }

        public bool TryDelRef(string assetPath, out T asset)
        {
            if (_cache.TryGetValue(assetPath, out var assetRef))
            {
                asset = assetRef.asset;
                assetRef.DeRef();
                return !assetRef.isAlive;
            }
            asset = null;
            return false;
        }
    }
}