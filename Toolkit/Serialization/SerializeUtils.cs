using System;
using System.Buffers;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PowerCellStudio
{
    public static class SerializeUtils
    {
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
                LinkLog.LogError($"SerializeToJson failed: {e.Message}");
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
                LinkLog.LogError($"DeserializeFromJson failed: {e.Message}");
                return default;
            }
        }

        public static byte[] SerializeToBinary<T>(T data)
        {
            if (data == null)
            {
                return Array.Empty<byte>();
            }
            try
            {
                var bytes = AnySerializer.Serializer.Serialize(data);
                // 2. GZip 压缩
                using (var memoryStream = new MemoryStream())
                {
                    using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                    {
                        gzipStream.Write(bytes, 0, bytes.Length);
                    }
                    bytes = memoryStream.ToArray();
                }
                return bytes;
            }
            catch (Exception e)
            {
                LinkLog.LogError($"SerializeToBinary failed: {e.Message}");
                return Array.Empty<byte>();
            }
        }

        public static T DeserializeFromBinary<T>(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return default;
            }
            try
            {
                byte[] result = null;
                using (var compressedStream = new MemoryStream(bytes))
                using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                using (var resultStream = new MemoryStream())
                {
                    gzipStream.CopyTo(resultStream);
                    result = resultStream.ToArray();
                }

                var data = AnySerializer.Serializer.Deserialize<T>(result, AnySerializer.SerializerOptions.None);
                // var json = Encoding.UTF8.GetString(result);
                // T data = JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
                // {
                //     // TypeNameHandling = TypeNameHandling.Auto,
                //     PreserveReferencesHandling = PreserveReferencesHandling.Objects
                // });
                return data;
            }
            catch (Exception e)
            {
                LinkLog.LogError($"DeserializeFromBinary failed: {e.Message}");
                return default;
            }
        }
        
        public static async Task<byte[]> SerializeToBinaryAsync<T>(T data)
        {
            if (data == null)
            {
                return Array.Empty<byte>();
            }
            try
            {
                var bytes = AnySerializer.Serializer.Serialize(data);

                // 2. GZip 压缩
                using (var memoryStream = new MemoryStream())
                using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                {
                    await gzipStream.WriteAsync(bytes, 0, bytes.Length);
                    bytes = memoryStream.ToArray();
                }

                return bytes;
            }
            catch (Exception e)
            {
                LinkLog.LogError($"SerializeToBinary failed: {e.Message}");
                return Array.Empty<byte>();
            }
        }

        public static async Task<T> DeserializeFromBinaryAsync<T>(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return default;
            }
            try
            {
                byte[] result = null;
                using (var compressedStream = new MemoryStream(bytes))
                using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                using (var resultStream = new MemoryStream())
                {
                    await gzipStream.CopyToAsync(resultStream);
                    result = resultStream.ToArray();
                }
                var data = AnySerializer.Serializer.Deserialize<T>(result, AnySerializer.SerializerOptions.None);
                // var json = Encoding.UTF8.GetString(result);
                // var data = JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
                // {
                //     // TypeNameHandling = TypeNameHandling.Auto,
                //     PreserveReferencesHandling = PreserveReferencesHandling.Objects
                // });
                return data;
            }
            catch (Exception e)
            {
                LinkLog.LogError($"DeserializeFromBinary failed: {e.Message}");
                return default;
            }
        }
    }
}