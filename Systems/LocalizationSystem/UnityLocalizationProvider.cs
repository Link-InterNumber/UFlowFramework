using System;
using System.Collections;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PowerCellStudio
{
    public class UnityLocalizationProvider : ILocalizationProvider
    {
        private StringTable _stringTable;
        private AssetTable _assetTable;

        public void Init(Language language)
        {
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[(int)language];
        }

        public IEnumerator ChangeLanguage(Language language)
        {
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[(int)language];
            yield return LoadStringTable();
            yield return LoadAssetTable();
        }

        public LoaderYieldInstruction<T> GetAssetAsync<T>(string key) where T : UnityEngine.Object
        {
            var path = GetAssetPath(key);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }
            if (_assetTable == null) return null;
            var _loaderYieldInstruction = AssetUtils.GetLoadHandler<T>(path);
            var handler = _assetTable.GetAssetAsync<T>(key);
            handler.Completed += op =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    _loaderYieldInstruction.SetAsset(op.Result);
                }
                else
                {
                    AssetLog.LogError($"Failed to load localized asset for key: {key}\n{op.OperationException}");
                    _loaderYieldInstruction.SetAsset(null);
                }
            };
            return _loaderYieldInstruction;
        }

        public string GetAssetGuid(string key)
        {
            if (_assetTable == null) return string.Empty;
            var entry = _assetTable.GetEntry(key);
            if (entry == null) return string.Empty;
            return entry.Guid;
        }

        public string GetAssetPath(string key)
        {
            if (_assetTable == null) return string.Empty;
            var entry = _assetTable.GetEntry(key);
            if (entry == null) return key;
            return entry.Address;
        }

        public bool TryGetString(string key, out string result, params object[] param)
        {
            if (_stringTable == null)
            {
                result = "N/A";
                return false;
            }
            var entry = _stringTable.GetEntry(key);
            var entryStr = entry?.GetLocalizedString() ?? key;
            if (param == null || param.Length == 0)
            {
                result = entryStr;
            }
            else
            {
                result = string.Format(entryStr, param);
            }
            return entry != null;
        }

        public void ReleaseAsset(string key)
        {
            _assetTable.ReleaseAsset(key);
        }

        private IEnumerator LoadStringTable()
        {
            var operationHandle = LocalizationSettings.StringDatabase.GetTableAsync(ConstSetting.LocalizationStringTable);
            yield return operationHandle;
            _stringTable = operationHandle.Result;
            if (!_stringTable)
            {
                AssetLog.LogError($"Can not load Localization string table: [{ConstSetting.LocalizationStringTable}]\n{operationHandle.OperationException}");
            }
        }

        private IEnumerator LoadAssetTable()
        {
            var operationHandle = LocalizationSettings.AssetDatabase.GetTableAsync(ConstSetting.LocalizationAssetTable);
            yield return operationHandle;
            _assetTable = operationHandle.Result;
            if (!_assetTable)
            {
                AssetLog.LogError($"Can not load Localization string table: [{ConstSetting.LocalizationAssetTable}]\n{operationHandle.OperationException}");
            }
        }
    }
}