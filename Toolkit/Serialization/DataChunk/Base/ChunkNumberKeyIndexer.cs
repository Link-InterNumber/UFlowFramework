using System;
using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public class ChunkNumberKeyIndexer<TKey> : IChunkIndexer<TKey>
    {
        private List<TKey> _keys;
        private Dictionary<int, long> _offsetMap;
        
        public int chunkCount  => _offsetMap.Count;

        public ChunkNumberKeyIndexer()
        {
            _keys = new List<TKey>();
            _offsetMap = new Dictionary<int, long>();
        }

        public void Init(IEnumerable<(int index, long offset, TKey[] keys)> chunks)
        {
            if (chunks == null) return;
            foreach (var chunk in chunks)
            {
                _offsetMap[chunk.index] = chunk.offset;
                _keys.Add(chunk.keys[0]);
            }
        }
        
        public void Clear()
        {
            _keys?.Clear();
            _offsetMap?.Clear();
        }

        public int GerChunkIndex(TKey key)
        {
            // 使用二分查找来提高查询效率
            // 查找_keys中第一个小于等于key的元素的索引
            int left = 0;
            int right = _keys.Count - 1;
            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                if (Comparer<TKey>.Default.Compare(_keys[mid], key) <= 0)
                {
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }
            return right >= 0 ? right : -1;
        }

        public IEnumerable<int> GetChunkIndexByKey(Func<TKey, bool> keyPredicate)
        {
            if (keyPredicate == null) yield break;
            Debug.LogWarning("ChunkNumberKeyIndexer does not support GetChunkIndexByKey operation, returning all chunk indices.");
            foreach (var chunkIndex in _offsetMap.Keys)
            {
                yield return chunkIndex;
            }
        }

        public long GetChunkOffset(int chunkIndex)
        {
            return _offsetMap != null && _offsetMap.TryGetValue(chunkIndex, out var offset) ? offset : -1;
        }
    }
}