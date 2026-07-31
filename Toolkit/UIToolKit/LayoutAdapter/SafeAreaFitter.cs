using UnityEngine;
using UnityEngine.EventSystems;

namespace PowerCellStudio
{
    public class SafeAreaFitter : UIBehaviour
    {
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
        public void TestSafeArea()
        {
            AdaptToSafeArea();
        }

        [TestButton]
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