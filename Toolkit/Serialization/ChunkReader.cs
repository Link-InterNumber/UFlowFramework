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
        /// Reads chunked binary records from a file and yields deserialized objects one by one.
        /// 从文件中按分块格式读取二进制记录，并逐条反序列化后返回。
        /// </summary>
        /// <param name="filePath">
        /// The target file path to read from.
        /// 要读取的目标文件路径。
        /// </param>
        /// <param name="offset">
        /// Byte offset where reading starts. If greater than 0, the stream seeks to this position first.
        /// 读取起始字节偏移量；当大于0时，会先将流定位到该位置再开始读取。
        /// </param>
        /// <param name="deEncrypt">
        /// Whether payload bytes should be processed by AES before deserialization.
        /// 是否在反序列化前对数据字节执行 AES 处理。
        /// </param>
        /// <typeparam name="TData">
        /// The target object type to deserialize.
        /// 反序列化目标对象类型。
        /// </typeparam>
        /// <returns>
        /// An enumerable sequence of deserialized objects; stops when the file ends, data is incomplete, or terminal zero-length chunk is encountered.
        /// 反序列化后的对象序列；当文件结束、数据不完整或遇到0长度结束块时停止。
        /// </returns>
        public static IEnumerable<TData> ReadYieldInstruction<TData>(string filePath, long offset, bool deEncrypt = false)
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
                    dataBytes = EncryptUtils.AESEncrypt(dataBytes, ConstSetting.FileEncryptionKey);
                }
                var data = SerializeUtils.DeserializeFromBinary<TData>(dataBytes);
                yield return data;
            }
        }
    }
}