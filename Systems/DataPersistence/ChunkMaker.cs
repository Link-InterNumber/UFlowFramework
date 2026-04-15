using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerCellStudio
{
    public static class ChunkMaker
    {
        public static IEnumerable<ChunkData<TData>> Slice<TData, TKey>(IEnumerable<TData> dataSource, int chunkSize, Func<TData, TKey> keySelector)
        {
            if (dataSource == null)
                yield break;
            var count = 0;
            var index = 0;
            var chunkDataList = new List<ChunkData<TData>>();

            var tempList = new List<TData>(chunkSize);
            var offset = 0L;
            foreach (var data in dataSource)
            {
                tempList.Add(data);
                count++;
                if (count % chunkSize != 0) continue;
                var chunkData = GetChunkData(tempList, index, offset, keySelector);
                yield return chunkData;
                index++;
                offset += chunkData.binaryData.Length;
                tempList.Clear();
            }

            if (tempList.Count > 0)
            {
                var chunkData = GetChunkData(tempList, chunkDataList.Count, offset, keySelector);
                yield return chunkData;
            }
        }

        private static ChunkData<TData> GetChunkData<TData, TKey>(List<TData> dataList, int index, long offset,  Func<TData, TKey> keySelector)
        {
            var sourceData = dataList.ToArray();
            var binaryData = SerializeUtils.SerializeToBinary(sourceData);
            var keyData = SerializeUtils.SerializeToBinary(dataList.Select(keySelector).ToArray());
            var chunkData = new ChunkData<TData>
            {
                Info = new ChunkInfo
                {
                    index = index,
                    offset = offset,
                    length = binaryData.Length,
                    keyData = keyData
                },
                binaryData = binaryData,
            };
            return chunkData;
        }
    }
    
    [Serializable]
    public class ChunkInfo
    {
        public int index;
        public long offset;
        public int length;
        /// <summary>
        /// 原始数据为TKey[]
        /// The original data is TKey[]
        /// </summary>
        public byte[] keyData;
    }

    [Serializable]
    public class ChunkData<T>
    {
        public ChunkInfo Info;
        /// <summary>
        /// 原始数据为TData[]
        /// The original data is TData[]
        /// </summary>
        public byte[] binaryData;

    }
}