using System.Linq;
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

        public static Bounds GetWorldBounds(this RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            var center = new Vector3(corners.Sum(o => o.x) / 4f, corners.Sum(o => o.y) / 4f);
            var size = new Vector3(corners.Max(o => o.x) - corners.Min(o => o.x), corners.Max(o => o.y) - corners.Min(o => o.y));
            var bounds = new Bounds(center, size);

            return bounds;
        }

        public static Vector3 GetWorldCenter(this RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            var center = new Vector3(corners.Sum(o => o.x) / 4f, corners.Sum(o => o.y) / 4f);
            return center;
        }

        public static bool IsOverlap(this RectTransform rt, RectTransform other)
        {
            if (!rt || !other) return false;
            
            return rt.GetWorldBounds().Intersects(other.GetWorldBounds());
        }
    }
}
