using System;
using System.Buffers;
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
        /// <param name="options">Chunk serializer/encryptor options. Leave null to use the default binary serializer and AES encryption. 分块序列化/加密配置；为null时使用默认二进制序列化和AES加密。</param>
        /// <typeparam name="TData">Target type to deserialize. 反序列化的目标类型。</typeparam>
        /// <returns>Sequence of deserialized records. 反序列化后的记录序列。</returns>
        public static IEnumerable<TData> ReadChunkData<TData>(string filePath, long offset, ChunkDataOptions options = null)
        {
            if (!File.Exists(filePath)) yield break;
            ChunkDataOptions resolvedOptions = ChunkDataOptions.Resolve(options);
            using var dataFile = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (offset > 0)
            {
                dataFile.Seek(offset, SeekOrigin.Begin);
            }

            var lengthBuffer = ArrayPool<byte>.Shared.Rent(4);
            while (dataFile.CanRead)
            {
                // 4 bytes for the length of the data
                // 前4字节为数据的长度
                var headCount = dataFile.Read(lengthBuffer, 0, 4);
                if (headCount != 4) yield break;
                var dataLength = System.BitConverter.ToInt32(lengthBuffer);
                // if dataLength is 0, it means the end of the chunk
                // 如果dataLength为0，表示数据块末尾
                if (dataLength == 0) yield break;
                
                // dataLength bytes for the data
                // dataLength字节为数据
                byte[] dataBytes = ArrayPool<byte>.Shared.Rent(dataLength);
                var readDataLength = dataFile.Read(dataBytes, 0, dataLength);
                if (readDataLength != dataLength) yield break;
                // deserialize the data
                // 反序列化数据
                if (resolvedOptions.ResolvedEncryptor != null)
                {
                    var decryptedBytes = resolvedOptions.ResolvedEncryptor.Decrypt(dataBytes, 0, dataLength);
                    ArrayPool<byte>.Shared.Return(dataBytes);
                    dataBytes = decryptedBytes;
                    var data = resolvedOptions.ResolvedSerializer.Read<TData>(dataBytes, 0, dataBytes.Length);
                    yield return data;
                }
                else
                {
                    var data = resolvedOptions.ResolvedSerializer.Read<TData>(dataBytes, 0, dataLength);
                    ArrayPool<byte>.Shared.Return(dataBytes);
                    yield return data;
                }
            }
            ArrayPool<byte>.Shared.Return(lengthBuffer);
        }

        /// <summary>
        /// Reads chunk index info and associated keys from the index file.
        /// 从索引文件中读取分块索引信息及关联键数组。
        /// </summary>
        /// <param name="filePath">Index file path to read. 要读取的索引文件路径。</param>
        /// <param name="options">Chunk serializer/encryptor options. Leave null to use the default binary serializer and AES encryption. 分块序列化/加密配置；为null时使用默认二进制序列化和AES加密。</param>
        /// <typeparam name="TKey">Chunk key type. 分块的键类型。</typeparam>
        /// <returns>Sequence of chunk index, file offset, and keys. 包含分块索引、偏移量和键数组的元组序列。</returns>
        public static IEnumerable<(int index, long offset, TKey[] keys)> ReadIndexFile<TKey>(string filePath, ChunkDataOptions options = null)
        {
            if (!File.Exists(filePath)) yield break;
            ChunkDataOptions resolvedOptions = ChunkDataOptions.Resolve(options);
            using var idxFile = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var lengthBuffer = ArrayPool<byte>.Shared.Rent(4);
            while (idxFile.CanRead)
            {
                var headCount = idxFile.Read(lengthBuffer, 0, 4);
                if (headCount != 4) yield break;
                
                var dataLength = System.BitConverter.ToInt32(lengthBuffer, 0);
                if (dataLength == 0) yield break;
                
                var dataBytes = ArrayPool<byte>.Shared.Rent(dataLength);
                var readDataLength = idxFile.Read(dataBytes, 0, dataLength);
                if (readDataLength != dataLength) yield break;
                
                if (resolvedOptions.ResolvedEncryptor != null)
                {
                    var decryptedBytes = resolvedOptions.ResolvedEncryptor.Decrypt(dataBytes, 0, dataLength);
                    ArrayPool<byte>.Shared.Return(dataBytes);
                    dataBytes = decryptedBytes;
                    var chunkInfo = resolvedOptions.ResolvedSerializer.Read<ChunkInfo>(dataBytes, 0, dataBytes.Length);
                    var keys = (chunkInfo.keyData != null && chunkInfo.keyData.Length > 0) 
                            ? resolvedOptions.ResolvedSerializer.Read<TKey[]>(chunkInfo.keyData, 0, chunkInfo.keyData.Length)
                            : Array.Empty<TKey>();
                    yield return (chunkInfo.index, chunkInfo.offset, keys);
                }
                else
                {
                    var chunkInfo = resolvedOptions.ResolvedSerializer.Read<ChunkInfo>(dataBytes, 0, dataLength);
                    var keys = (chunkInfo.keyData != null && chunkInfo.keyData.Length > 0) 
                            ? resolvedOptions.ResolvedSerializer.Read<TKey[]>(chunkInfo.keyData, 0, chunkInfo.keyData.Length)
                            : Array.Empty<TKey>();
                    ArrayPool<byte>.Shared.Return(dataBytes);
                    yield return (chunkInfo.index, chunkInfo.offset, keys);
                }
            }
            ArrayPool<byte>.Shared.Return(lengthBuffer);
        }

        public static IEnumerable<(int index, long offset, TKey[] keys)> ReadIndexFileWithoutKeyBytes<TKey>(string filePath, ChunkDataOptions options = null)
        {
            if (!File.Exists(filePath)) yield break;
            ChunkDataOptions resolvedOptions = ChunkDataOptions.Resolve(options);
            using var idxFile = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var lengthBuffer = ArrayPool<byte>.Shared.Rent(4);
            while (idxFile.CanRead)
            {
                var headCount = idxFile.Read(lengthBuffer, 0, 4);
                if (headCount != 4) yield break;
                
                var dataLength = System.BitConverter.ToInt32(lengthBuffer, 0);
                if (dataLength == 0) yield break;
                
                var dataBytes = ArrayPool<byte>.Shared.Rent(dataLength);
                var readDataLength = idxFile.Read(dataBytes, 0, dataLength);
                if (readDataLength != dataLength) yield break;
                
                if (resolvedOptions.ResolvedEncryptor != null)
                {
                    var decryptedBytes = resolvedOptions.ResolvedEncryptor.Decrypt(dataBytes, 0, dataLength);
                    ArrayPool<byte>.Shared.Return(dataBytes);
                    dataBytes = decryptedBytes;
                    var chunkInfo = resolvedOptions.ResolvedSerializer.Read<ChunkInfo>(dataBytes, 0, dataBytes.Length);
                    var keys = Array.Empty<TKey>();
                    yield return (chunkInfo.index, chunkInfo.offset, keys);
                }
                else
                {
                    var chunkInfo = resolvedOptions.ResolvedSerializer.Read<ChunkInfo>(dataBytes, 0, dataLength);
                    var keys = Array.Empty<TKey>();
                    ArrayPool<byte>.Shared.Return(dataBytes);
                    yield return (chunkInfo.index, chunkInfo.offset, keys);
                }
            }
            ArrayPool<byte>.Shared.Return(lengthBuffer);
        }
    }
}