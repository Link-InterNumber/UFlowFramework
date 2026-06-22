using UnityEngine;

namespace PowerCellStudio
{
    public static class Vector3Extension
    {
        /// <summary>
        /// 返回一个新的三维向量，并将 X 分量替换为指定值。
        /// Returns a new Vector3 with the X component replaced by the specified value.
        /// </summary>
        /// <param name="v">原始向量。</param>
        /// <param name="x">新的 X 分量值。</param>
        /// <returns>替换 X 分量后的三维向量。</returns>
        public static Vector3 WithX(this Vector3 v, float x)
        {
            v.x = x;
            return v;
        }

        /// <summary>
        /// 返回一个新的三维向量，并将 Y 分量替换为指定值。
        /// Returns a new Vector3 with the Y component replaced by the specified value.
        /// </summary>
        /// <param name="v">原始向量。</param>
        /// <param name="y">新的 Y 分量值。</param>
        /// <returns>替换 Y 分量后的三维向量。</returns>
        public static Vector3 WithY(this Vector3 v, float y)
        {
            v.y = y;
            return v;
        }

        /// <summary>
        /// 返回一个新的三维向量，并将 Z 分量替换为指定值。
        /// Returns a new Vector3 with the Z component replaced by the specified value.
        /// </summary>
        /// <param name="v">原始向量。</param>
        /// <param name="z">新的 Z 分量值。</param>
        /// <returns>替换 Z 分量后的三维向量。</returns>
        public static Vector3 WithZ(this Vector3 v, float z)
        {
            v.z = z;
            return v;
        }

        /// <summary>
        /// 返回一个新的三维向量，并在 X 分量上增加指定值。
        /// Returns a new Vector3 with the specified value added to the X component.
        /// </summary>
        /// <param name="v">原始向量。</param>
        /// <param name="x">要增加到 X 分量的值。</param>
        /// <returns>增加 X 分量后的三维向量。</returns>
        public static Vector3 AddX(this Vector3 v, float x)
        {
            v.x += x;
            return v;
        }

        /// <summary>
        /// 返回一个新的三维向量，并在 Y 分量上增加指定值。
        /// Returns a new Vector3 with the specified value added to the Y component.
        /// </summary>
        /// <param name="v">原始向量。</param>
        /// <param name="y">要增加到 Y 分量的值。</param>
        /// <returns>增加 Y 分量后的三维向量。</returns>
        public static Vector3 AddY(this Vector3 v, float y)
        {
            v.y += y;
            return v;
        }

        /// <summary>
        /// 返回一个新的三维向量，并在 Z 分量上增加指定值。
        /// Returns a new Vector3 with the specified value added to the Z component.
        /// </summary>
        /// <param name="v">原始向量。</param>
        /// <param name="z">要增加到 Z 分量的值。</param>
        /// <returns>增加 Z 分量后的三维向量。</returns>
        public static Vector3 AddZ(this Vector3 v, float z)
        {
            v.z += z;
            return v;
        }

        /// <summary>
        /// 返回向量在 XZ 平面上的投影，并将 Y 分量设为 0。
        /// Returns the projection of the vector onto the XZ plane with the Y component set to 0.
        /// </summary>
        /// <param name="v">原始向量。</param>
        /// <returns>位于 XZ 平面上的三维向量。</returns>
        public static Vector3 XZ(this Vector3 v)
        {
            return new Vector3(v.x, 0f, v.z);
        }

        /// <summary>
        /// 将三维向量的 X 和 Y 分量转换为二维向量。
        /// Converts the X and Y components of a Vector3 to a Vector2.
        /// </summary>
        /// <param name="v">原始三维向量。</param>
        /// <returns>由 X 和 Y 分量组成的二维向量。</returns>
        public static Vector2 ToVector2XY(this Vector3 v)
        {
            return new Vector2(v.x, v.y);
        }

        /// <summary>
        /// 将三维向量的 X 和 Z 分量转换为二维向量。
        /// Converts the X and Z components of a Vector3 to a Vector2.
        /// </summary>
        /// <param name="v">原始三维向量。</param>
        /// <returns>由 X 和 Z 分量组成的二维向量。</returns>
        public static Vector2 ToVector2XZ(this Vector3 v)
        {
            return new Vector2(v.x, v.z);
        }

        /// <summary>
        /// 将向量长度限制在指定最大长度内。
        /// Clamps the vector magnitude to the specified maximum length.
        /// </summary>
        /// <param name="v">要限制长度的向量。</param>
        /// <param name="maxLength">最大长度。</param>
        /// <returns>长度被限制后的三维向量。</returns>
        public static Vector3 ClampMagnitude(this Vector3 v, float maxLength)
        {
            return Vector3.ClampMagnitude(v, maxLength);
        }

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
        public static Vector3 RotationAngleByZAxis(this Vector3 v3, float angle)
        {
            return Quaternion.AngleAxis(angle, Vector3.forward) * v3;
        }

