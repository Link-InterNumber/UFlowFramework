using TMPro;
using UnityEngine;

namespace PowerCellStudio
{
    [AddComponentMenu("UI/TextMeshProUGUIEx", 32)]
    public class TextMeshProUGUIEx : TextMeshProUGUI
    {
        public bool staticText;
        public string localizationKey;

        protected override void Awake()
        {
            base.Awake();
            if(!Application.isPlaying) return;
            if (!staticText || string.IsNullOrEmpty(localizationKey)) return;
            SetLocalizedText();
            EventManager.instance.onLanguageChange.AddListener(OnLocalChange);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if(!Application.isPlaying) return;
            if (!staticText || string.IsNullOrEmpty(localizationKey)) return;
            EventManager.instance.onLanguageChange.RemoveListener(OnLocalChange);
        }

        public void SetLocalizationText(string key, params object[] param)
        {
            if (param != null && param.Length > 0)
            {
                text = string.Format(LocalizationManager.instance.GetString(key), param);
            }
            else
            {
                text = LocalizationManager.instance.GetString(key);
            }
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
            text = LocalizationManager.instance.GetString(localizationKey);
        }
    }
}