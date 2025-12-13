using System;
using System.IO;
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
                var bytes = SerializeUtils.SerializeToBinary(data);
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
                    var bytes = await SerializeUtils.SerializeToBinaryAsync(data);
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
                
                return SerializeUtils.DeserializeFromBinary<T>(decryptedData);
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
            if (!TryGetSaveFilePath(saveKey, out var filePath))
            {
                onComplete?.Invoke(default);
                return;
            }

            if (!File.Exists(filePath))
            {
                onComplete?.Invoke(default);
                return;
            }
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
                    data = await SerializeUtils.DeserializeFromBinaryAsync<T>(decryptedData);
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