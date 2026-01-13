using System;
using System.Collections;

namespace PowerCellStudio
{
    public interface ILocalizationConfigProvider
    {
        public string GetLocalizedString(string key, Language language);
    }

    public class CustomLocalizationProvider: ILocalizationProvider, IDisposable
    {
        public CustomLocalizationProvider(ILocalizationConfigProvider configProvider)
        {
            _configProvider = configProvider;
            if (_configProvider == null)
            {
                LinkLog.LogError("CustomLocalizationProvider requires a valid ILocalizationConfigProvider.");
            }
        }

        private ILocalizationConfigProvider _configProvider;
        private IAssetLoader _assetLoader;
        private Language _currentLanguage;

        public void Init(Language language)
        {
            AssetUtils.DeSpawnLoader(_assetLoader);
            _assetLoader = AssetUtils.SpawnLoader("CustomLocalizationProvider");
        }

        public bool TryGetString(string key, out string result, params object[] param)
        {
            if (_configProvider == null)
            {
                result = string.Empty;
                return false;
            }
            var localizedString = _configProvider.GetLocalizedString(key, _currentLanguage);
            if (string.IsNullOrEmpty(localizedString))
            {
                result = string.Empty;
                return false;
            }
            result = string.Format(localizedString, param);
            return true;
        }
        
        public LoaderYieldInstruction<T> GetAssetAsync<T>(string key) where T : UnityEngine.Object
        {
            var path = GetAssetPath(key);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }
            return _assetLoader.LoadAsYieldInstruction<T>(path);
        }
        
        public void ReleaseAsset(string key)
        {
            var path = GetAssetPath(key);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            _assetLoader.Release(path);
        }
        
        public string GetAssetPath(string key)
        {
            return TryGetString(key, out string path)? path : string.Empty;
        }
        
        public string GetAssetGuid(string key)
        {
            return GetAssetPath(key);
        }
        
        public IEnumerator ChangeLanguage(Language language)
        {
            _currentLanguage = language;
            yield break;
        }

        public void Dispose()
        {
            AssetUtils.DeSpawnLoader(_assetLoader);
            _assetLoader = null;
        }
   }
}