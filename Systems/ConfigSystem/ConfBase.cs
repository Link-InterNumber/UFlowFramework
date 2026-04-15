using System;
using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    [Serializable]
    public class ConfBase
    {
        public static implicit operator bool(ConfBase conf)
        {
            return conf != null;
        }
    }

    public abstract class ConfBaseCollections
    {
        public AssetLoadStatus loadStatus => _loadStatus;
        protected AssetLoadStatus _loadStatus = AssetLoadStatus.Unload;
        protected string _assetPath;
        protected ConfAsyncLoadHandle _loadHandle;
        public string assetPath => _assetPath;
        protected int _refCount = 0;
        public abstract void LoadConfAsync<T>(T handle) where T : ConfAsyncLoadHandle;
        public abstract void Release();
        
    }

    public class ConfChunkIndexer<TKey>
    {
        private Dictionary<TKey, int> _keys;
        private Dictionary<int, ChunkInfo> _confBaseData;

        public ConfChunkIndexer(IEnumerable<ChunkInfo> chunks)
        {
            _keys = new Dictionary<TKey, int>();
            _confBaseData = new Dictionary<int, ChunkInfo>();
            if (chunks == null) return;
            foreach (var chunk in chunks)
            {
                var keyByte = chunk.keyData;
                var keys = SerializeUtils.DeserializeFromBinary<TKey[]>(keyByte);
                for (var i = 0; i < keys.Length; i++)
                {
                    _keys[keys[i]] = chunk.index;
                }
                _confBaseData[chunk.index] = chunk;
            }
        }

        public int GerChunkIndex(TKey key)
        {
            if (key == null) return -1;
            return _keys.GetValueOrDefault(key, -1);
        }

        public ChunkInfo GetChunkInfo(int chunkIndex)
        {
            return _confBaseData.GetValueOrDefault(chunkIndex);
        }
    }

    public class ConfRef
    {
        private HashSet<int> _loadedChunk;

        public ConfRef()
        {
            _loadedChunk = new HashSet<int>();
        }
        
        public bool IsChunkLoaded(int chunkIndex) => _loadedChunk.Contains(chunkIndex);
    }

    [Serializable]
    public abstract class ConfBaseData 
#if SCRIPTABLE_OBJECT_CONFIG
        : ScriptableObject
#endif
    {
    }
}