using System;
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

        public static byte[] SerializeToBinary<T>(T data)
        {
            if (data == null)
            {
                return Array.Empty<byte>();
            }
            try
            {
                return BinarySerializer.Serialize<T>(data);
            }
            catch (Exception e)
            {
                LinkLog.LogError($"SerializeToBinary failed: {e.Message}\n {e.InnerException}");
                return Array.Empty<byte>();
            }
        }

        public static T DeserializeFromBinary<T>(byte[] bytes, int offset = 0, int count = -1)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return default;
            }
            try
            {
                return BinarySerializer.Deserialize<T>(bytes, offset, count);
            }
            catch (Exception e)
            {
                LinkLog.LogError($"DeserializeFromBinary failed: {e.Message}\n {e.InnerException}");
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

        public static async Task<T> DeserializeFromBinaryAsync<T>(byte[] bytes, int offset = 0, int count = -1)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return default;
            }
            return await Task.Run(() => DeserializeFromBinary<T>(bytes, offset, count));
        }
    }
}