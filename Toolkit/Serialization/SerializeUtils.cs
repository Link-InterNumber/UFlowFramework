using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PowerCellStudio
{
    public static class SerializeUtils
    {
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
        private static readonly JsonSerializerSettings SafeJsonSerializerSettings = new JsonSerializerSettings
        {
            // TypeNameHandling = TypeNameHandling.Auto,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects
        };

        public static string SerializeToJson<T>(T data, bool prettyPrint = false)
        {
            if (data == null)
            {
                return "{}";
            }
            try
            {
                return JsonConvert.SerializeObject(data,
                    prettyPrint ? Formatting.Indented : Newtonsoft.Json.Formatting.None);
            }
            catch (Exception e)
            {
                LinkLog.LogError($"SerializeToJson failed: {e.Message}\n {e.InnerException}");
                return "{}";
            }
        }

        public static T DeserializeFromJson<T>(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }
            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception e)
            {
                LinkLog.LogError($"DeserializeFromJson failed: {e.Message}\n {e.InnerException}");
                return default;
            }
        }

        /// <summary>
        /// 将对象序列化为二进制数据。默认使用快速模式，直接进行二进制序列化；如果选择安全模式，对于数据类型发生变化也能保证兼容性，但不能保证发生变化的数据结构能被正确序列化和反序列化。
        /// </summary>
        public static byte[] SerializeToBinary<T>(T data, BinaryObjectSerializationMode mode = BinaryObjectSerializationMode.Fast)
        {
            if (data == null)
            {
                return Array.Empty<byte>();
            }
            try
            {
                // 在安全模式下，使用 JSON 序列化并转换为 UTF-8 字节数组，以避免二进制序列化带来的安全风险。
                if (mode == BinaryObjectSerializationMode.Safe)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
                        using (var streamWriter = new StreamWriter(gzipStream, Utf8NoBom, 1024, true))
                        using (var jsonWriter = new JsonTextWriter(streamWriter))
                        {
                            var serializer = JsonSerializer.CreateDefault(SafeJsonSerializerSettings);
                            serializer.Serialize(jsonWriter, data);
                            jsonWriter.Flush();
                        }

                        return memoryStream.ToArray();
                    }
                }

                return BinarySerializer.Serialize<T>(data);
            }
            catch (Exception e)
            {
                LinkLog.LogError($"SerializeToBinary failed: {e.Message}\n {e.InnerException}");
                return Array.Empty<byte>();
            }
        }

        /// <summary>
        /// 将二进制数据反序列化为对象。如果序列化时使用了安全模式，则反序列化时也必须使用安全模式。
        /// </summary>
        public static T DeserializeFromBinary<T>(byte[] bytes, int offset = 0, int count = -1, BinaryObjectSerializationMode mode = BinaryObjectSerializationMode.Fast)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return default;
            }
            try
            {
                var actualCount = count == -1 ? bytes.Length - offset : count;
                if (offset < 0 || actualCount < 0 || offset + actualCount > bytes.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset), "Offset and count must specify a valid range in the byte array.");
                }

                // 在安全模式下，先进行 GZip 解压缩，然后将 UTF-8 字节数组转换回 JSON 字符串，最后反序列化为对象。
                if (mode == BinaryObjectSerializationMode.Safe)
                {
                    using (var compressedStream = new MemoryStream(bytes, offset, actualCount, false))
                    using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                    using (var streamReader = new StreamReader(gzipStream, Utf8NoBom, true))
                    using (var jsonReader = new JsonTextReader(streamReader))
                    {
                        var serializer = JsonSerializer.CreateDefault(SafeJsonSerializerSettings);
                        return serializer.Deserialize<T>(jsonReader);
                    }
                }

                return BinarySerializer.Deserialize<T>(bytes, offset, actualCount);
            }
            catch (Exception e)
            {
                LinkLog.LogError($"DeserializeFromBinary failed: {e.Message}\n {e.InnerException}");
                return default;
            }
        }
        
        /// <summary>
        /// 异步将对象序列化为二进制数据。默认使用快速模式，直接进行二进制序列化；如果选择安全模式，对于数据类型发生变化也能保证兼容性，但不能保证发生变化的数据结构能被正确序列化和反序列化。
        /// </summary>
        public static async Task<byte[]> SerializeToBinaryAsync<T>(T data, BinaryObjectSerializationMode mnode = BinaryObjectSerializationMode.Fast)
        {
            if (data == null)
            {
                return Array.Empty<byte>();
            }
            return await Task.Run(() => SerializeToBinary(data, mnode));
        }

        /// <summary>
        /// 异步将二进制数据反序列化为对象。如果序列化时使用了安全模式，则反序列化时也必须使用安全模式。
        /// </summary>
        public static async Task<T> DeserializeFromBinaryAsync<T>(byte[] bytes, int offset = 0, int count = -1, BinaryObjectSerializationMode objectSerializationMode = BinaryObjectSerializationMode.Fast)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return default;
            }
            return await Task.Run(() => DeserializeFromBinary<T>(bytes, offset, count, objectSerializationMode));
        }
    }
}