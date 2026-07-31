using UnityEngine;
using UnityEngine.EventSystems;

namespace PowerCellStudio
{
    public class AspectRatioLayout : UIBehaviour
    {
        public enum AdaptDirection
        {
            Horizontal,
            Vertical
        }

        public AdaptDirection adaptDirection = AdaptDirection.Horizontal;

        public float minRatio = 0.6f;
        public float maxRatio = 2f;

        private Vector2 _initScale;
        private RectTransform _rectTransform;
        private Vector2 _lastDisplaySize;

        protected override void Awake()
        {
            base.Awake();
            _rectTransform = transform as RectTransform;
            _initScale = _rectTransform ? (Vector2)_rectTransform.localScale : Vector2.one;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ApplyScale(true);
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            ApplyScale(false);
        }

        /// <summary>
        /// 手动重新计算当前 Canvas 的显示比例。
        /// </summary>
        public void ForceUpdate()
        {
            ApplyScale(true);
        }

        private void ApplyScale(bool force)
        {
            if (!_rectTransform)
            {
                return;
            }

            var displaySize = GetDisplaySize();
            if (displaySize.x <= 0f || displaySize.y <= 0f)
            {
                return;
            }

            if (!force && displaySize == _lastDisplaySize)
            {
                return;
            }

            _lastDisplaySize = displaySize;

            var defaultUISize = ConstSetting.DefaultUISize;
            if (defaultUISize.x <= 0f || defaultUISize.y <= 0f)
            {
                return;
            }

            var ratio = displaySize.x / displaySize.y;
            var defaultRatio = defaultUISize.x / defaultUISize.y;
            var lowerRatio = Mathf.Min(minRatio, maxRatio);
            var upperRatio = Mathf.Max(minRatio, maxRatio);
            var targetRatio = Mathf.Clamp(ratio, lowerRatio, upperRatio);

            // 以 DefaultUISize 为设计比例，在允许范围内按当前比例缩放。
            var scaleFactor = adaptDirection == AdaptDirection.Horizontal
                ? targetRatio / defaultRatio
                : defaultRatio / targetRatio;

            _rectTransform.localScale = new Vector3(
                _initScale.x * scaleFactor,
                _initScale.y * scaleFactor,
                _rectTransform.localScale.z);
        }

        private Vector2 GetDisplaySize()
        {
            if (Screen.width > 0 && Screen.height > 0)
            {
                return new Vector2(Screen.width, Screen.height);
            }

            return Vector2.zero;
        }
    }
}