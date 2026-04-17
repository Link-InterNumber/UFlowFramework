using System;

namespace PowerCellStudio
{
    [Serializable]
    public class ChunkInfo
    {
        public int index;
        public long offset;
        /// <summary>
        /// 原始数据为TKey[]
        /// The original data is TKey[]
        /// </summary>
        public byte[] keyData;
    }
}