        /// <summary>
        /// 将向量绕二维平面的Y轴旋转指定角度。
        /// Rotates the vector around the Y-axis by a specified angle in 2D.
        /// </summary>
        /// <param name="v3">要旋转的向量。</param>
        /// <param name="angle">旋转角度（度）。</param>
        /// <returns>旋转后的向量。</returns>
        public static Vector3 RotationAngleByYAxis(this Vector3 v3, float angle)
        {
            return Quaternion.AngleAxis(angle, Vector3.up) * v3;
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
        /// 检查当前位置是否在目标位置的指定范围内。
        /// Checks if the current position is within a specified range from the target position.
        /// </summary>
        /// <param name="v3">当前位置。</param>
        /// <param name="target">目标位置。</param>
        /// <param name="range">范围的半径。</param>
        /// <returns>如果在范围内则为真，否则为假。</returns>
        public static bool IsInRange(this Vector3 v3, Vector3 target, float range)
        {
            if (range < 0f) return false;
            float dx = Mathf.Abs(v3.x - target.x);
            if (dx > range) return false;
            float dy = Mathf.Abs(v3.y - target.y);
            if (dy > range) return false;
            float dz = Mathf.Abs(v3.z - target.z);
            if (dz > range) return false;

            if (dx + dy + dz <= range) return true;

            var distance = dx * dx + dy * dy + dz * dz;
            return distance <= (range * range);
        }
        
        /// <summary>
        /// 检查当前位置是否在目标位置的指定二维范围内。
        /// Checks if the current position is within a specified 2D range from the target position.
        /// </summary>
        /// <param name="v3">当前位置。</param>
        /// <param name="target">目标位置。</param>
        /// <param name="range">范围的半径。</param>
        /// <returns>如果在范围内则为真，否则为假。</returns>
        public static bool IsInRange2D(this Vector3 v3, Vector3 target, float range)
        {
            if (range < 0f) return false;
            float dx = Mathf.Abs(v3.x - target.x);
            if (dx > range) return false;
            float dy = Mathf.Abs(v3.y - target.y);
            if (dy > range) return false;

            if (dx + dy <= range) return true;

            var distance = dx * dx + dy * dy;
            return distance <= (range * range);
        }
    }
    
    public static class Vector2Extension
    {
        /// <summary>
        /// 返回一个新的二维向量，并将 X 分量替换为指定值。
        /// Returns a new Vector2 with the X component replaced by the specified value.
        /// </summary>
        /// <param name="v">原始向量。</param>
        /// <param name="x">新的 X 分量值。</param>
        /// <returns>替换 X 分量后的二维向量。</returns>
        public static Vector2 WithX(this Vector2 v, float x)
        {
            v.x = x;
            return v;
        }

        /// <summary>
        /// 返回一个新的二维向量，并将 Y 分量替换为指定值。
        /// Returns a new Vector2 with the Y component replaced by the specified value.
        /// </summary>
        /// <param name="v">原始向量。</param>
        /// <param name="y">新的 Y 分量值。</param>
        /// <returns>替换 Y 分量后的二维向量。</returns>
        public static Vector2 WithY(this Vector2 v, float y)
        {
            v.y = y;
            return v;
        }

        /// <summary>
        /// 将二维向量作为 X、Y 分量转换为三维向量，并使用指定的 Z 分量。
        /// Converts a Vector2 to a Vector3 using the vector as X and Y components and the specified Z component.
        /// </summary>
        /// <param name="v">原始二维向量。</param>
        /// <param name="z">三维向量的 Z 分量。</param>
        /// <returns>转换后的三维向量。</returns>
        public static Vector3 ToVector3XY(this Vector2 v, float z = 0f)
        {
            return new Vector3(v.x, v.y, z);
        }

        /// <summary>
        /// 将二维向量作为 X、Z 分量转换为三维向量，并使用指定的 Y 分量。
        /// Converts a Vector2 to a Vector3 using the vector as X and Z components and the specified Y component.
        /// </summary>
        /// <param name="v">原始二维向量。</param>
        /// <param name="y">三维向量的 Y 分量。</param>
        /// <returns>转换后的三维向量。</returns>
        public static Vector3 ToVector3XZ(this Vector2 v, float y = 0f)
        {
            return new Vector3(v.x, y, v.y);
        }

        /// <summary>
        /// 将角度转换为二维方向向量。
        /// Converts an angle in degrees to a 2D direction vector.
        /// </summary>
        /// <param name="angle">角度（度）。</param>
        /// <returns>由角度表示的二维方向向量。</returns>
        public static Vector2 AngleToDirection(float angle)
        {
            float rad = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        /// <summary>
        /// 将二维方向向量转换为角度。
        /// Converts a 2D direction vector to an angle in degrees.
        /// </summary>
        /// <param name="direction">二维方向向量。</param>
        /// <returns>方向向量对应的角度（度）。</returns>
        public static float ToAngle(this Vector2 direction)
        {
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }

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
        /// 计算两个二维位置之间的曼哈顿距离。
        /// Computes the Manhattan distance between two 2D position.
        /// </summary>
        /// <param name="v2">原始位置。</param>
        /// <param name="target">目标位置。</param>
        /// <returns>曼哈顿距离。</returns>
        public static float ManhattanDistance(this Vector2 v2, Vector2 target)
        {
            return Mathf.Abs(v2.x - target.x) + Mathf.Abs(v2.y - target.y);
        }
        
        /// <summary>
        /// 检查当前位置是否在目标位置的指定范围内。
        /// Checks if the current vector is within a specified range from the target position.
        /// </summary>
        /// <param name="v2">当前位置。</param>
        /// <param name="target">目标位置。</param>
        /// <param name="range">范围的半径。</param>
        /// <returns>如果在范围内则为真，否则为假。</returns>
        public static bool IsInRange(this Vector2 v2, Vector2 target, float range)
        {
            if (range < 0f) return false;

            float dx = Mathf.Abs(v2.x - target.x);
            if (dx > range) return false;
            float dy = Mathf.Abs(v2.y - target.y);
            if (dy > range) return false;

            if (dx + dy <= range) return true;

            var distance = dx * dx + dy * dy;
            return distance <= (range * range);
        }
    }
}