using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PowerCellStudio
{
    /// <summary>
    /// 根据默认 UI 尺寸与当前屏幕尺寸调整 RectTransform 的布局尺寸。
    /// Adjusts the RectTransform layout size according to the default UI size and current screen size.
    /// </summary>
    public class AspectRatioLayout : UIBehaviour
    {
        [Flags]
        /// <summary>
        /// 指定需要跟随屏幕尺寸适配的方向，支持按位组合。
        /// Specifies the directions that follow the screen size; values can be combined as flags.
        /// </summary>
        public enum AdaptMatch
        {
            /// <summary>
            /// 适配水平方向。
            /// Adapts the horizontal direction.
            /// </summary>
            Width = 1 << 0,

            /// <summary>
            /// 适配垂直方向。
            /// Adapts the vertical direction.
            /// </summary>
            Height = 1 << 1
        }

        /// <summary>
        /// 指定需要适配的方向，可同时包含 Width 和 Height。
        /// Specifies the directions to adapt; can include both Width and Height.
        /// </summary>
        public AdaptMatch match = AdaptMatch.Width | AdaptMatch.Height;

        private Vector2 _initSize;
        private RectTransform _rectTransform;
        protected override void Awake()
        {
            base.Awake();
            _rectTransform = transform as RectTransform;
            _initSize = _rectTransform?.sizeDelta ?? Vector2.zero;
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();
            AdaptToAspectRatio();
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            AdaptToAspectRatio();
        }

        private void AdaptToAspectRatio()
        {
            if (!_rectTransform) return;
            var screenSize = UIManager.ScreenSize;
            var defaultScreenSize = ConstSetting.DefaultUISize;

            var radio = new Vector2(
                _initSize.x / defaultScreenSize.x,
                _initSize.y / defaultScreenSize.y
            );
            var matchToWidth = (match & AdaptMatch.Width) != 0;
            var matchToHeight = (match & AdaptMatch.Height) != 0;
            
            var adaptedSize = new Vector2(
                matchToWidth ? screenSize.x * radio.x : _initSize.x,
                matchToHeight ? screenSize.y * radio.y : _initSize.y
            );
            _rectTransform.sizeDelta = adaptedSize;
        }
    }
}