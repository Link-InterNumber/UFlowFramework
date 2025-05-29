using UnityEngine;

namespace PowerCellStudio
{
    public static class BoundsExtension
    {
        /// <summary>
        /// 判断两个Bounds是否相交 / Determines whether two Bounds intersect.
        /// </summary>
        /// <param name="a">第一个Bounds / First Bounds</param>
        /// <param name="b">第二个Bounds / Second Bounds</param>
        /// <returns>如果相交返回true，否则返回false / Returns true if they intersect, otherwise false.</returns>
        public static bool IntersectsBounds(this Bounds a, Bounds b)
        {
            return a.Intersects(b);
        }

        public static bool TryGetIntersection(this Bounds self, Bounds other, out Bounds intersection)
        {
            intersection = new Bounds();
            if (!self.Intersects(other)) return false;
            intersection.SetMinMax(
                new Vector3(Mathf.Max(self.min.x, other.min.x), Mathf.Max(self.min.y, other.min.y),Mathf.Max(self.min.z, other.min.z)),
                new Vector3(Mathf.Min(self.max.x, other.max.x), Mathf.Min(self.max.y, other.max.y), Mathf.Min(self.max.z, other.max.z)));
            return true;
        }

        /// <summary>
        /// 判断一个Bounds是否包含另一个Bounds / Determines whether one Bounds contains another Bounds.
        /// </summary>
        /// <param name="a">外部Bounds / Outer Bounds</param>
        /// <param name="b">内部Bounds / Inner Bounds</param>
        /// <returns>如果a包含b返回true，否则返回false / Returns true if a contains b, otherwise false.</returns>
        public static bool ContainsBounds(this Bounds a, Bounds b)
        {
            return a.Contains(b.min) && a.Contains(b.max);
        }

        /// <summary>
        /// 扩展Bounds以包含多个点 / Expands the Bounds to encapsulate multiple points.
        /// </summary>
        /// <param name="bounds">要扩展的Bounds / The Bounds to expand</param>
        /// <param name="points">要包含的点 / Points to encapsulate</param>
        public static void EncapsulatePoints(this Bounds bounds, params Vector3[] points)
        {
            foreach (var point in points)
            {
                bounds.Encapsulate(point);
            }
        }

        /// <summary>
        /// 获取Bounds的八个角点 / Gets the eight corner points of the Bounds.
        /// </summary>
        /// <param name="bounds">目标Bounds / The target Bounds</param>
        /// <returns>八个角点的数组 / Array of eight corner points</returns>
        public static Vector3[] GetCorners(this Bounds bounds)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            return new Vector3[]
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, max.y, max.z)
            };
        }

        /// <summary>
        /// 按比例缩放Bounds / Scales the Bounds by a given factor.
        /// </summary>
        /// <param name="bounds">要缩放的Bounds / The Bounds to scale</param>
        /// <param name="scale">缩放比例 / Scale factor</param>
        /// <returns>缩放后的Bounds / Scaled Bounds</returns>
        public static Bounds Scale(this Bounds bounds, Vector3 scale)
        {
            return new Bounds(bounds.center, Vector3.Scale(bounds.size, scale));
        }


    }
}