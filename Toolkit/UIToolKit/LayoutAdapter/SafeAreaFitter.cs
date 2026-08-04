using UnityEngine;
using UnityEngine.EventSystems;

namespace PowerCellStudio
{
    /// <summary>
    /// 将 RectTransform 调整到屏幕安全区域内。
    /// Fits the RectTransform within the screen safe area.
    /// </summary>
    public class SafeAreaFitter : UIBehaviour
    {
        /// <summary>
        /// 是否在组件启用或 Canvas 层级变化时自动适配安全区域。
        /// Determines whether to automatically fit the safe area when the component is enabled or the Canvas hierarchy changes.
        /// </summary>
        public bool runInAutomatic = true;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!runInAutomatic) return;
            AdaptToSafeArea();
        }
        
        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            if (!runInAutomatic) return;
            AdaptToSafeArea();
        }

        /// <summary>
        /// 手动重新计算并应用当前屏幕安全区域。
        /// Manually recalculates and applies the current screen safe area.
        /// </summary>
        [TestButton("TestSafeArea")]
        public void ForceUpdate()
        {
            AdaptToSafeArea();
        }

        private void AdaptToSafeArea()
        {
            var root = transform as RectTransform;
            if (!root) return;
            var safeArea = Screen.safeArea;
            var scale = UIManager.PixelScale;
            var offsetMin = new Vector2(
                Mathf.Max(0, safeArea.min.x * scale),
                Mathf.Max(0, safeArea.min.y * scale));
            var offsetMax = safeArea.max * scale - UIManager.ScreenSize;
            // offsetMax.x = Mathf.Min(offsetMax.x);
            // offsetMax.y = Mathf.Min(offsetMax.y);

            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = offsetMin;
            root.offsetMax = offsetMax;
        }

        [TestButton]
        /// <summary>
        /// 输出当前安全区域和 RectTransform 的适配参数。
        /// Logs the current safe area and the RectTransform adaptation parameters.
        /// </summary>
        public void LogSafeArea()
        {
            var safeArea = Screen.safeArea;
            Debug.Log($"Safe Area: {safeArea}");
                        var root = transform as RectTransform;
            if (!root) return;
            Debug.Log($"Root Offset Min: {root.offsetMin}, Offset Max: {root.offsetMax}");
            Debug.Log($"Root Anchor Min: {root.anchorMin}, Anchor Max: {root.anchorMax}");
        }
    }
}