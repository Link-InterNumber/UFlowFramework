using TMPro;
using UnityEngine;

namespace PowerCellStudio
{
    [AddComponentMenu("UI/TextMeshProUGUIEx", 32)]
    public class TextMeshProUGUIEx : TextMeshProUGUI
    {
        public bool staticText;
        public string localizationKey;

        private bool _addListener;
        private object[] _paramCache;

        protected override void Awake()
        {
            base.Awake();
            if(!Application.isPlaying) return;
            if (!staticText || string.IsNullOrEmpty(localizationKey)) return;
            SetLocalizedText();
            EventManager.instance.onLanguageChange.AddListener(OnLocalChange);
            _addListener = true;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _paramCache = null;
            localizationKey = null;
            if(!Application.isPlaying) return;
            if (!staticText || string.IsNullOrEmpty(localizationKey)) return;
            EventManager.instance.onLanguageChange.RemoveListener(OnLocalChange);
        }

        public void SetLocalizationText(string key, params object[] param)
        {
            localizationKey = key;
            if (param != null && param.Length > 0)
            {
                _paramCache = param;
                text = string.Format(LocalizationManager.instance.GetString(key), param);
            }
            else
            {
                _paramCache = null;
                text = LocalizationManager.instance.GetString(key);
            }
            if(_addListener) return;
            EventManager.instance.onLanguageChange.AddListener(OnLocalChange);
            _addListener = true
        }

        private void OnLocalChange(Language obj)
        {
            SetLocalizedText();
        }

        private void SetLocalizedText()
        {
#if UNITY_EDITOR
            if(!Application.isPlaying) return;
#endif
            if (_paramCache != null && _paramCache.Length > 0)
            {
                text = string.Format(LocalizationManager.instance.GetString(localizationKey), _paramCache);
            }
            else
            {
                text = LocalizationManager.instance.GetString(localizationKey);
            }
        }
    }
}