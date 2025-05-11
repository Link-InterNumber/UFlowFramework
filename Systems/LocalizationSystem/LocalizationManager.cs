using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PowerCellStudio
{
    public class LocalizationManager : SingletonBase<LocalizationManager>
    {
        private StringTable _stringTable;
        private AssetTable _assetTable;
        private Language _curLanguage = ConstSetting.DefaultLanguage;
        public Language curLanguage => _curLanguage;
        
        public bool isChinese => curLanguage == Language.ChineseSimplified
                                 || curLanguage == Language.ChineseTraditional;

        public bool isChineseSimplified => curLanguage == Language.ChineseSimplified;
        public bool isChineseTraditional => curLanguage == Language.ChineseTraditional;

        private Font _fontAsset;
        public Font font => _fontAsset;
        private IAssetLoader _assetLoader;

        public IEnumerator Init(Action callback)
        {
            _curLanguage = ConstSetting.DefaultLanguage;
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[(int)_curLanguage];
            return ChangeLanguageHandle(callback);
        }

        private IEnumerator LoadStringTable()
        {
            var operationHandle = LocalizationSettings.StringDatabase.GetTableAsync(ConstSetting.LocalizationStringTable);
            yield return operationHandle;
            if (operationHandle.Status == AsyncOperationStatus.Succeeded)
            {
                _stringTable = operationHandle.Result;
            }
            else
            {
                AssetLog.LogError($"Can not load Localization string table: [{ConstSetting.LocalizationStringTable}]\n{operationHandle.OperationException}");
            }
        }
        
        private IEnumerator LoadAssetTable()
        {
            var operationHandle = LocalizationSettings.AssetDatabase.GetTableAsync(ConstSetting.LocalizationAssetTable);
            yield return operationHandle;
            if (operationHandle.Status == AsyncOperationStatus.Succeeded)
            {
                _assetTable = operationHandle.Result;
            }
            else
            {
                AssetLog.LogError($"Can not load Localization string table: [{ConstSetting.LocalizationAssetTable}]\n{operationHandle.OperationException}");
            }
        }

        public string GetString(string key, params object[] param)
        {
            if (_stringTable == null) return "N/A";
            var entry = _stringTable.GetEntry(key);
            if (entry == null) return key;
            return string.Format(entry.GetLocalizedString(), param);
        }
        
        public bool TryGetString(string key, out string result)
        {
            if (_stringTable == null)
            {
                result = "N/A";
                return false;
            }
            var entry = _stringTable.GetEntry(key);
            result = entry?.GetLocalizedString() ?? key;
            return entry != null;
        }
        
        public AsyncOperationHandle<T> GetAssetAsync<T>(string key) where T : UnityEngine.Object
        {
            if (_assetTable == null) return default;
            return _assetTable.GetAssetAsync<T>(key);
        }
        
        public string GetAssetGuid(string key)
        {
            if (_assetTable == null) return string.Empty;
            var entry = _assetTable.GetEntry(key);
            if (entry == null) return key;
            return entry.Address;
        }
        
        public bool TryGetAssetGuid(string key, out string result)
        {
            if (_assetTable == null)
            {
                result = string.Empty;
                return false;
            }
            var entry = _assetTable.GetEntry(key);
            result = entry?.Address ?? key;
            return entry != null;
        }

        public Coroutine ChangeLanguage(Language language, Action callBack = null)
        {
            if (_curLanguage == language) return null;
            _curLanguage = language;
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[(int)language];
            return ApplicationManager.instance.StartCoroutine(ChangeLanguageHandle(callBack));
        }

        private IEnumerator ChangeLanguageHandle(Action callBack)
        {
            yield return LoadStringTable();
            yield return LoadAssetTable();
            if (ConstSetting.LanguageFont.TryGetValue(_curLanguage, out var fontPath))
            {
                if(_assetLoader != null) AssetUtils.DeSpawnLoader(_assetLoader);
                _assetLoader = AssetUtils.SpawnLoader(this.GetType().Name);
                var handler = _assetLoader.LoadAsYieldInstruction<Font>(fontPath);
                yield return handler;
                _fontAsset = handler.asset;
            }
            EventManager.instance.onLanguageChange.Invoke(_curLanguage);
            callBack?.Invoke();
        }
    }
}