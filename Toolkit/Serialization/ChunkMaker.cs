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
        /// <param name="fileDirectory">
        /// Target directory to save the file.
        /// 要保存文件的目标目录。
        /// </param>
        /// <param name="fileName">
        /// Name of the file to write. 
        /// 要写入的文件名
        /// </param>
        /// <param name="data">
        /// Source sequence to serialize and write.
        /// 需要序列化并写入的数据序列。
        /// </param>
        /// <param name="keySelector">
        /// A function to extract the key used for chunking from each data item.
        /// 从每个数据项中提取分块使用的键的函数。
        /// </param>
        /// <param name="chunkSize">
        /// Number of records to include in each chunk. If the total number of records is not divisible by this chunk size, the last chunk will contain the remaining records.
        /// 每个数据块包含的记录数。如果记录总数不能被该块大小整除，则最后一个块将包含剩余的记录。
        /// </param>
        /// <param name="deEncrypt">
        /// Whether serialized bytes should be encrypted before writing.
        /// 是否在写入前对序列化字节执行加密处理。
        /// </param>
        /// <typeparam name="TData">
        /// Element type of the source sequence.
        /// 源数据序列元素类型。
        /// </typeparam>
        /// <typeparam name="TKey">
        /// Type of the key used for chunking.
        /// 分块使用的键类型。
        /// </typeparam>
        /// <returns>
        /// An enumerable of <see cref="ChunkInfo"/> objects, each representing a chunk of data written to the file. The <see cref="ChunkInfo.keyData"/> field contains the serialized keys for the corresponding chunk.
        /// 一个 <see cref="ChunkInfo"/> 对象的枚举，每个对象表示写入文件的一块数据。<see cref="ChunkInfo.keyData"/> 字段包含对应数据块的序列化键。
        /// </returns>
        public static IEnumerable<ChunkInfo> WriteYieldInstruction<TData, TKey>(string fileDirectory, string fileName, IEnumerable<TData> data,
            Func<TData, TKey> keySelector, int chunkSize, bool deEncrypt = false)
        {
            var dataFilePath = Path.Combine(fileDirectory, $"{fileName}Data.bytes");
            var indexFilePath = Path.Combine(fileDirectory, $"{fileName}Index.bytes");
            using var dataFile = new FileStream(dataFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
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
                if (keySelector != null) keys.Add(keySelector(item));
                var dataBytes = SerializeUtils.SerializeToBinary(item);
                if (deEncrypt)
                {
                    dataBytes = EncryptUtils.AESEncrypt(dataBytes, ConstSetting.FileEncryptionKey);
                }
                // 写入数据长度
                // Write the length of the data
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
                    yield return chunkInfo;
                    offset = offsetCounter;
                    index++;
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
}