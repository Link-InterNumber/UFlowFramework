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
            LocalizationManager.instance.onLanguageChange.AddListener(OnLocalChange);
            _addListener = true;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _paramCache = null;
            if(!Application.isPlaying) return;
            if (_addListener)
            {
                LocalizationManager.instance.onLanguageChange.RemoveListener(OnLocalChange);
            }
            localizationKey = null;
        }

        public void SetLocalizationText(string key, params object[] param)
        {
            localizationKey = key;
            if (param != null && param.Length > 0)
            {
                _paramCache = param;
            }
            else
            {
                _paramCache = null;
            }
            SetLocalizedText();
            if(_addListener) return;
            LocalizationManager.instance.onLanguageChange.AddListener(OnLocalChange);
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
            if (!LocalizationManager.instance.TryGetString(localizationKey, out var localizedText, _paramCache))
            {
#if UNITY_EDITOR
                text = $"[N/A]{localizationKey}";
#endif
                return;
            }
            text = localizedText;
        }
    }
}