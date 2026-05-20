using System;
using System.Collections;
using System.Collections.Generic;

namespace PowerCellStudio
{
    public class ChunkDataQueryer<TKey, TData> 
    {
        private string _dataFilePath;
        private ChunkDataMap<TKey, TData> _dataMap;
        private IChunkIndexer<TKey> _indexer;
        private Func<TData, TKey> _keySelector;
        private ChunkDataOptions _options;

        public void Prepare(string indexFilePath, string dataFilePath, Func<TData, TKey> keySelector,
            Func<TKey, bool> keyPredicate, Func<TKey, int> keyToChunkIndex, ChunkDataOptions options = null)
        {
            _dataFilePath = dataFilePath;
            _keySelector = keySelector;
            _options = options;
            var indexer = new ChunkCustomIndexer<TKey>(keyPredicate, keyToChunkIndex);
            _dataMap = new ChunkDataMap<TKey, TData>();
            var reader = ChunkReader.ReadIndexFile<TKey>(indexFilePath, options);
            indexer.Init(reader);
            _indexer = indexer;
            _dataMap.Init(_indexer.chunkCount);
        }

        public void Prepare(string indexFilePath, string dataFilePath, Func<TData, TKey> keySelector, ChunkDataOptions options = null)
        {
            _dataFilePath = dataFilePath;
            _keySelector = keySelector;
            _options = options;
            var (indexer, dataMap) = CreateChunkTools(indexFilePath, options);
            _indexer = indexer;
            _dataMap = dataMap;
        }
        
        public IEnumerator PrepareYieldInstruction(string indexFilePath, string dataFilePath, Func<TData, TKey> keySelector, int operationUnit = 512, ChunkDataOptions options = null)
        {
            _dataFilePath = dataFilePath;
            _keySelector = keySelector;
            _options = options;
            var indexer = new ChunkIndexer<TKey>();
            _dataMap = new ChunkDataMap<TKey, TData>();
            var reader = ChunkReader.ReadIndexFile<TKey>(indexFilePath, options);
            yield return indexer.InitAsync(reader, operationUnit);
            _indexer = indexer;
            _dataMap.Init(_indexer.chunkCount);
        }
        
        private void LoadChunk(int chunkIndex, Action<TData> onAdd)
        {
            var offset = _indexer.GetChunkOffset(chunkIndex);
            if (offset < 0) return;
            var dataSource = ChunkReader.ReadChunkData<TData>(_dataFilePath, offset, _options);
            _dataMap.AddChunk(chunkIndex, dataSource, _keySelector, onAdd);
        }

        public TData Get(TKey key, Action<TData> onAdd)
        {
            var chunkIndex = _indexer.GerChunkIndex(key);
            if (chunkIndex < 0) return default;
            if (!_dataMap.IsChunkLoaded(chunkIndex))
            {
                LoadChunk(chunkIndex, onAdd);
            }
            return _dataMap.GetData(chunkIndex, key);
        }

        public IEnumerable<TData> GetByKey(Func<TKey, bool> keyPredicate, Action<TData> onAdd)
        {
            if (keyPredicate == null) yield break;
            var indexer = _indexer as ChunkIndexer<TKey>;
            if (indexer == null) yield break;
            var chunkIndexes = indexer.GetChunkIndexByKey(keyPredicate);
            foreach (var chunkIndex in chunkIndexes)
            {
                if (!_dataMap.IsChunkLoaded(chunkIndex))
                {
                    var offset = _indexer.GetChunkOffset(chunkIndex);
                    if (offset < 0) continue;
                    var dataSource = ChunkReader.ReadChunkData<TData>(_dataFilePath, offset, _options);
                    foreach (var confBase in dataSource)
                    {
                        if (!keyPredicate.Invoke(_keySelector(confBase))) continue;
                        yield return confBase;
                    }
                }
                else
                {
                    foreach (var data in _dataMap.GetAllData(chunkIndex))
                    {
                        if (!keyPredicate.Invoke(_keySelector(data))) continue;
                        yield return data;
                    }
                }
            }
        }

        public IEnumerable<TData> GetAll()
        {
            for (var i = 0; i < _indexer.chunkCount; i++)
            {
                if (_dataMap.IsChunkLoaded(i))
                {
                    foreach (var confBase in _dataMap.GetAllData(i))
                        yield return confBase;
                    continue;
                }

                var offset = _indexer.GetChunkOffset(i);
                if (offset < 0) continue;
                var dataSource = ChunkReader.ReadChunkData<TData>(_dataFilePath, offset, _options);
                foreach (var confBase in dataSource)
                    yield return confBase;
            }
        }

        public IEnumerable<TData> Find(Func<TData, bool> predicate)
        {
            if (predicate == null) yield break;
            var dataSource = GetAll();
            foreach (var data in dataSource)
            {
                if (!predicate.Invoke(data)) continue;
                yield return data;
            }
        }

        public void TryClearUnused(Action<IEnumerable<TData>> onRemove)
        {
            _dataMap.TryClearUnused(onRemove);
        }

        public void Clear(Action<IEnumerable<TData>> onRemove)
        {
            _dataMap.ClearAll(onRemove);
            _indexer.Clear();
        }
        
        public static (ChunkIndexer<TKey> indexer, ChunkDataMap<TKey, TData> dataMap) CreateChunkTools(string indexFilePath, ChunkDataOptions options = null)
        {
            var chunkIndexer = new ChunkIndexer<TKey>();
            var dataMap = new ChunkDataMap<TKey, TData>();
            var reader = ChunkReader.ReadIndexFile<TKey>(indexFilePath, options);
            chunkIndexer.Init(reader);
            dataMap.Init(chunkIndexer.chunkCount);
            return (chunkIndexer, dataMap);
        }
    }
}