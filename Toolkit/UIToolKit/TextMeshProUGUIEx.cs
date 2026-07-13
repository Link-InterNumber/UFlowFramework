using TMPro;
using UnityEngine;

namespace PowerCellStudio
{
    [AddComponentMenu("UI/TextMeshProUGUIEx", 32)]
    [System.CLSCompliant(false)]
    public class TextMeshProUGUIEx : TextMeshProUGUI
    {
        public bool staticText;
        public string localizationKey;

        [Header("Four Corner Gradient")]
        public bool enableFourCornerGradient;
        public Gradient textGradient = new Gradient();
        public float gradientAngle;

        private bool _addListener;
        private object[] _paramCache;

        protected override void Awake()
        {
            base.Awake();
            if(!Application.isPlaying) return;
            if (!staticText || string.IsNullOrEmpty(localizationKey)) return;
            SetLocalizedText();
            LocalizationManager.instance?.onLanguageChange.AddListener(OnLocalChange);
            _addListener = true;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            OnPreRenderText += ApplyFourCornerGradient;
        }

        protected override void OnDisable()
        {
            OnPreRenderText -= ApplyFourCornerGradient;
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _paramCache = null;
            if(!Application.isPlaying) return;
            if (_addListener)
            {
                LocalizationManager.instance?.onLanguageChange.RemoveListener(OnLocalChange);
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
            LocalizationManager.instance?.onLanguageChange.AddListener(OnLocalChange);
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
            if (LocalizationManager.instance == null || !LocalizationManager.instance.TryGetString(localizationKey, out var localizedText, _paramCache))
            {
#if UNITY_EDITOR
                text = $"[N/A]{localizationKey}";
#endif
                return;
            }
            text = localizedText;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetVerticesDirty();
        }
#endif

        private void ApplyFourCornerGradient(TMP_TextInfo textInfo)
        {
            if (!enableFourCornerGradient || textInfo == null || textInfo.characterCount == 0) return;

            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            var hasVisibleVertex = false;
            for (var i = 0; i < textInfo.characterCount; i++)
            {
                var characterInfo = textInfo.characterInfo[i];
                if (!characterInfo.isVisible) continue;

                var meshInfo = textInfo.meshInfo[characterInfo.materialReferenceIndex];
                var vertices = meshInfo.vertices;
                var vertexIndex = characterInfo.vertexIndex;
                for (var j = 0; j < 4; j++)
                {
                    var position = vertices[vertexIndex + j];
                    min.x = Mathf.Min(min.x, position.x);
                    min.y = Mathf.Min(min.y, position.y);
                    max.x = Mathf.Max(max.x, position.x);
                    max.y = Mathf.Max(max.y, position.y);
                    hasVisibleVertex = true;
                }
            }

            if (!hasVisibleVertex) return;

            var radians = gradientAngle * Mathf.Deg2Rad;
            var cos = Mathf.Cos(radians);
            var sin = Mathf.Sin(radians);
            var center = (min + max) * 0.5f;
            var rotatedMinX = float.MaxValue;
            var rotatedMaxX = float.MinValue;
            for (var i = 0; i < textInfo.characterCount; i++)
            {
                var characterInfo = textInfo.characterInfo[i];
                if (!characterInfo.isVisible) continue;

                var meshInfo = textInfo.meshInfo[characterInfo.materialReferenceIndex];
                var vertices = meshInfo.vertices;
                var vertexIndex = characterInfo.vertexIndex;
                for (var j = 0; j < 4; j++)
                {
                    var rotatedX = GetRotatedPosition(vertices[vertexIndex + j], center, cos, sin).x;
                    rotatedMinX = Mathf.Min(rotatedMinX, rotatedX);
                    rotatedMaxX = Mathf.Max(rotatedMaxX, rotatedX);
                }
            }

            var rotatedWidth = rotatedMaxX - rotatedMinX;
            for (var i = 0; i < textInfo.characterCount; i++)
            {
                var characterInfo = textInfo.characterInfo[i];
                if (!characterInfo.isVisible) continue;

                var meshInfo = textInfo.meshInfo[characterInfo.materialReferenceIndex];
                var vertices = meshInfo.vertices;
                var colors = meshInfo.colors32;
                var vertexIndex = characterInfo.vertexIndex;
                for (var j = 0; j < 4; j++)
                {
                    var rotatedX = GetRotatedPosition(vertices[vertexIndex + j], center, cos, sin).x;
                    var gradientPosition = rotatedWidth > Mathf.Epsilon ? Mathf.Clamp01((rotatedX - rotatedMinX) / rotatedWidth) : 0.5f;
                    var gradientColor = textGradient.Evaluate(gradientPosition);
                    gradientColor.a *= colors[vertexIndex + j].a / 255f;
                    colors[vertexIndex + j] = gradientColor;
                }
            }
        }

        private static Vector2 GetRotatedPosition(Vector3 position, Vector2 center, float cos, float sin)
        {
            var offset = new Vector2(position.x - center.x, position.y - center.y);
            return new Vector2(offset.x * cos - offset.y * sin, offset.x * sin + offset.y * cos) + center;
        }
    }
}