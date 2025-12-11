using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using System.IO.Compression;
#if !UNITY_WEBGL
using System.Threading.Tasks;
#endif

namespace PowerCellStudio
{
    [DataProcessor(PlayerDataType.Binary)]
    public class BinaryDataProcessor : PersistenceDataProcessor
    {
        private static readonly string _directoryName = "Binary";
        private static readonly string _extension = "bytes";
        public override string directoryName => _directoryName;
        public override string extension => _extension;

        public override bool Save<T>(string saveKey, T data, bool encrypt)
        {
            if (!TryGetSaveFilePath(saveKey, out var filePath)) return false;
            CheckDirectory();
            try
            {
                var json = JsonConvert.SerializeObject(data, Formatting.None, new JsonSerializerSettings
                {
                    // TypeNameHandling = TypeNameHandling.Auto,
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects
                });
                var bytes = Encoding.UTF8.GetBytes(json);
                // 2. GZip 压缩
                using (var memoryStream = new MemoryStream())
                {
                    using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                    {
                        gzipStream.Write(bytes, 0, bytes.Length);
                    }
                    bytes = memoryStream.ToArray();
                }
                if (encrypt) bytes = EncryptUtils.AESEncrypt(bytes, ConstSetting.FileEncryptionKey);
                File.WriteAllBytes(filePath, bytes);
                
                LinkLog.Log($"Save a Binary at {filePath}");
            }
            catch (Exception e)
            {
                LinkLog.LogError($"Failed to save binary data: {e.Message}");
                return false;
            }
            return true;
        }

        public override void SaveAsync<T>(string saveKey, T data, Action<bool> onComplete, bool encrypt)
        {
#if UNITY_WEBGL
            var isSuccess = Save<T>(saveKey, data, encrypt);
            onComplete?.Invoke(isSuccess);
#else
            CheckDirectory();
            _ = SaveDataBinaryHandler(saveKey, data, onComplete, encrypt);
#endif
        }
        private async Task SaveDataBinaryHandler<T>(string saveKey, T data, Action<bool> onComplete, bool encrypt)
        {
            if (!TryGetSaveFilePath(saveKey, out var filePath))
            {
                onComplete?.Invoke(false);
                return;
            }
            try
            {
                // 将繁重的 序列化 和 加密 工作移至线程池，避免阻塞主线程
                await Task.Run(async () =>
                {
                    var json = JsonConvert.SerializeObject(data, Formatting.None, new JsonSerializerSettings
                    {
                        // TypeNameHandling = TypeNameHandling.Auto,
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects
                    });
                    var bytes = Encoding.UTF8.GetBytes(json);

                    // 2. GZip 压缩
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                        {
                            await gzipStream.WriteAsync(bytes, 0, bytes.Length);
                        }
                        bytes = memoryStream.ToArray();
                    }
                
                    if (encrypt)
                    {
                        bytes = EncryptUtils.AESEncrypt(bytes, ConstSetting.FileEncryptionKey);
                    }

                    await File.WriteAllBytesAsync(filePath, bytes);
                });

                LinkLog.Log($"Save a Binary at {filePath}");
                onComplete?.Invoke(true);
            }
            catch (Exception e)
            {
                LinkLog.LogError($"Failed to save binary data: {e.Message}");
                onComplete?.Invoke(false);
            }
        }

        public override T Read<T>(string saveKey, bool decrypt)
        {
            if (!TryGetSaveFilePath(saveKey, out var filePath)) return default;
            if (!File.Exists(filePath)) return default;

            try
            {
                byte[] encryptedData = File.ReadAllBytes(filePath);
                var decryptedData = decrypt ? EncryptUtils.AESDecrypt(encryptedData, ConstSetting.FileEncryptionKey) : encryptedData;

                using (var compressedStream = new MemoryStream(decryptedData))
                using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                using (var resultStream = new MemoryStream())
                {
                    gzipStream.CopyTo(resultStream);
                    decryptedData = resultStream.ToArray();
                }

                var json = Encoding.UTF8.GetString(decryptedData);
                T data = JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
                {
                    // TypeNameHandling = TypeNameHandling.Auto,
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects
                });
                return data;
            }
            catch (Exception e)
            {
                LinkLog.LogError($"Failed to read binary data: {e.Message}");
                return default;
            }
        }

        public override void ReadAsync<T>(string saveKey, Action<T> onComplete, bool decrypt)
        {
#if UNITY_WEBGL
            var data = Read<T>(saveKey, decrypt);
            onComplete?.Invoke(data);
#else
            if (!TryGetSaveFilePath(saveKey, out var filePath)) return;
            if (!File.Exists(filePath)) return;
            _ = ReadDataBinaryHandler(saveKey, onComplete, decrypt);
#endif
        }
        private async Task ReadDataBinaryHandler<T>(string saveKey, Action<T> onComplete, bool decrypt)
        {
            if (!TryGetSaveFilePath(saveKey, out var filePath))
            {
                onComplete?.Invoke(default);
                return;
            }
            try
            {
                T data = default;
                await Task.Run(async () =>
                {
                    byte[] encryptedData = await File.ReadAllBytesAsync(filePath);
                    var decryptedData = decrypt ? EncryptUtils.AESDecrypt(encryptedData, ConstSetting.FileEncryptionKey) : encryptedData;

                    using (var compressedStream = new MemoryStream(decryptedData))
                    using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                    using (var resultStream = new MemoryStream())
                    {
                        await gzipStream.CopyToAsync(resultStream);
                        decryptedData = resultStream.ToArray();
                    }
                
                    var json = Encoding.UTF8.GetString(decryptedData);
                    data = JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
                    {
                        // TypeNameHandling = TypeNameHandling.Auto,
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects
                    });
                });
                onComplete?.Invoke(data);
            }
            catch (Exception e)
            {
                LinkLog.LogError($"Failed to read binary data: {e.Message}");
                onComplete?.Invoke(default);
            }
        }
    }
}