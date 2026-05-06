using System;

namespace PowerCellStudio
{
    public class CacheRef<T> : IDisposable where T : UnityEngine.Object
    {
        private T _asset;
        private int refCount;
        
        public CacheRef(T asset)
        {
            _asset = asset;
            refCount = 0;
        }
        
        public T asset => _asset;
        
        public bool isAlive => refCount > 0;

        public void AddRef(int addValue)
        {
            if (!_asset) return;
            if (refCount < 0) refCount = 0;
            refCount += addValue;
        }

        public void DeRef(int delValue)
        {
            if (!_asset) return;
            if (refCount < 1) return;
            refCount -= delValue;
        }

        public void Dispose()
        {
            _asset =  null;
            refCount = 0;
        }
    }
}