using UnityEngine;

namespace PowerCellStudio
{
    public static class Vector3Extension
    {
        /// <summary>
        /// 如果向量为零则返回默认值，否则返回归一化的向量。
        /// Returns a normalized vector or a default value if the vector is zero.
        /// </summary>
        /// <param name="v3">要检查的向量。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <returns>归一化向量或默认值。</returns>
        public static Vector3 NormalizedOrDefault(this Vector3 v3, Vector3 defaultValue)
        {
            return v3.Equals(Vector3.zero) ? defaultValue : v3.normalized;
        }

        /// <summary>
        /// 将向量绕二维平面的Z轴旋转指定角度。
        /// Rotates the vector around the Z-axis by a specified angle in 2D.
        /// </summary>
        /// <param name="v3">要旋转的向量。</param>
        /// <param name="angle">旋转角度（度）。</param>
        /// <returns>旋转后的向量。</returns>
        public static Vector3 RotationAngle2D(this Vector3 v3, float angle)
        {
            return Quaternion.AngleAxis(angle, Vector3.forward) * v3;
        }
        
        /// <summary>
        /// 计算两个三维向量之间的曼哈顿距离。
        /// Computes the Manhattan distance between two 3D vectors.
        /// </summary>
        /// <param name="v3">原始向量。</param>
        /// <param name="target">目标向量。</param>
        /// <returns>曼哈顿距离。</returns>
        public static float ManhattanDistance(this Vector3 v3, Vector3 target)
        {
            return Mathf.Abs(v3.x - target.x) + Mathf.Abs(v3.y - target.y) + Mathf.Abs(v3.z - target.z);
        }
        
        /// <summary>
        /// 检查当前向量是否在目标向量的指定范围内。
        /// Checks if the current vector is within a specified range from the target vector.
        /// </summary>
        /// <param name="v3">当前向量。</param>
        /// <param name="target">目标向量。</param>
        /// <param name="range">范围的半径。</param>
        /// <returns>如果在范围内则为真，否则为假。</returns>
        public static bool IsInRange(this Vector3 v3, Vector3 target, float range)
        {
            var distance = Vector3.SqrMagnitude(target - v3);
            return distance <= (range * range);
        }
        
        /// <summary>
        /// 检查当前向量是否在目标向量的指定二维范围内。
        /// Checks if the current vector is within a specified 2D range from the target vector.
        /// </summary>
        /// <param name="v3">当前向量。</param>
        /// <param name="target">目标向量。</param>
        /// <param name="range">范围的半径。</param>
        /// <returns>如果在范围内则为真，否则为假。</returns>
        public static bool IsInRange2D(this Vector3 v3, Vector3 target, float range)
        {
            var v2 = new Vector2(v3.x, v3.y);
            var target2D = new Vector2(target.x, target.y);
            var manhattanDistance = v2.ManhattanDistance(target2D);
            if (manhattanDistance > range) return false;
            var distance = Vector2.SqrMagnitude(target2D - v2);
            return distance <= (range * range);
        }
    }
    
    public static class Vector2Extension
    {
        /// <summary>
        /// 如果向量为零则返回默认值，否则返回归一化的向量。
        /// Returns a normalized vector or a default value if the vector is zero.
        /// </summary>
        /// <param name="v2">要检查的向量。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <returns>归一化向量或默认值。</returns>
        public static Vector2 NormalizedOrDefault(this Vector2 v2, Vector2 defaultValue)
        {
            return v2.Equals(Vector2.zero) ? defaultValue : v2.normalized;
        }

        /// <summary>
        /// 将向量绕二维平面的Z轴旋转指定角度。
        /// Rotates the vector around the Z-axis by a specified angle in 2D.
        /// </summary>
        /// <param name="v2">要旋转的向量。</param>
        /// <param name="angle">旋转角度（度）。</param>
        /// <returns>旋转后的向量。</returns>
        public static Vector2 RotationAngle2D(this Vector2 v2, float angle)
        {
            return Quaternion.AngleAxis(angle, Vector3.forward) * v2;
        }
        
        /// <summary>
        /// 计算两个二维向量之间的曼哈顿距离。
        /// Computes the Manhattan distance between two 2D vectors.
        /// </summary>
        /// <param name="v2">原始向量。</param>
        /// <param name="target">目标向量。</param>
        /// <returns>曼哈顿距离。</returns>
        public static float ManhattanDistance(this Vector2 v2, Vector2 target)
        {
            return Mathf.Abs(v2.x - target.x) + Mathf.Abs(v2.y - target.y);
        }
        
        /// <summary>
        /// 检查当前向量是否在目标向量的指定范围内。
        /// Checks if the current vector is within a specified range from the target vector.
        /// </summary>
        /// <param name="v2">当前向量。</param>
        /// <param name="target">目标向量。</param>
        /// <param name="range">范围的半径。</param>
        /// <returns>如果在范围内则为真，否则为假。</returns>
        public static bool IsInRange(this Vector2 v2, Vector2 target, float range)
        {
            var manhattanDistance = v2.ManhattanDistance(target);
            if (manhattanDistance > range) return false;
            var distance = Vector2.SqrMagnitude(target - v2);
            return distance <= (range * range);
        }
    }
}