using System;
using System.Collections.Generic;

namespace PowerCellStudio
{
    public interface IChunkDataMap<TKey, TData>
    {
        public void Init(int chunkLength);

        public void AddChunk(int chunkIndex, IEnumerable<TData> dataSource, Func<TData, TKey> keySelector, Action<TData> onAdd);

        public void TryClearUnused(Action<IEnumerable<TData>> onRemove);

        public void ClearAll(Action<IEnumerable<TData>> onRemove);

        public bool IsChunkLoaded(int chunkIndex);

        public TData GetData(int chunkIndex, TKey key);

        public IEnumerable<TData> GetAllData(int chunkIndex);
    }
}