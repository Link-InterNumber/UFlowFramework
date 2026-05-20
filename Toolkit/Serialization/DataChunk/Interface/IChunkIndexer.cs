using System;
using System.Collections.Generic;

namespace PowerCellStudio
{
    public interface IChunkIndexer<TKey>
    {
        public int chunkCount { get; }
        
        public void Clear();

        public int GerChunkIndex(TKey key);

        public IEnumerable<int> GetChunkIndexByKey(Func<TKey, bool> keyPredicate);

        public long GetChunkOffset(int chunkIndex);
    }
}