using System;
using System.Collections;
using System.Collections.Generic;

namespace PowerCellStudio
{
    public class ChunkDataQueryer<TKey, TData> 
    {
        private string _dataFilePath;
        private ChunkDataMap<TKey, TData> _dataMap;
        private ChunkIndexer<TKey> _indexer;
        private Func<TData, TKey> _keySelector;

        public void Prepare(string indexFilePath, string dataFilePath, Func<TData, TKey> keySelector)
        {
            _dataFilePath = dataFilePath;
            _keySelector = keySelector;
            var (indexer, dataMap) = CreateChunkTools<TKey, TData>(indexFilePath);
            _indexer = indexer;
            _dataMap = dataMap;
        }
        
        public IEnumerator PrepareYieldInstruction(string indexFilePath, string dataFilePath, Func<TData, TKey> keySelector, int operationUnit = 512)
        {
            _dataFilePath = dataFilePath;
            _keySelector = keySelector;
            var reader = ChunkReader.ReadIndexFile<TKey>(indexFilePath);
            yield return _indexer.InitAsync(reader, operationUnit);
            _dataMap.Init(_indexer.chunkCount);
        }
        
        private void LoadChunk(int chunkIndex, Action<TData> onAdd)
        {
            var offset = _indexer.GetChunkOffset(chunkIndex);
            if (offset < 0) return;
            var dataSource = ChunkReader.ReadChunkData<TData>(_dataFilePath, offset);
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
            var chunkIndexes = _indexer.GetChunkIndexByKey(keyPredicate);
            foreach (var chunkIndex in chunkIndexes)
            {
                if (!_dataMap.IsChunkLoaded(chunkIndex))
                {
                    LoadChunk(chunkIndex, onAdd);
                }
                foreach (var data in _dataMap.GetAllData(chunkIndex))
                {
                    if (!keyPredicate.Invoke(_keySelector(data))) continue;
                    yield return data;
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
                var dataSource = ChunkReader.ReadChunkData<TData>(_dataFilePath, offset);
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

        public void TryClearUnused()
        {
            _dataMap.TryClearUnused();
        }

        public void Clear()
        {
            _dataMap.ClearAll();
            _indexer.Clear();
        }
        
        public static (ChunkIndexer<TKey> indexer, ChunkDataMap<TKey, TData> dataMap) CreateChunkTools<TKey, TData>(string indexFilePath)
        {
            var chunkIndexer = new ChunkIndexer<TKey>();
            var dataMap = new ChunkDataMap<TKey, TData>();
            var reader = ChunkReader.ReadIndexFile<TKey>(indexFilePath);
            chunkIndexer.Init(reader);
            dataMap.Init(chunkIndexer.chunkCount);
            return (chunkIndexer, dataMap);
        }
    }
}