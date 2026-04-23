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
                using (var memoryStream = new MemoryStream())
                {
                    using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
                    using (var streamWriter = new StreamWriter(gzipStream, new UTF8Encoding(false)))
                    using (var jsonWriter = new JsonTextWriter(streamWriter))
                    {
                        var serializer = JsonSerializer.CreateDefault(new JsonSerializerSettings
                        {
                            // TypeNameHandling = TypeNameHandling.Auto,
                            PreserveReferencesHandling = PreserveReferencesHandling.Objects
                        });
                        serializer.Serialize(jsonWriter, data);
                    }
                    return memoryStream.ToArray();
                }
                // return BinarySerializer.Serialize<T>(data);
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
                using (var compressedStream = new MemoryStream(bytes))
                using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                using (var streamReader = new StreamReader(gzipStream, new UTF8Encoding(false)))
                using (var jsonReader = new JsonTextReader(streamReader))
                {
                    var serializer = JsonSerializer.CreateDefault(new JsonSerializerSettings
                    {
                        // TypeNameHandling = TypeNameHandling.Auto,
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects
                    });
                    return serializer.Deserialize<T>(jsonReader);
                }
                // return BinarySerializer.Deserialize<T>(bytes);
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
            return await Task.Run(() => SerializeToBinary(data));
        }

        public static async Task<T> DeserializeFromBinaryAsync<T>(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return default;
            }
            return await Task.Run(() => DeserializeFromBinary<T>(bytes));
        }
    }
}