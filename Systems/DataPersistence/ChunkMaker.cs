using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerCellStudio
{
    public static class ChunkMaker
    {
        public static List<ChunkData<TData>> Slice<TData, TKey>(IEnumerable<TData> dataSource, int chunkSize, Func<TData, TKey> keySelector)
        {
            if (dataSource == null)
                return new List<ChunkData<TData>>();
            var count = 0;
            var chunkDataList = new List<ChunkData<TData>>();

            var tempList = new List<TData>(chunkSize);
            var offset = 0L;
            foreach (var data in dataSource)
            {
                tempList.Add(data);
                count++;
                if (count % chunkSize != 0) continue;
                var chunkData = GetChunkData(tempList, chunkDataList.Count, offset, keySelector);
                chunkDataList.Add(chunkData);
                offset += chunkData.binaryData.Length;
                tempList = new List<TData>(chunkSize);
            }

            if (tempList.Count > 0)
            {
                var chunkData = GetChunkData(tempList, chunkDataList.Count, offset, keySelector);
                chunkDataList.Add(chunkData);
            }

            return chunkDataList;
        }

        private static ChunkData<TData> GetChunkData<TData, TKey>(List<TData> dataList, int index, long offset,  Func<TData, TKey> keySelector)
        {
            var binaryData = SerializeUtils.SerializeToBinary(dataList);
            var keyData = SerializeUtils.SerializeToBinary(dataList.Select(keySelector).ToArray());
            var chunkData = new ChunkData<TData>
            {
                data = new List<TData>(dataList),
                Info = new ChunkInfo
                {
                    index = index,
                    offset = offset,
                    length = binaryData.Length
                },
                binaryData = binaryData,
                keyData = keyData
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
    }

    public class ChunkData<T>
    {
        public ChunkInfo Info;
        public List<T> data;
        public byte[] binaryData;
        public byte[] keyData;
    }
}