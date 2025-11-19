using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PowerCellStudio
{
    public abstract class AssetLocalizationSwitch : MonoBehaviour
    {
        public string localizationKey;

        private void Awake()
        {
            if (!Application.isPlaying) return;
            EventManager.instance.onLanguageChange.AddListener(OnLocalChange);
        }

        private void OnDestroy()
        {
            if (!Application.isPlaying) return;
            EventManager.instance.onLanguageChange.RemoveListener(OnLocalChange);
            LocalizationManager.instance.ReleaseAsset(localizationKey);
            localizationKey = null;
        }
        
        private void OnEnable()
        {
            OnLocalChange(LocalizationManager.instance.curLanguage);
        }
        
        private void OnLocalChange(Language data)
        {
            BeforeLoaded();
            var handler = LocalizationManager.instance.GetAssetAsync<Object>(localizationKey);
            handler.OnLoadSuccess(OnLoaded);
            handler.OnLoadFailed(OnLoadFailed);
        }

        protected abstract void BeforeLoaded();

        protected abstract void OnLoaded(Object asset);

        protected abstract void OnLoadFailed();
        
    }
}