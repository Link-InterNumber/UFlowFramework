using UnityEngine;
using UnityEngine.EventSystems;

namespace PowerCellStudio
{
    /// <summary>
    /// 根据当前显示区域与默认 UI 尺寸的宽高比调整自身的 localScale。
    /// Adjusts its localScale according to the aspect ratio between the current display area and the default UI size.
    /// </summary>
    public class AspectRatioScaler : UIBehaviour
    {
        /// <summary>
        /// 指定使用宽度或高度作为比例适配基准。
        /// Specifies whether width or height is used as the aspect-ratio adaptation basis.
        /// </summary>
        public enum AdaptMatch
        {
            /// <summary>
            /// 以宽度作为适配基准。
            /// Uses width as the adaptation basis.
            /// </summary>
            Width,

            /// <summary>
            /// 以高度作为适配基准。
            /// Uses height as the adaptation basis.
            /// </summary>
            Height
        }

        /// <summary>
        /// 当前组件使用的比例适配基准。
        /// Gets or sets the aspect-ratio adaptation basis used by this component.
        /// </summary>
        public AdaptMatch adaptMatch = AdaptMatch.Width;

        /// <summary>
        /// 缩放系数允许使用的最小值。
        /// Minimum allowed scale factor.
        /// </summary>
        public float minScale = 0.6f;

        /// <summary>
        /// 缩放系数允许使用的最大值。
        /// Maximum allowed scale factor.
        /// </summary>
        public float maxScale = 2f;

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
        /// 手动重新计算并应用当前显示区域的比例缩放。
        /// Manually recalculates and applies the aspect-ratio scale for the current display area.
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
            var defaultRatio = defaultUISize.x * 1f / defaultUISize.y;

            // 以 DefaultUISize 为设计比例，在允许范围内按当前比例缩放。
            var scaleFactor = adaptMatch == AdaptMatch.Width
                ? ratio / defaultRatio
                : defaultRatio / ratio;
            
            var lowerScale = Mathf.Min(minScale, maxScale);
            var upperScale = Mathf.Max(minScale, maxScale);
            scaleFactor = Mathf.Clamp(scaleFactor, lowerScale, upperScale);
            
            _rectTransform.localScale = new Vector3(
                _initScale.x * scaleFactor,
                _initScale.y * scaleFactor,
                _rectTransform.localScale.z);
        }

        private Vector2 GetDisplaySize()
        {
            if (UICamera.instance)
            {
                return new Vector2(UICamera.instance.currentScreen.x, UICamera.instance.currentScreen.y);
            }
            if (Screen.width > 0 && Screen.height > 0)
            {
                return new Vector2(Screen.width, Screen.height);
            }

            return Vector2.zero;
        }
    }
}