using System;
using System.IO;
using UnityEngine;

namespace PowerCellStudio
{
    [DataProcessor(PlayerDataType.PlayerPrefs)]
    public class PlayerPrefsProcessor : PersistenceDataProcessor
    {
        public override string directoryName => "PlayerPref";

        public override string extension => "playerPref";

        public override bool HasSave(string saveKey)
        {
            return PlayerPrefs.HasKey(saveKey);
        }

        public override void Clear(string saveKey)
        {
            PlayerPrefs.DeleteKey(saveKey);
            base.Clear(saveKey);
        }

        public override void ClearAll()
        {
            PlayerPrefs.DeleteAll();
            base.ClearAll();
        }

        public override bool Save<T>(string saveKey, T data, bool encrypt)
        {
            if (string.IsNullOrEmpty(saveKey)) return false;
            string json = SerializeStringPayload(PlayerDataType.PlayerPrefs, data);
            var versionedJson = PersistenceEnvelopeUtility.PackString(GetCurrentVersion<T>(), json);
            if (encrypt)
            {
                var jsonEn = EncryptUtils.Base64Encrypt(versionedJson);
                PlayerPrefs.SetString(saveKey, jsonEn);
            }
            else
            {
                PlayerPrefs.SetString(saveKey, versionedJson);
            }
            PlayerPrefs.Save();
            if (TryGetSaveFilePath(saveKey, out var filePath))
            {
                CheckDirectory();
                File.WriteAllText(filePath, "");
            }
            return true;
        }

        public override void SaveAsync<T>(string saveKey, T data, Action<bool> onComplete, bool encrypt)
        {
            var isSuccessResult = Save<T>(saveKey, data, encrypt);
            onComplete?.Invoke(isSuccessResult);
        }

        public override T Read<T>(string saveKey, bool decrypt)
        {
            if (!PlayerPrefs.HasKey(saveKey)) return default;
            string json = PlayerPrefs.GetString(saveKey, "{}");
            var content = decrypt ? EncryptUtils.Base64Decrypt(json) : json;
            var version = 0;
            T result;
            if (PersistenceEnvelopeUtility.TryUnpackString(content, out var storedVersion, out var payload))
            {
                version = storedVersion;
                result = DeserializeStringPayload<T>(PlayerDataType.PlayerPrefs, version, payload);
            }
            else
            {
                result = DeserializeStringPayload<T>(PlayerDataType.PlayerPrefs, version, content);
            }

            if (result != null && TryUpgradeData(version, result, out var upgradedData, out var upgraded) && upgraded)
            {
                result = upgradedData;
                Save(saveKey, result, decrypt);
            }

            return result;
        }

        public override void ReadAsync<T>(string saveKey, Action<T> onComplete, bool decrypt)
        {
            var data = Read<T>(saveKey, decrypt);
            onComplete?.Invoke(data);
        }
    }
}