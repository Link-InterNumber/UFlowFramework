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
                string json = SerializeUtils.SerializeToJson(data);
                if (encrypt)
                {
                    var jsonEn = EncryptUtils.AESEncrypt(json, ConstSetting.FileEncryptionKey);
                    File.WriteAllText(filePath, jsonEn);
                }
                else
                {
                    File.WriteAllText(filePath, json);
                }
                LinkLog.Log($"Save a Json at {filePath}");
                return true;
            }
            catch (Exception e)
            {
                LinkLog.LogError($"Failed to save json data: {e.Message}");
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
                    string json = SerializeUtils.SerializeToJson(data);
                    if (encrypt)
                    {
                        var jsonEn = EncryptUtils.AESEncrypt(json, ConstSetting.FileEncryptionKey);
                        await File.WriteAllTextAsync(filePath, jsonEn);
                    }
                    else
                    {
                        await File.WriteAllTextAsync(filePath, json);
                    }
                });
                LinkLog.Log($"Save a Json at {filePath}");
                onComplete?.Invoke(true);
            }
            catch (Exception e)
            {
                LinkLog.LogError($"Failed to save json data: {e.Message}");
                onComplete?.Invoke(false);
            }
        }

        public override T Read<T>(string saveKey, bool decrypt)
        {
            if (!TryGetSaveFilePath(saveKey, out var filePath)) return default;
            if (!File.Exists(filePath)) return default;

            var jsonEn = File.ReadAllText(filePath);
            T result = default;
            try
            {
                if (decrypt)
                {
                    var json = EncryptUtils.AESDecrypt(jsonEn, ConstSetting.FileEncryptionKey);
                    result = SerializeUtils.DeserializeFromJson<T>(json);
                }
                result = SerializeUtils.DeserializeFromJson<T>(jsonEn);
            }
            catch (Exception e)
            {
                LinkLog.LogError($"Failed to read json data: {e.Message}");
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
                    var jsonEn = await File.ReadAllTextAsync(filePath);
                    try
                    {
                        if (decrypt)
                        {
                            var json = EncryptUtils.AESDecrypt(jsonEn, ConstSetting.FileEncryptionKey);
                            data = SerializeUtils.DeserializeFromJson<T>(json);
                        }
                        data = SerializeUtils.DeserializeFromJson<T>(jsonEn);
                    }
                    catch (Exception e)
                    {
                        LinkLog.LogError($"Failed to read json data: {e.Message}");
                    }
                });
                onComplete?.Invoke(data);
            }
            catch (Exception e)
            {
                LinkLog.LogError($"Failed to read json data: {e.Message}");
                onComplete?.Invoke(default);
            }
        }
    }
}