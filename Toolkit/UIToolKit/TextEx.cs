using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    [AddComponentMenu("UI/TextEx", 31)]
    public class TextEx : Text
    {
        public bool staticText;
        public string localizationKey;
        public bool changeFontWhenLanChange = true;

        private bool _addListener;
        private object[] _paramCache;

        protected override void Awake()
        {
            base.Awake();
            if(!Application.isPlaying) return;
            if(changeFontWhenLanChange)
            {
                EventManager.instance.onLanguageChange.AddListener(ChangeFont);
                ChangeFont(LocalizationManager.instance.curLanguage);
            }
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
            EventManager.instance.onLanguageChange.RemoveListener(OnLocalChange);
            EventManager.instance.onLanguageChange.RemoveListener(ChangeFont);
        }

        private void ChangeFont(Language data)
        {
            if (LocalizationManager.instance.font != null)
                font = LocalizationManager.instance.font;
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
            _addListener = true;
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