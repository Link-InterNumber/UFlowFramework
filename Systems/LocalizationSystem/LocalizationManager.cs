using System;
using System.IO;
using System.Collections;
using System.Linq;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

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
            if (AssetUtils.loadMode != AssetUtils.LoadMode.Addressable)
            {
                var assetLoader = AssetUtils.SpawnLoader(this.GetType().Name);
                var settingPath = AssetUtils.CombinePaths(ConstSetting.LocalizationSettingDirectory, ConstSetting.LocalizationSettingName);
                var handler = assetLoader.LoadAsYieldInstruction<LocalizationSettings>(settingPath);
                yield return handler;
                var settings = handler.asset;
                handler.Dispose();
                if (settings)
                {
                    LocalizationSettings.Instance = settings;
                    AssetLog.Log("Localization initialized from AssetBundle.");
                }
                else
                {
                    AssetLog.LogError("Failed to load LocalizationSettings from AssetBundle.");
                    yield break;
                }
            }
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[(int)_curLanguage];
            yield return ChangeLanguageHandle(callback);
        }

        private IEnumerator LoadStringTable()
        {
            if (AssetUtils.loadMode == AssetUtils.LoadMode.Addressable)
            {
                var operationHandle = LocalizationSettings.StringDatabase.GetTableAsync(Path.GetFileNameWithoutExtension(ConstSetting.LocalizationStringTable));
                yield return operationHandle;
                _stringTable = operationHandle.Result;
                if (!_stringTable)
                {
                    AssetLog.LogError($"Can not load Localization string table: [{ConstSetting.LocalizationStringTable}]\n{operationHandle.OperationException}");
                }
            }
            else
            {
                var fileName = $"{Path.GetFileNameWithoutExtension(ConstSetting.LocalizationStringTable)}_{LocalizationSettings.SelectedLocale.Identifier.Code}.asset";
                var path = AssetUtils.CombinePaths(ConstSetting.LocalizationSettingDirectory, fileName);
                var handler = _assetLoader.LoadAsYieldInstruction<StringTable>(path);
                yield return handler;
                _stringTable = handler.asset;
                handler.Dispose();
                if (!_stringTable)
                {
                    AssetLog.LogError($"Can not load Localization string table: [{ConstSetting.LocalizationStringTable}]");
                }
            }

        }
        
        private IEnumerator LoadAssetTable()
        {
            if (_assetTable) _assetTable.ReleaseAssets();
            if (AssetUtils.loadMode == AssetUtils.LoadMode.Addressable)
            {
                var operationHandle = LocalizationSettings.AssetDatabase.GetTableAsync(Path.GetFileNameWithoutExtension(ConstSetting.LocalizationAssetTable));
                yield return operationHandle;
                _assetTable = operationHandle.Result;
                if (!_assetTable)
                {
                    AssetLog.LogError($"Can not load Localization string table: [{ConstSetting.LocalizationAssetTable}]\n{operationHandle.OperationException}");
                }
            }
            else
            {
                var fileName = $"{Path.GetFileNameWithoutExtension(ConstSetting.LocalizationAssetTable)}_{LocalizationSettings.SelectedLocale.Identifier.Code}.asset";
                var path = AssetUtils.CombinePaths(ConstSetting.LocalizationSettingDirectory, fileName);
                var handler = _assetLoader.LoadAsYieldInstruction<AssetTable>(path);
                yield return handler;
                _assetTable = handler.asset;
                handler.Dispose();
                if (!_assetTable)
                {
                    AssetLog.LogError($"Can not load Localization string table: [{ConstSetting.LocalizationAssetTable}]");
                }
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

        public void ReleaseAsset(string key)
        {
            _assetTable.ReleaseAsset(key);
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
            if (ConstSetting.LanguageFont.TryGetValue(_curLanguage, out var fontPath))
            {
                if (!_fontAsset || !_fontAsset.name.Equals(Path.GetFileNameWithoutExtension(fontPath)))
                {
                    if(_assetLoader != null) AssetUtils.DeSpawnLoader(_assetLoader);
                    _assetLoader = AssetUtils.SpawnLoader(this.GetType().Name);
                    var handler = _assetLoader.LoadAsYieldInstruction<Font>(fontPath);
                    yield return handler;
                    if (handler.asset) _fontAsset = handler.asset;
                    handler.Dispose();
                }
            }
            if(_assetLoader == null)
                _assetLoader = AssetUtils.SpawnLoader(this.GetType().Name);
            yield return LoadStringTable();
            yield return LoadAssetTable();
            EventManager.instance.onLanguageChange.Invoke(_curLanguage);
            callBack?.Invoke();
        }
    }
}