using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PowerCellStudio
{
    public class ChunkCustomIndexer<TKey> : IChunkIndexer<TKey>
    {
        private Func<TKey, bool> _keyPredicate;
        private Func<TKey, int> _keyToChunkIndex;
        private Dictionary<int, long> _offsetMap;

        public int chunkCount => _offsetMap.Count;

        /// <summary>
        /// 自定义索引器，用户可以通过传入一个判断key是否满足条件的函数和一个将key转换为chunkIndex的函数来实现自定义的索引逻辑。
        /// </summary>
        /// <param name="keyPredicate">判断key是否在数据范围内的函数 </param>
        /// <param name="keyToChunkIndex">将key转换为chunkIndex的函数</param>
        public ChunkCustomIndexer(Func<TKey, bool> keyPredicate, Func<TKey, int> keyToChunkIndex)
        {
            _keyPredicate = keyPredicate;
            _keyToChunkIndex = keyToChunkIndex;
            _offsetMap = new Dictionary<int, long>();
        }

        /// <summary>
        /// 接收ChunkReader.ReadIndexFile<TKey>方法读取的分块索引信息和关联键数组，并将分块索引和偏移量存储在内部字典中，以供后续查询使用。
        /// </summary>
        /// <param name="chunks"></param>
        public void Init(IEnumerable<(int index, long offset, TKey[] keys)> chunks)
        {
            if (chunks == null) return;
            foreach (var chunk in chunks)
            {
                _offsetMap[chunk.index] = chunk.offset;
            }
        }
        
        public void Clear()
        {
            _offsetMap?.Clear();
            _keyToChunkIndex = null;
            _keyPredicate = null;
        }

        public int GerChunkIndex(TKey key)
        {
            if (_keyPredicate != null && _keyPredicate.Invoke(key))
            {
                return _keyToChunkIndex != null ? _keyToChunkIndex.Invoke(key) : -1;
            }
            return -1;
        }

        public IEnumerable<int> GetChunkIndexByKey(Func<TKey, bool> keyPredicate)
        {
            if (keyPredicate == null) yield break;
            Debug.LogWarning("ChunkCustomIndexer does not support GetChunkIndexByKey operation, returning all chunk indices.");
            foreach (var chunkIndex in _offsetMap.Keys.OrderBy(index => index))
            {
                yield return chunkIndex;
            }
        }

        public IEnumerable<int> GetAllChunkIndexes()
        {
            return _offsetMap.Keys.OrderBy(index => index);
        }

        public long GetChunkOffset(int chunkIndex)
        {
            return _offsetMap != null && _offsetMap.TryGetValue(chunkIndex, out var offset) ? offset : -1;
        }
    }
}