using System;
using System.Collections;
using System.Collections.Generic;

namespace PowerCellStudio
{
    public interface IConfBaseCollections
    {
        string assetPath { get; }
        string idxFilePath { get; }
        AssetLoadStatus loadStatus { get; }
        void Prepare();
        IEnumerator PrepareAsync();
        void Release(bool force);
    }
    
    public abstract class ConfBaseCollections<TKey, TData> : IConfBaseCollections
        where TData : ConfBase
    {
        public AssetLoadStatus loadStatus => _loadStatus;
        protected AssetLoadStatus _loadStatus = AssetLoadStatus.Unload;
        protected string _assetPath;
        public string assetPath => _assetPath;
        protected string _idxFilePath;
        public string idxFilePath => _idxFilePath;
        protected ChunkDataQueryer<TKey, TData> _confQueryer;
        protected int _refCount = 0;

        protected abstract TKey GetKey(TData data);

        protected abstract void OnAddData(TData data);
        
        protected abstract void OnRemoveData(IEnumerable<TData> data);

        protected bool CheckLoadStatus()
        {
            switch (_loadStatus)
            {
                case AssetLoadStatus.Loaded:
                    return true;
                case AssetLoadStatus.Loading:
                    ConfigLog.LogError($"{GetType().Name} is loading, please wait for it to be loaded!");
                    break;
                case AssetLoadStatus.Unload:
                    ConfigLog.LogError($"{GetType().Name} is not loaded yet!");
                    break;
            }
            return false;
        }
        
        public void Prepare()
        {
            if (_refCount > 0)
            {
                _refCount++;
                return;
            }
            _confQueryer = new ChunkDataQueryer<TKey, TData>();
            _confQueryer.Prepare(_idxFilePath,  _assetPath, GetKey);
            _refCount = 1;
            _loadStatus = AssetLoadStatus.Loaded;
        }
        
        public IEnumerator PrepareAsync()
        {
            if (_refCount > 0)
            {
                _refCount++;
                yield break;
            }
            _loadStatus = AssetLoadStatus.Loading;
            _confQueryer = new ChunkDataQueryer<TKey, TData>();
            yield return _confQueryer.PrepareYieldInstruction(_idxFilePath,  _assetPath, GetKey);
            _refCount = 1;
            _loadStatus = AssetLoadStatus.Loaded;
        }
        
        public void Release(bool force)
        {
            if (!CheckLoadStatus()) return;
            if (_refCount == 0) return;
            if (force)
            {
                _refCount = 0;
                _confQueryer.Clear(OnRemoveData);
                _loadStatus = AssetLoadStatus.Unload;
                return;
            }
            _refCount--;
            if (_refCount > 0)
            {
                _confQueryer.TryClearUnused(OnRemoveData);
                return;
            }
            _confQueryer.Clear(OnRemoveData);
            _loadStatus = AssetLoadStatus.Unload;
        }

        public TData Get(TKey key)
        {
            if (!CheckLoadStatus()) return null;
            return _confQueryer.Get(key, OnAddData);
        }
        
        public IEnumerable<TData> GetByKey(Func<TKey, bool> keyPredicate)
        {
            if (!CheckLoadStatus()) yield break;
            var confMatched = _confQueryer.GetByKey(keyPredicate, OnAddData);
            foreach (var confBase in confMatched)
            {
                yield return confBase;
            }
        }

        public IEnumerable<TData> GetAll()
        {
            if (!CheckLoadStatus()) yield break;
            var dataSource = _confQueryer.GetAll();
            foreach (var conf in dataSource)
            {
                yield return conf;
            }
        }
        
        public IEnumerable<TData> Find(Func<TData, bool> predicate)
        {
            if (!CheckLoadStatus()) yield break;
            var dataSource = _confQueryer.Find(predicate);
            foreach (var conf in dataSource)
            {
                yield return conf;
            }
        }

    }
}