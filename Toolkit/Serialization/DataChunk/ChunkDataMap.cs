using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace PowerCellStudio
{
    public class ChunkDataMap<TKey, TData>
    {
        /// <summary>
        /// 根据chunk的index放置引用计数
        /// </summary>
        private int[] _loadedChunkRefCount;
        
        /// <summary>
        /// 根据chunk的index放置的配置表数据
        /// </summary>
        private Dictionary<TKey, TData>[] _chunkDataMap;
        
        private bool _isInited => _loadedChunkRefCount != null && _chunkDataMap != null;

        public void Init(int chunkLength)
        {
            _loadedChunkRefCount = new int[chunkLength];
            _chunkDataMap = new Dictionary<TKey, TData>[chunkLength];
            for (var i = 0; i < chunkLength; i++)
            {
                _chunkDataMap[i] = new Dictionary<TKey, TData>();
            }
        }
        
        private void AddRef(int chunkIndex)
        {
            if (chunkIndex < 0 || chunkIndex >= _loadedChunkRefCount.Length) return;
            var currentRefCount = _loadedChunkRefCount[chunkIndex];
            if (currentRefCount <= 0)
            {
                _loadedChunkRefCount[chunkIndex] = 1;
            }
            _loadedChunkRefCount[chunkIndex] = currentRefCount + 1;
        }
        
        public void AddChunk(int chunkIndex, IEnumerable<TData> dataSource, Func<TData, TKey> keySelector, Action<TData> onAdd)
        {
            if (!_isInited) return;
            if (chunkIndex < 0 || chunkIndex >= _chunkDataMap.Length) return;
            var dataMap = _chunkDataMap[chunkIndex];
            foreach (var data in dataSource)
            {
                var key = keySelector(data);
                dataMap[key] = data;
                onAdd?.Invoke(data);
            }
            AddRef(chunkIndex);
        }

        public void TryClearUnused()
        {
            if (!_isInited) return;
            List<(int chunkIndex, int refCount)> toClearList = ListPool<(int chunkIndex, int refCount)>.Get();
            for (var i = 0; i < _loadedChunkRefCount.Length; i++)
            {
                var refCount = _loadedChunkRefCount[i];
                if (refCount <= 0) continue;
                toClearList.Add((i, refCount));
            }
            var keep = Mathf.Max(1, toClearList.Count / 3);
            if (keep == toClearList.Count) return;
            toClearList.Sort((a, b) => b.refCount.CompareTo(a.refCount));
            for (var i = 0; i < toClearList.Count; i++)
            {
                var chunkIndex = toClearList[i].chunkIndex;
                if (i < keep)
                {
                    _loadedChunkRefCount[chunkIndex] = 1;
                    continue;
                }
                ReleaseChunk(chunkIndex);
            }
            ListPool<(int chunkIndex, int refCount)>.Release(toClearList);
        }
        
        public void ClearAll()
        {
            if (!_isInited) return;
            for (var i = 0; i < _loadedChunkRefCount.Length; i++)
            {
                ReleaseChunk(i);
            }
        }
            

        private void ReleaseChunk(int chunkIndex)
        {
            if (chunkIndex < 0 || chunkIndex >= _loadedChunkRefCount.Length) return;
            _chunkDataMap[chunkIndex].Clear();
            _loadedChunkRefCount[chunkIndex] = 0;
        }

        public bool IsChunkLoaded(int chunkIndex)
        {
            if (!_isInited) return false;
            if (chunkIndex < 0 || chunkIndex >= _loadedChunkRefCount.Length) return false;
            return _loadedChunkRefCount[chunkIndex] > 0;
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
            foreach (var conf in _chunkDataMap[chunkIndex].Values)
            {
                yield return conf;
            }
        }
    }
}