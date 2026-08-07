using System;
using System.IO;
#if !UNITY_WEBGL
using System.Threading.Tasks;
#endif

namespace PowerCellStudio
{
    [DataProcessor(PlayerDataType.Json)]
    public class JsonDataProcessor : PersistenceDataProcessor
    {
        private static readonly string _directoryName = "Json";
        private static readonly string _extension = "json";
        public override string directoryName => _directoryName;
        public override string extension => _extension;

        public override bool Save<T>(string saveKey, T data, bool encrypt)
        {
            if (!TryGetSaveFilePath(saveKey, out var filePath)) return false;
            CheckDirectory();
            try
            {
                string json = SerializeStringPayload(PlayerDataType.Json, data);
                var versionedJson = PersistenceEnvelopeUtility.PackString(GetCurrentVersion<T>(), json);
                if (encrypt)
                {
                    var jsonEn = EncryptUtils.AESGcmEncrypt(versionedJson, ConstSetting.FileEncryptionKey);
                    File.WriteAllText(filePath, jsonEn);
                }
                else
                {
                    File.WriteAllText(filePath, versionedJson);
                }
                LinkLogger.Log($"Save a Json at {filePath}");
                return true;
            }
            catch (Exception e)
            {
                LinkLogger.LogError($"Failed to save json data: {e.Message}");
                return false;
            }
        }

        public override void SaveAsync<T>(string saveKey, T data, Action<bool> onComplete, bool encrypt)
        {
#if UNITY_WEBGL
            var isSuccess = Save<T>(saveKey, data, encrypt);
            onComplete?.Invoke(isSuccess);
#else
            CheckDirectory();
            _ = SaveDataJsonHandler(saveKey, data, onComplete, encrypt);
#endif
        }

        private async Task SaveDataJsonHandler<T>(string saveKey, T data, Action<bool> onComplete, bool encrypt)
        {
            if (!TryGetSaveFilePath(saveKey, out var filePath))
            {
                onComplete?.Invoke(default);
                return;
            }
            try
            {
                await Task.Run(async () =>
                {
                    string json = SerializeStringPayload(PlayerDataType.Json, data);
                    var versionedJson = PersistenceEnvelopeUtility.PackString(GetCurrentVersion<T>(), json);
                    if (encrypt)
                    {
                        var jsonEn = EncryptUtils.AESGcmEncrypt(versionedJson, ConstSetting.FileEncryptionKey);
                        await File.WriteAllTextAsync(filePath, jsonEn);
                    }
                    else
                    {
                        await File.WriteAllTextAsync(filePath, versionedJson);
                    }
                });
                LinkLogger.Log($"Save a Json at {filePath}");
                onComplete?.Invoke(true);
            }
            catch (Exception e)
            {
                LinkLogger.LogError($"Failed to save json data: {e.Message}");
                onComplete?.Invoke(false);
            }
        }

        public override T Read<T>(string saveKey, bool decrypt)
        {
            if (!TryGetSaveFilePath(saveKey, out var filePath)) return default;
            if (!File.Exists(filePath)) return default;

            T result = default;
            try
            {
                var rawText = File.ReadAllText(filePath);
                var content = decrypt
                    ? EncryptUtils.AESGcmDecrypt(rawText, ConstSetting.FileEncryptionKey)
                    : rawText;
                var version = 0;
                if (PersistenceEnvelopeUtility.TryUnpackString(content, out var storedVersion, out var payload))
                {
                    version = storedVersion;
                    result = DeserializeStringPayload<T>(PlayerDataType.Json, version, payload);
                }
                else
                {
                    result = DeserializeStringPayload<T>(PlayerDataType.Json, version, content);
                }

                if (result != null && TryUpgradeData(version, result, out var upgradedData, out var upgraded) && upgraded)
                {
                    result = upgradedData;
                    Save(saveKey, result, decrypt);
                }
            }
            catch (Exception e)
            {
                LinkLogger.LogError($"Failed to read json data: {e.Message}");
            }
            return result;
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
            if (!TryGetSaveFilePath(saveKey, out var filePath) || !File.Exists(filePath))
            {
                onComplete?.Invoke(default);
                return;
            }
            try
            {
                T data = default;
                await Task.Run(async () =>
                {
                    var rawText = await File.ReadAllTextAsync(filePath);
                    try
                    {
                        var content = decrypt
                            ? EncryptUtils.AESGcmDecrypt(rawText, ConstSetting.FileEncryptionKey)
                            : rawText;
                        var version = 0;
                        if (PersistenceEnvelopeUtility.TryUnpackString(content, out var storedVersion, out var payload))
                        {
                            version = storedVersion;
                            data = DeserializeStringPayload<T>(PlayerDataType.Json, version, payload);
                        }
                        else
                        {
                            data = DeserializeStringPayload<T>(PlayerDataType.Json, version, content);
                        }

                        if (data != null && TryUpgradeData(version, data, out var upgradedData, out var upgraded) && upgraded)
                        {
                            data = upgradedData;
                            Save(saveKey, data, decrypt);
                        }
                    }
                    catch (Exception e)
                    {
                        LinkLogger.LogError($"Failed to read json data: {e.Message}");
                    }
                });
                onComplete?.Invoke(data);
            }
            catch (Exception e)
            {
                LinkLogger.LogError($"Failed to read json data: {e.Message}");
                onComplete?.Invoke(default);
            }
        }
    }
}