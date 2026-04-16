using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PowerCellStudio
{
    public static class ChunkMaker
    {
        /// <summary>
        /// Writes data to a file in chunked format: each record is stored as a 4-byte length prefix followed by payload bytes.
        /// 以分块格式将数据写入文件：每条记录由4字节长度前缀和数据字节组成。
        /// </summary>
        /// <param name="filePath">
        /// Destination file path. Existing file content will be overwritten because <see cref="FileMode.Create"/> is used.
        /// 目标文件路径。由于使用 <see cref="FileMode.Create"/>，若文件已存在将被覆盖。
        /// </param>
        /// <param name="data">
        /// Source sequence to serialize and write.
        /// 需要序列化并写入的数据序列。
        /// </param>
        /// <param name="deEncrypt">
        /// Whether serialized bytes should be processed by AES before writing.
        /// 是否在写入前对序列化字节执行 AES 处理。
        /// </param>
        /// <typeparam name="TData">
        /// Element type of the source sequence.
        /// 源数据序列元素类型。
        /// </typeparam>
        /// <returns>
        /// Total number of bytes written for all records excluding the final 0-length terminator.
        /// 所有有效记录写入的总字节数（不包含末尾0长度结束标记）。
        /// </returns>
        public static IEnumerable<ChunkInfo> WriteYieldInstruction<TData, TKey>(string filePath, IEnumerable<TData> data,
            Func<TData, TKey> keySelector, int chunkSize, bool deEncrypt = false)
        {
            using var dataFile = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            if (chunkSize <= 0)
            {
                chunkSize = 256;
            }
            var offset = 0L;
            var offsetCounter = 0L;
            var keys = new List<TKey>();
            var index = 0;
            foreach (var item in data)
            {
                if (item == null) continue;
                keys.Add(keySelector(item));
                var dataBytes = SerializeUtils.SerializeToBinary(item);
                if (deEncrypt)
                {
                    dataBytes = EncryptUtils.AESEncrypt(dataBytes, ConstSetting.FileEncryptionKey);
                }
                var lengthBytes = System.BitConverter.GetBytes(dataBytes.Length);
                dataFile.Write(lengthBytes, 0, lengthBytes.Length);
                dataFile.Write(dataBytes, 0, dataBytes.Length);
                offsetCounter += lengthBytes.Length + dataBytes.Length;
                if (keys.Count == chunkSize)
                {
                    // write a 0 to indicate the end of the chunk
                    // 写入0表示数据块末尾
                    var dataLengthBytes = System.BitConverter.GetBytes(0);
                    dataFile.Write(dataLengthBytes, 0 , dataLengthBytes.Length);
                    offsetCounter += dataLengthBytes.Length;
                    var chunkInfo = new ChunkInfo
                    {
                        index = index,
                        offset = offset,
                        keyData = SerializeUtils.SerializeToBinary(keys.ToArray())
                    };
                    keys.Clear();
                    offset = offsetCounter;
                    index++;
                    yield return chunkInfo;
                }
            }
            if (keys.Count > 0)
            {
                // write a 0 to indicate the end of the chunk
                // 写入0表示数据块末尾
                var dataLengthBytes = System.BitConverter.GetBytes(0);
                dataFile.Write(dataLengthBytes, 0 , dataLengthBytes.Length);
                var chunkInfo = new ChunkInfo
                {
                    index = index,
                    offset = offset,
                    keyData = SerializeUtils.SerializeToBinary(keys.ToArray())
                };
                keys.Clear();
                yield return chunkInfo;
            }
        }
                
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