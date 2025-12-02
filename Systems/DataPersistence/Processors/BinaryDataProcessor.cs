using System;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
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
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(memoryStream, data);
                    var bytes = memoryStream.ToArray();
                    if (encrypt) bytes = EncryptUtils.AESEncrypt(bytes, ConstSetting.FileEncryptionKey);
                    File.WriteAllBytes(filePath, bytes);
                    memoryStream.Close();
                }
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
                await Task.Run(() =>
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        BinaryFormatter formatter = new BinaryFormatter();
                        formatter.Serialize(memoryStream, data);

                        var bytes = memoryStream.ToArray();
                        if (encrypt)
                        {
                            bytes = EncryptUtils.AESEncrypt(bytes, ConstSetting.FileEncryptionKey);
                        }

                        File.WriteAllBytesAsync(filePath, bytes);
                    }
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
                using MemoryStream memoryStream = new MemoryStream(decryptedData);
                // 使用BinaryFormatter进行反序列化
                BinaryFormatter formatter = new BinaryFormatter();
                T data = (T)formatter.Deserialize(memoryStream);
                // 关闭文件流
                memoryStream.Close();
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
                await Task.Run(() =>
                {
                    byte[] encryptedData = File.ReadAllBytes(filePath);
                    var decryptedData = decrypt ? EncryptUtils.AESDecrypt(encryptedData, ConstSetting.FileEncryptionKey) : encryptedData;
                    using (MemoryStream memoryStream = new MemoryStream(decryptedData))
                    {
                        // 使用BinaryFormatter进行反序列化
                        BinaryFormatter formatter = new BinaryFormatter();
                        data = (T)formatter.Deserialize(memoryStream);
                    }
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