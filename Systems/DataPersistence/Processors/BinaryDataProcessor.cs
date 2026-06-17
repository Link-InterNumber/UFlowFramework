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
                var bytes = SerializeBinaryPayload(PlayerDataType.Binary, data);
                bytes = PersistenceEnvelopeUtility.PackBinary(GetCurrentVersion<T>(), bytes);
                if (encrypt) bytes = EncryptUtils.AESEncrypt(bytes, ConstSetting.FileEncryptionKey);
                File.WriteAllBytes(filePath, bytes);
                LinkLogger.Log($"Save a Binary at {filePath}");
            }
            catch (Exception e)
            {
                LinkLogger.LogError($"Failed to save binary data: {e.Message}");
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
                    var bytes = SerializeBinaryPayload(PlayerDataType.Binary, data);
                    bytes = PersistenceEnvelopeUtility.PackBinary(GetCurrentVersion<T>(), bytes);
                    if (encrypt)
                    {
                        bytes = EncryptUtils.AESEncrypt(bytes, ConstSetting.FileEncryptionKey);
                    }

                    await File.WriteAllBytesAsync(filePath, bytes);
                });

                LinkLogger.Log($"Save a Binary at {filePath}");
                onComplete?.Invoke(true);
            }
            catch (Exception e)
            {
                LinkLogger.LogError($"Failed to save binary data: {e.Message}");
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
                var content = decrypt ? EncryptUtils.AESDecrypt(encryptedData, ConstSetting.FileEncryptionKey) : encryptedData;
                var version = 0;
                if (PersistenceEnvelopeUtility.TryUnpackBinary(content, out var storedVersion, out var payload))
                {
                    version = storedVersion;
                    var result = DeserializeBinaryPayload<T>(PlayerDataType.Binary, version, payload);
                    if (result != null && TryUpgradeData(version, result, out var upgradedData, out var upgraded) && upgraded)
                    {
                        result = upgradedData;
                        Save(saveKey, result, decrypt);
                    }

                    return result;
                }

                var legacyResult = DeserializeBinaryPayload<T>(PlayerDataType.Binary, version, content);
                if (legacyResult != null && TryUpgradeData(version, legacyResult, out var upgradedLegacy, out var legacyUpgraded) && legacyUpgraded)
                {
                    legacyResult = upgradedLegacy;
                    Save(saveKey, legacyResult, decrypt);
                }

                return legacyResult;
            }
            catch (Exception e)
            {
                LinkLogger.LogError($"Failed to read binary data: {e.Message}");
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
                    var content = decrypt ? EncryptUtils.AESDecrypt(encryptedData, ConstSetting.FileEncryptionKey) : encryptedData;
                    var version = 0;
                    if (PersistenceEnvelopeUtility.TryUnpackBinary(content, out var storedVersion, out var payload))
                    {
                        version = storedVersion;
                        data = DeserializeBinaryPayload<T>(PlayerDataType.Binary, version, payload);
                    }
                    else
                    {
                        data = DeserializeBinaryPayload<T>(PlayerDataType.Binary, version, content);
                    }

                    if (data != null && TryUpgradeData(version, data, out var upgradedData, out var upgraded) && upgraded)
                    {
                        data = upgradedData;
                        Save(saveKey, data, decrypt);
                    }
                });
                onComplete?.Invoke(data);
            }
            catch (Exception e)
            {
                LinkLogger.LogError($"Failed to read binary data: {e.Message}");
                onComplete?.Invoke(default);
            }
        }
    }
}