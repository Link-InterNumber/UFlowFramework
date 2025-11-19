using System;
using System.IO;
using System.Collections;
using UnityEngine;

namespace PowerCellStudio
{
   public class LocalizationManager : SingletonBase<LocalizationManager>
    {
        private ILocalizationProvider _localizationProvider;
        private Language _curLanguage = ConstSetting.DefaultLanguage;
        public Language curLanguage => _curLanguage;
        
        public bool isChinese => curLanguage == Language.ChineseSimplified
                                 || curLanguage == Language.ChineseTraditional;

        public bool isChineseSimplified => curLanguage == Language.ChineseSimplified;
        public bool isChineseTraditional => curLanguage == Language.ChineseTraditional;

        private Font _fontAsset;
        public Font font => _fontAsset;
        private IAssetLoader _assetLoader;

        public IEnumerator Init(ILocalizationProvider provider, Action callback)
        {
            _localizationProvider = provider;
            _curLanguage = ConstSetting.DefaultLanguage;
            _localizationProvider?.Init(_curLanguage);
            yield return ChangeLanguageHandle(callback);
        }

        public string GetString(string key, params object[] param)
        {
            if (_localizationProvider == null)
            {
                return string.Empty;
            }
            _localizationProvider.TryGetString(key, out string result, param);
            return result;
        }
        
        public bool TryGetString(string key, out string result, params object[] param)
        {
            if (_localizationProvider == null)
            {
                result = string.Empty;
                return false;
            }
            return _localizationProvider.TryGetString(key, out result, param);
        }
        
        public LoaderYieldInstruction<T> GetAssetAsync<T>(string key) where T : UnityEngine.Object
        {
            return _localizationProvider?.GetAssetAsync<T>(key)?? null;
        }

        public void ReleaseAsset(string key)
        {
            _localizationProvider?.ReleaseAsset(key);
        }
        
        public string GetAssetPath(string key)
        {
            return _localizationProvider?.GetAssetPath(key)?? string.Empty;
        }
        
        public bool TryGetAssetGuid(string key, out string result)
        {
            result = _localizationProvider?.GetAssetGuid(key);
            return !string.IsNullOrEmpty(result);
        }

        public Coroutine ChangeLanguage(Language language, Action callBack = null)
        {
            if (_curLanguage == language)
            {
                callBack?.Invoke();
                return null;  
            }
            _curLanguage = language;
            return ApplicationManager.RunCoroutine(ChangeLanguageHandle(callBack));
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
            yield return _localizationProvider.ChangeLanguage(_curLanguage);
            EventManager.instance.onLanguageChange.Invoke(_curLanguage);
            callBack?.Invoke();
        }
    }
}