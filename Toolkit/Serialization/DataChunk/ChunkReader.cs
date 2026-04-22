using System;
using System.Collections.Generic;
using System.IO;

namespace PowerCellStudio
{
    /// <summary>
    /// Provides helper methods for writing and reading length-prefixed binary chunks.
    /// 提供基于“长度前缀 + 数据体”格式的二进制读取辅助方法。
    /// </summary>
    /// <remarks>
    /// File format:
    /// 1) 4-byte Int32 length prefix
    /// 2) payload bytes
    /// 3) a terminal 4-byte zero length (0) to mark end of stream
    ///
    /// 文件格式：
    /// 1）4字节 Int32 长度前缀
    /// 2）对应长度的数据字节
    /// 3）末尾写入一个4字节的0长度（0）作为结束标记
    /// </remarks>
    public static class ChunkReader
    {
        /// <summary>
        /// Reads chunked records from a file and yields deserialized objects sequentially.
        /// 从文件中按分块格式顺序读取并以迭代形式返回反序列化后的对象。
        /// </summary>
        /// <param name="filePath">Target file path to read. 要读取的目标文件路径。</param>
        /// <param name="offset">Starting byte offset for reading. 读取的起始字节偏移量。</param>
        /// <param name="deEncrypt">Decrypt before deserialization if true. 为true时在反序列化前解密。</param>
        /// <typeparam name="TData">Target type to deserialize. 反序列化的目标类型。</typeparam>
        /// <returns>Sequence of deserialized records. 反序列化后的记录序列。</returns>
        public static IEnumerable<TData> ReadChunkData<TData>(string filePath, long offset, bool deEncrypt = true)
        {
            if (!File.Exists(filePath)) yield break;
            using var dataFile = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (offset > 0)
            {
                dataFile.Seek(offset, SeekOrigin.Begin);
            }
            var lengthBuffer = new byte[4];
            while (dataFile.CanRead)
            {
                // 4 bytes for the length of the data
                // 前4字节为数据的长度
                var headCount = dataFile.Read(lengthBuffer, 0, 4);
                if (headCount != 4) yield break;
                var dataLength = System.BitConverter.ToInt32(lengthBuffer, 0);
                // if dataLength is 0, it means the end of the chunk
                // 如果dataLength为0，表示数据块末尾
                if (dataLength == 0) yield break;
                
                // dataLength bytes for the data
                // dataLength字节为数据
                var dataBytes = new byte[dataLength];
                var readDataLength = dataFile.Read(dataBytes, 0, dataLength);
                if (readDataLength != dataLength) yield break;
                // deserialize the data
                // 反序列化数据
                if (deEncrypt)
                {
                    dataBytes = EncryptUtils.AESDecrypt(dataBytes, ConstSetting.FileEncryptionKey);
                }
                var data = SerializeUtils.DeserializeFromBinary<TData>(dataBytes);
                yield return data;
            }
        }

        /// <summary>
        /// Reads chunk index info and associated keys from the index file.
        /// 从索引文件中读取分块索引信息及关联键数组。
        /// </summary>
        /// <param name="filePath">Index file path to read. 要读取的索引文件路径。</param>
        /// <param name="deEncrypt">Decrypt before deserialization if true. 为true时在反序列化前解密。</param>
        /// <typeparam name="TKey">Chunk key type. 分块的键类型。</typeparam>
        /// <returns>Sequence of chunk index, file offset, and keys. 包含分块索引、偏移量和键数组的元组序列。</returns>
        public static IEnumerable<(int index, long offset, TKey[] keys)> ReadIndexFile<TKey>(string filePath, bool deEncrypt = true)
        {
            if (!File.Exists(filePath)) yield break;
            using var idxFile = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var lengthBuffer = new byte[4];
            while (idxFile.CanRead)
            {
                var headCount = idxFile.Read(lengthBuffer, 0, 4);
                if (headCount != 4) yield break;
                
                var dataLength = System.BitConverter.ToInt32(lengthBuffer, 0);
                if (dataLength == 0) yield break;
                
                var dataBytes = new byte[dataLength];
                var readDataLength = idxFile.Read(dataBytes, 0, dataLength);
                if (readDataLength != dataLength) yield break;
                
                if (deEncrypt)
                {
                    dataBytes = EncryptUtils.AESDecrypt(dataBytes, ConstSetting.FileEncryptionKey);
                }
                
                var chunkInfo = SerializeUtils.DeserializeFromBinary<ChunkInfo>(dataBytes);
                var keys = (chunkInfo.keyData != null && chunkInfo.keyData.Length > 0) 
                    ? SerializeUtils.DeserializeFromBinary<TKey[]>(chunkInfo.keyData) 
                    : Array.Empty<TKey>();
                    
                yield return (chunkInfo.index, chunkInfo.offset, keys);
            }
        }
    }
}