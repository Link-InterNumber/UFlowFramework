using System.Linq;
using UnityEngine;

namespace PowerCellStudio
{
    /// <summary>
    /// 提供 RectTransform 相关的 UI 布局、尺寸、位置和检测扩展方法。
    /// Provides RectTransform extension methods for UI layout, size, position, and hit testing.
    /// </summary>
    public static class RectTransformExtension
    {
        /// <summary>
        /// 设置锚点和位置为拉伸填充父物体。
        /// Sets anchors and offsets to stretch and fill the parent.
        /// </summary>
        /// <param name="rectTransform">目标 RectTransform。Target RectTransform.</param>
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
        /// 设置 RectTransform 的宽度和高度。
        /// Sets the width and height of the RectTransform.
        /// </summary>
        /// <param name="rectTransform">目标 RectTransform。Target RectTransform.</param>
        /// <param name="width">宽度。Width.</param>
        /// <param name="height">高度。Height.</param>
        public static void SetSize(this RectTransform rectTransform, float width, float height)
        {
            if (!rectTransform) return;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        /// <summary>
        /// 设置 RectTransform 的宽度。
        /// Sets the width of the RectTransform.
        /// </summary>
        /// <param name="rectTransform">目标 RectTransform。Target RectTransform.</param>
        /// <param name="width">宽度。Width.</param>
        public static void SetWidth(this RectTransform rectTransform, float width)
        {
            if (!rectTransform) return;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }

        /// <summary>
        /// 设置 RectTransform 的高度。
        /// Sets the height of the RectTransform.
        /// </summary>
        /// <param name="rectTransform">目标 RectTransform。Target RectTransform.</param>
        /// <param name="height">高度。Height.</param>
        public static void SetHeight(this RectTransform rectTransform, float height)
        {
            if (!rectTransform) return;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        /// <summary>
        /// 获取 RectTransform 当前矩形的宽度。
        /// Gets the width of the current RectTransform rectangle.
        /// </summary>
        /// <param name="rectTransform">目标 RectTransform。Target RectTransform.</param>
        /// <returns>矩形宽度；如果目标无效则返回 0。Rectangle width, or 0 if the target is invalid.</returns>
        public static float GetWidth(this RectTransform rectTransform)
        {
            return rectTransform ? rectTransform.rect.width : 0f;
        }

        /// <summary>
        /// 获取 RectTransform 当前矩形的高度。
        /// Gets the height of the current RectTransform rectangle.
        /// </summary>
        /// <param name="rectTransform">目标 RectTransform。Target RectTransform.</param>
        /// <returns>矩形高度；如果目标无效则返回 0。Rectangle height, or 0 if the target is invalid.</returns>
        public static float GetHeight(this RectTransform rectTransform)
        {
            return rectTransform ? rectTransform.rect.height : 0f;
        }

        /// <summary>
        /// 获取 RectTransform 在世界空间中的包围盒。
        /// Gets the world-space bounds of the RectTransform.
        /// </summary>
        /// <param name="rt">目标 RectTransform。Target RectTransform.</param>
        /// <returns>世界空间包围盒；如果目标无效则返回默认包围盒。World-space bounds, or default bounds if the target is invalid.</returns>
        public static Bounds GetWorldBounds(this RectTransform rt)
        {
            if (!rt) return new Bounds();
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            var center = new Vector3(corners.Sum(o => o.x) / 4f, corners.Sum(o => o.y) / 4f);
            var size = new Vector3(corners.Max(o => o.x) - corners.Min(o => o.x), corners.Max(o => o.y) - corners.Min(o => o.y));
            var bounds = new Bounds(center, size);

            return bounds;
        }

        /// <summary>
        /// 获取 RectTransform 在世界空间中的中心点。
        /// Gets the world-space center point of the RectTransform.
        /// </summary>
        /// <param name="rt">目标 RectTransform。Target RectTransform.</param>
        /// <returns>世界空间中心点。World-space center point.</returns>
        public static Vector3 GetWorldCenter(this RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            var center = new Vector3(corners.Sum(o => o.x) / 4f, corners.Sum(o => o.y) / 4f);
            return center;
        }

        /// <summary>
        /// 设置 RectTransform 锚定位置的 X 分量。
        /// Sets the X component of the RectTransform anchored position.
        /// </summary>
        /// <param name="rectTransform">目标 RectTransform。Target RectTransform.</param>
        /// <param name="x">新的锚定位置 X 分量值。New anchored position X component.</param>
        public static void SetAnchoredPositionX(this RectTransform rectTransform, float x)
        {
            if (!rectTransform) return;

            var position = rectTransform.anchoredPosition;
            position.x = x;
            rectTransform.anchoredPosition = position;
        }

        /// <summary>
        /// 设置 RectTransform 锚定位置的 Y 分量。
        /// Sets the Y component of the RectTransform anchored position.
        /// </summary>
        /// <param name="rectTransform">目标 RectTransform。Target RectTransform.</param>
        /// <param name="y">新的锚定位置 Y 分量值。New anchored position Y component.</param>
        public static void SetAnchoredPositionY(this RectTransform rectTransform, float y)
        {
            if (!rectTransform) return;

            var position = rectTransform.anchoredPosition;
            position.y = y;
            rectTransform.anchoredPosition = position;
        }

        /// <summary>
        /// 判断两个 RectTransform 的世界空间包围盒是否重叠。
        /// Determines whether the world-space bounds of two RectTransforms overlap.
        /// </summary>
        /// <param name="rt">当前 RectTransform。Current RectTransform.</param>
        /// <param name="other">要检测的另一个 RectTransform。Other RectTransform to test.</param>
        /// <returns>如果两个包围盒重叠则返回 true，否则返回 false。Returns true if the bounds overlap; otherwise, false.</returns>
        public static bool IsOverlap(this RectTransform rt, RectTransform other)
        {
            if (!rt || !other) return false;
            
            return rt.GetWorldBounds().Intersects(other.GetWorldBounds());
        }

        /// <summary>
        /// 判断屏幕坐标点是否位于 RectTransform 矩形范围内。
        /// Determines whether a screen point is inside the RectTransform rectangle.
        /// </summary>
        /// <param name="rectTransform">目标 RectTransform。Target RectTransform.</param>
        /// <param name="screenPoint">屏幕坐标点。Screen point.</param>
        /// <param name="camera">用于坐标检测的相机；Screen Space Overlay 可传 null。Camera used for the test; pass null for Screen Space Overlay.</param>
        /// <returns>如果屏幕点在矩形范围内则返回 true，否则返回 false。Returns true if the screen point is inside the rectangle; otherwise, false.</returns>
        public static bool ContainsScreenPoint(this RectTransform rectTransform, Vector2 screenPoint, Camera camera = null)
        {
            if (!rectTransform) return false;

            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, camera);
        }

        /// <summary>
        /// 表示 UI 节点可对齐到父节点的常用锚点位置。
        /// Represents common anchor positions for aligning a UI element to its parent.
        /// </summary>
        public enum AnchorPosition
        {
            /// <summary>
            /// 左上角。
            /// Top-left corner.
            /// </summary>
            TopLeft,
            /// <summary>
            /// 顶部居中。
            /// Top center.
            /// </summary>
            Top,
            /// <summary>
            /// 右上角。
            /// Top-right corner.
            /// </summary>
            TopRight,
            /// <summary>
            /// 左侧居中。
            /// Left center.
            /// </summary>
            Left,
            /// <summary>
            /// 中心位置。
            /// Center position.
            /// </summary>
            Center,
            /// <summary>
            /// 右侧居中。
            /// Right center.
            /// </summary>
            Right,
            /// <summary>
            /// 左下角。
            /// Bottom-left corner.
            /// </summary>
            BottomLeft,
            /// <summary>
            /// 底部居中。
            /// Bottom center.
            /// </summary>
            Bottom,
            /// <summary>
            /// 右下角。
            /// Bottom-right corner.
            /// </summary>
            BottomRight
        }

        /// <summary>
        /// 将 UI 节点移动到父节点的指定锚点位置。
        /// Moves the UI element to the specified anchor position of its parent.
        /// </summary>
        /// <param name="rectTransform">要移动的 UI 节点。UI element to move.</param>
        /// <param name="position">目标锚点位置。Target anchor position.</param>
        /// <param name="offset">锚定位置偏移量。Anchored position offset.</param>
        public static void MoveTo(this RectTransform rectTransform, AnchorPosition position, Vector2 offset = default)
        {
            if (rectTransform == null || rectTransform.parent == null)
                return;

            Vector2 anchor; //, pivot;

            switch (position)
            {
                case AnchorPosition.TopLeft:
                    anchor = new Vector2(0, 1);
                    // pivot = new Vector2(0, 1);
                    break;
                case AnchorPosition.Top:
                    anchor = new Vector2(0.5f, 1);
                    // pivot = new Vector2(0.5f, 1);
                    break;
                case AnchorPosition.TopRight:
                    anchor = new Vector2(1, 1);
                    // pivot = new Vector2(1, 1);
                    break;
                case AnchorPosition.Left:
                    anchor = new Vector2(0, 0.5f);
                    // pivot = new Vector2(0, 0.5f);
                    break;
                case AnchorPosition.Center:
                    anchor = new Vector2(0.5f, 0.5f);
                    // pivot = new Vector2(0.5f, 0.5f);
                    break;
                case AnchorPosition.Right:
                    anchor = new Vector2(1, 0.5f);
                    // pivot = new Vector2(1, 0.5f);
                    break;
                case AnchorPosition.BottomLeft:
                    anchor = new Vector2(0, 0);
                    // pivot = new Vector2(0, 0);
                    break;
                case AnchorPosition.Bottom:
                    anchor = new Vector2(0.5f, 0);
                    // pivot = new Vector2(0.5f, 0);
                    break;
                case AnchorPosition.BottomRight:
                    anchor = new Vector2(1, 0);
                    // pivot = new Vector2(1, 0);
                    break;
                default:
                    anchor = new Vector2(0.5f, 0.5f);
                    // pivot = new Vector2(0.5f, 0.5f);
                    break;
            }

            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            // rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = offset;
        }
    }
}
