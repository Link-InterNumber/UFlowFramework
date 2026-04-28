using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.Pool;

namespace PowerCellStudio
{
    public static class ChunkMaker
    {
        /// <summary>
        /// Writes records as [4-byte length + payload] chunks, ending each chunk with 0.
        /// 以[4字节长度+数据]分块写入记录，并以0结束每个块。
        /// </summary>
        /// <param name="filePath">Output file path. 输出文件路径。</param>
        /// <param name="data">Source records to serialize and write. 待序列化并写入的源记录。</param>
        /// <param name="keySelector">Extracts per-record chunk key. 提取每条记录的分块键。</param>
        /// <param name="chunkSize">Maximum records per chunk. 每个分块的最大记录数。</param>
        /// <param name="deEncrypt">Encrypts bytes before writing when true. 为true时写入前加密字节。</param>
        /// <typeparam name="TData">Record type. 记录类型。</typeparam>
        /// <typeparam name="TKey">Chunk key type. 分块键类型。</typeparam>
        /// <returns>Chunk metadata sequence; <see cref="ChunkInfo.keyData"/> stores serialized <c>TKey[]</c>. 分块元数据序列；<see cref="ChunkInfo.keyData"/>保存序列化<c>TKey[]</c>。</returns>
        public static void StreamWriteSync<TKey, TData>(string fileDirectory, string fileName, IEnumerable<TData> data,
            Func<TData, TKey> keySelector, int chunkSize, bool deEncrypt = true)
        {
            if (!Directory.Exists(fileDirectory)) Directory.CreateDirectory(fileDirectory);
            var dataFilePath = Path.Combine(fileDirectory, $"{fileName}Data.bytes");
            var chunkInfos = WriteYieldInstruction(dataFilePath, data, keySelector, chunkSize, deEncrypt);
            var indexFilePath = Path.Combine(fileDirectory, $"{fileName}Index.bytes");
            StreamWriteChunkInfo(indexFilePath, chunkInfos, deEncrypt);
        }
        
        private static IEnumerable<ChunkInfo> WriteYieldInstruction<TData, TKey>(string filePath, IEnumerable<TData> data,
            Func<TData, TKey> keySelector, int chunkSize, bool deEncrypt = false)
        {
            using var dataFile = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            if (chunkSize <= 0)
            {
                chunkSize = 256;
            }
            var offset = 0L;
            var offsetCounter = 0L;
            var keys = ListPool<TKey>.Get();
            var index = 0;
            foreach (var item in data)
            {
                if (item == null) continue;
                if (keySelector != null) keys.Add(keySelector(item));
                var dataBytes = SerializeUtils.SerializeToBinary(item);
                if (dataBytes == null || dataBytes.Length == 0)
                {
                    UnityEngine.Debug.LogError($"[ChunkMaker] 序列化失败或数据为空，跳过当前数据。类型: {item?.GetType()}");
                    continue; // 发生序列化错误时跳过，避免写入 byte[0]
                }
                
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
                        keyData = keys.Count > 0 ? SerializeUtils.SerializeToBinary(keys.ToArray()) : Array.Empty<byte>(),
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
            ListPool<TKey>.Release(keys);
            dataFile.Flush();
        }
        
        private static void StreamWriteChunkInfo(string filePath, IEnumerable<ChunkInfo> chunkInfos, bool deEncrypt)
        {
            using var idxFile = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            foreach (var chunkInfo in chunkInfos)
            {
                var dataBytes = SerializeUtils.SerializeToBinary(chunkInfo);
                if (deEncrypt)
                {
                    dataBytes = EncryptUtils.AESEncrypt(dataBytes, ConstSetting.FileEncryptionKey);
                }
                var lengthBytes = System.BitConverter.GetBytes(dataBytes.Length);
                idxFile.Write(lengthBytes, 0, lengthBytes.Length);
                idxFile.Write(dataBytes, 0, dataBytes.Length);
            }
            idxFile.Flush();
        }
    }
}