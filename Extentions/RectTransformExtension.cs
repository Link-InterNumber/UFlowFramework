using UnityEngine;

namespace PowerCellStudio
{
    public static class RectTransformExtension
    {
        /// <summary>
        /// 设置锚点和位置为拉伸填充父物体。
        /// Set anchors and position to stretch and fill the parent.
        /// </summary>
        /// <param name="rectTransform">目标RectTransform / Target RectTransform.</param>
        public static void Adapt2Parent(this RectTransform rectTransform)
        {
            if(!rectTransform || rectTransform.parent == null) return;
            rectTransform.localPosition = Vector3.zero;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 设置RectTransform的宽高。
        /// Set the width and height of the RectTransform.
        /// </summary>
        /// <param name="rectTransform">目标RectTransform / Target RectTransform.</param>
        /// <param name="width">宽度 / Width.</param>
        /// <param name="height">高度 / Height.</param>
        public static void SetSize(this RectTransform rectTransform, float width, float height)
        {
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }
    }
}
