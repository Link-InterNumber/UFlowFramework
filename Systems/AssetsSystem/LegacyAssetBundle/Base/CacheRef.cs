using System;

namespace PowerCellStudio
{
    public class CacheRef<T> : IDisposable where T : UnityEngine.Object
    {
        private T _asset;
        private int _refCount;
        public int refCount => _refCount;
        
        public CacheRef(T asset)
        {
            _asset = asset;
            _refCount = 0;
        }
        
        public T asset => _asset;
        
        public bool isAlive => _refCount > 0;

        public void AddRef(int addValue)
        {
            if (!_asset) return;
            if (_refCount < 0) _refCount = 0;
            _refCount += addValue;
        }

        public void DeRef(int delValue)
        {
            if (!_asset) return;
            if (_refCount < 1) return;
            _refCount -= delValue;
        }

        public void Dispose()
        {
            _asset =  null;
            _refCount = 0;
        }
    }
}