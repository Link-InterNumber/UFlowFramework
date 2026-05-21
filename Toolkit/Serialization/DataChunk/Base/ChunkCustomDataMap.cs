using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace PowerCellStudio
{
    public class ChunkCustomDataMap<TKey, TData> : IChunkDataMap<TKey, TData>
    {
        /// <summary>
        /// 根据chunk的index放置引用计数
        /// </summary>
        private Dictionary<int, int> _loadedChunkRefCount;
        
        /// <summary>
        /// 根据chunk的index放置的配置表数据
        /// </summary>
        private Dictionary<int, Dictionary<TKey, TData>> _chunkDataMap;
        
        private bool _isInited => _loadedChunkRefCount != null && _chunkDataMap != null;

        public void Init(int chunkLength)
        {
            _loadedChunkRefCount = new Dictionary<int, int>(chunkLength);
            _chunkDataMap = new Dictionary<int, Dictionary<TKey, TData>>(chunkLength);
        }
        
        private void AddRef(int chunkIndex)
        {
            if (!_isInited || chunkIndex < 0) return;
            var currentRefCount = _loadedChunkRefCount.GetValueOrDefault(chunkIndex);
            if (currentRefCount <= 0)
            {
                _loadedChunkRefCount[chunkIndex] = 1;
                return;
            }
            _loadedChunkRefCount[chunkIndex] = currentRefCount + 1;
        }
        
        public void AddChunk(int chunkIndex, IEnumerable<TData> dataSource, Func<TData, TKey> keySelector, Action<TData> onAdd)
        {
            if (!_isInited) return;
            if (chunkIndex < 0) return;
            if (!_chunkDataMap.TryGetValue(chunkIndex, out var dataMap))
            {
                dataMap = new Dictionary<TKey, TData>();
                _chunkDataMap[chunkIndex] = dataMap;
            }
            foreach (var data in dataSource)
            {
                var key = keySelector(data);
                dataMap[key] = data;
                onAdd?.Invoke(data);
            }
            AddRef(chunkIndex);
        }

        public void TryClearUnused(Action<IEnumerable<TData>> onRemove)
        {
            if (!_isInited) return;
            List<(int chunkIndex, int refCount)> toClearList = ListPool<(int chunkIndex, int refCount)>.Get();
            foreach (var kvp in _loadedChunkRefCount)
            {
                var refCount = kvp.Value;
                if (refCount <= 0) continue;
                toClearList.Add((kvp.Key, refCount));
            }
            var keep = Mathf.Max(1, toClearList.Count / 3);
            if (keep == toClearList.Count)
            {
                ListPool<(int chunkIndex, int refCount)>.Release(toClearList);
                return;
            }
            toClearList.Sort((a, b) => b.refCount.CompareTo(a.refCount));
            for (var i = 0; i < toClearList.Count; i++)
            {
                var chunkIndex = toClearList[i].chunkIndex;
                if (i < keep)
                {
                    _loadedChunkRefCount[chunkIndex] = 1;
                    continue;
                }
                ReleaseChunk(chunkIndex, onRemove);
            }
            ListPool<(int chunkIndex, int refCount)>.Release(toClearList);
        }
        
        public void ClearAll(Action<IEnumerable<TData>> onRemove)
        {
            if (!_isInited) return;
            List<int> chunkIndexes = ListPool<int>.Get();
            foreach (var chunkIndex in _chunkDataMap.Keys)
            {
                chunkIndexes.Add(chunkIndex);
            }
            for (var i = 0; i < chunkIndexes.Count; i++)
            {
                ReleaseChunk(chunkIndexes[i], onRemove);
            }
            ListPool<int>.Release(chunkIndexes);
        }
            

        private void ReleaseChunk(int chunkIndex, Action<IEnumerable<TData>> onRemove)
        {
            if (!_chunkDataMap.TryGetValue(chunkIndex, out var dataMap)) return;
            onRemove?.Invoke(dataMap.Values);
            dataMap.Clear();
            _loadedChunkRefCount[chunkIndex] = 0;
        }

        public bool IsChunkLoaded(int chunkIndex)
        {
            if (!_isInited) return false;
            if (chunkIndex < 0) return false;
            return _loadedChunkRefCount.TryGetValue(chunkIndex, out var refCount) && refCount > 0;
        }

        public TData GetData(int chunkIndex, TKey key)
        {
            if (!IsChunkLoaded(chunkIndex)) return default;
            AddRef(chunkIndex);
            return _chunkDataMap[chunkIndex].GetValueOrDefault(key);
        }

        public IEnumerable<TData> GetAllData(int chunkIndex)
        {
            if (!IsChunkLoaded(chunkIndex)) yield break;
            AddRef(chunkIndex);
            if (!_chunkDataMap.TryGetValue(chunkIndex, out var dataMap)) yield break;
            foreach (var conf in dataMap.Values)
            {
                yield return conf;
            }
        }
    }
}