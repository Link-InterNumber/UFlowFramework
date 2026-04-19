using System;
using System.Collections;
using System.Collections.Generic;

namespace PowerCellStudio
{
    public class ConfChunkIndexer<TKey>
    {
        private Dictionary<TKey, int> _keyMap;
        private Dictionary<int, long> _offsetMap;
        
        public int chunkCount => _offsetMap.Count;

        public ConfChunkIndexer()
        {
            _keyMap = new Dictionary<TKey, int>();
            _offsetMap = new Dictionary<int, long>();
        }

        public void Init(IEnumerable<(int index, long offset, TKey[] keys)> chunks)
        {
            if (chunks == null) return;
            foreach (var chunk in chunks)
            {
                for (var i = 0; i < chunk.keys.Length; i++)
                {
                    _keyMap[chunk.keys[i]] = chunk.index;
                }
                _offsetMap[chunk.index] = chunk.offset;
            }
        }
        
        public IEnumerator InitAsync(IEnumerable<(int index, long offset, TKey[] keys)> chunks)
        {
            if (chunks == null) yield break;
            var operationUnit = 512;
            var count = 0;
            foreach (var chunk in chunks)
            {
                for (var i = 0; i < chunk.keys.Length; i++)
                {
                    _keyMap[chunk.keys[i]] = chunk.index;
                    count++;
                    if (count >= operationUnit)
                    {
                        count = 0;
                        yield return null;
                    }
                }
                _offsetMap[chunk.index] = chunk.offset;
            }
        }
        
        public void Clear()
        {
            _keyMap.Clear();
            _offsetMap.Clear();
        }

        public int GerChunkIndex(TKey key)
        {
            if (key == null) return -1;
            return _keyMap.TryGetValue(key, out var chunkIndex) ? chunkIndex : -1;
        }

        public IEnumerable<int> GetChunkIndexByKey(Func<TKey, bool> keyPredicate)
        {
            if (keyPredicate == null) yield break;
            foreach (var kvp in _keyMap)
            {
                if (!keyPredicate.Invoke(kvp.Key)) continue;
                yield return kvp.Value;
            }
        }

        public long GetChunkOffset(int chunkIndex)
        {
            return _offsetMap.TryGetValue(chunkIndex, out var offset) ? offset : -1;
        }
    }
}