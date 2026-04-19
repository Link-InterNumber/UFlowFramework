using System;
using System.Collections;
using System.Collections.Generic;

namespace PowerCellStudio
{
    public interface IConfBaseCollections
    {
        AssetLoadStatus loadStatus { get; }
        void Prepare();
        IEnumerator PrepareAsync();
        void Release(bool force);
    }
    
    public abstract class ConfBaseCollections<TData, TKey> : IConfBaseCollections
        where TData : ConfBase
    {
        public AssetLoadStatus loadStatus => _loadStatus;
        protected AssetLoadStatus _loadStatus = AssetLoadStatus.Unload;
        protected string _assetPath;
        protected string _idxFilePath;
        protected ConfChunkIndexer<TKey> _chunkIndexer = new();
        protected ConfRef<TData, TKey> _confRef = new();
        protected int _refCount = 0;

        protected abstract TKey GetKey(TData data);

        protected abstract void OnAddData(TData data);

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
            var reader = ChunkReader.ReadIndexFile<TKey>(_idxFilePath);
            _chunkIndexer.Init(reader);
            _confRef.Init(_chunkIndexer.chunkCount);
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
            var reader = ChunkReader.ReadIndexFile<TKey>(_idxFilePath);
            _loadStatus = AssetLoadStatus.Loading;
            yield return _chunkIndexer.InitAsync(reader);
            _confRef.Init(_chunkIndexer.chunkCount);
            _refCount = 1;
            _loadStatus = AssetLoadStatus.Loaded;
        }
        
        public void Release(bool force)
        {
            if (_refCount == 0) return;
            _refCount--;
            if (_refCount > 0)
            {
                _confRef.TryClearUnused();
                return;
            }
            _confRef.ClearAll();
            _chunkIndexer.Clear();
            _loadStatus = AssetLoadStatus.Unload;
        }
        
        protected void LoadChunk(int chunkIndex)
        {
            var offset = _chunkIndexer.GetChunkOffset(chunkIndex);
            if (offset < 0) return;
            var confProvider = ChunkReader.ReadChunkData<TData>(_assetPath, offset);
            _confRef.AddChunk(chunkIndex, confProvider, GetKey, OnAddData);
        }

        public TData Get(TKey key)
        {
            if (!CheckLoadStatus()) return null;
            var chunkIndex = _chunkIndexer.GerChunkIndex(key);
            if (chunkIndex < 0) return null;
            if (!_confRef.IsChunkLoaded(chunkIndex))
            {
                LoadChunk(chunkIndex);
            }
            return _confRef.GetConfData(chunkIndex, key);
        }
        
        public IEnumerable<TData> GetByKey(Func<TKey, bool> keyPredicate)
        {
            if (!CheckLoadStatus()) yield break;
            if (keyPredicate == null) yield break;
            var chunkIndexes = _chunkIndexer.GetChunkIndexByKey(keyPredicate);
            foreach (var chunkIndex in chunkIndexes)
            {
                if (!_confRef.IsChunkLoaded(chunkIndex))
                {
                    LoadChunk(chunkIndex);
                }
                foreach (var confBase in _confRef.GetAllConfData(chunkIndex))
                {
                    if (!keyPredicate.Invoke(GetKey(confBase))) continue;
                    yield return confBase;
                }
            }
        }

        public IEnumerable<TData> GetAll()
        {
            if (!CheckLoadStatus()) yield break;
            for (var i = 0; i < _chunkIndexer.chunkCount; i++)
            {
                if (_confRef.IsChunkLoaded(i))
                {
                    foreach (var confBase in _confRef.GetAllConfData(i))
                        yield return confBase;
                    continue;

                }

                var offset = _chunkIndexer.GetChunkOffset(i);
                if (offset < 0) continue;
                var confs = ChunkReader.ReadChunkData<TData>(_assetPath, offset);
                foreach (var confBase in confs)
                    yield return confBase;
            }
        }
        
        public IEnumerable<TData> Find(Func<TData, bool> predicate)
        {
            if (predicate == null) yield break;
            var allConfs = GetAll();
            foreach (var conf in allConfs)
            {
                if (!predicate.Invoke(conf)) continue;
                yield return conf;
            }
        }

    }
}