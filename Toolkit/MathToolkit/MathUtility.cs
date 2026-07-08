using UnityEngine;

namespace PowerCellStudio
{
    public static class MathUtility
    {
        /// <summary>
        /// 万分比整数基数
        /// </summary>
        public static readonly int MillionInt = 10000;
        
        /// <summary>
        /// 万分比长整数基数
        /// </summary>
        public static readonly long MillionLong = 10000;

        public static bool TrueMillion(int v)
        {
            return Randomizer.Default.True(v, MillionInt);
        }

        public static bool TrueMillion(long v)
        {
            return Randomizer.Default.True(v, MillionLong);
        }
        
        public static float Remap(float val, float start, float end, float toStart, float toEnd)
        {
            if (end.Equals(start)) return toStart;
            return Mathf.Lerp(toStart, toEnd, (val - start) / (end - start));
        }

        /// <summary>
        /// 带钳制的映射，将输入区间映射到输出区间。
        /// </summary>
        public static float RemapClamped(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            var t = Mathf.Clamp01(InverseLerpSafe(fromMin, fromMax, value));
            return Mathf.Lerp(toMin, toMax, t);
        }

        [System.CLSCompliant(false)]
        public static Oval2D CreateOval2D(float width, float height, Vector2 position, float rotateClockwise = 0)
        {
            width = Mathf.Abs(width);
            height = Mathf.Abs(height);
            var oval = new Oval2D(width, height);
            oval.offset = position;
            oval.rotateClockwise = rotateClockwise;
            return oval;
        }

        public static ProceduralCurve CreateProceduralCurve(float initValue, float frequency, float damping = 1f, float response = 0f)
        {
            var curve = new ProceduralCurve
            {
                frequency = Mathf.Max(0.1f, frequency),
                damping = damping,
                response = response
            };
            curve.InitCal(initValue);
            return curve;
        }

        [System.CLSCompliant(false)]
        public static Parabola2D CreateParabola2D(Vector2 startPos, Vector2 endPos, float heightRelateTo2Point)
        {
            if (startPos.Equals(endPos))
            {
                LinkLogger.LogError("Parabola2D start position can not be same to end position!");
                return null;
            }
            return new Parabola2D(startPos, endPos, heightRelateTo2Point);
        }

        public static float Smoothstep(float from, float to, float t)
        {
            if (to.Equals(from)) return t <= from ? 0f : 1f;
            t = Mathf.Clamp01((t - from) / (to - from));
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// 安全反插值，避免 from 与 to 相等导致除零。
        /// </summary>
        public static float InverseLerpSafe(float from, float to, float value, float defaultValue = 0f)
        {
            if (Mathf.Approximately(from, to)) return defaultValue;
            return (value - from) / (to - from);
        }

        /// <summary>
        /// 使用指定误差比较两个浮点数是否近似相等。
        /// </summary>
        public static bool Approximately(float a, float b, float epsilon = 0.00001f)
        {
            return Mathf.Abs(a - b) <= Mathf.Abs(epsilon);
        }

        /// <summary>
        /// 判断浮点值是否近似为 0。
        /// </summary>
        public static bool IsZero(float value, float epsilon = 0.00001f)
        {
            return Mathf.Abs(value) <= Mathf.Abs(epsilon);
        }

        /// <summary>
        /// 将角度归一化到 [0, 360)。
        /// </summary>
        public static float NormalizeAngle360(float angle)
        {
            return Mathf.Repeat(angle, 360f);
        }

        /// <summary>
        /// 将角度归一化到 (-180, 180]。
        /// </summary>
        public static float NormalizeAngle180(float angle)
        {
            angle = NormalizeAngle360(angle);
            if (angle > 180f) angle -= 360f;
            return angle;
        }

        /// <summary>
        /// 将整数值环绕到 [minInclusive, maxExclusive)。
        /// </summary>
        public static int Wrap(int value, int minInclusive, int maxExclusive)
        {
            var length = maxExclusive - minInclusive;
            if (length <= 0) return minInclusive;
            var wrapped = (value - minInclusive) % length;
            if (wrapped < 0) wrapped += length;
            return minInclusive + wrapped;
        }

        /// <summary>
        /// 将浮点值环绕到 [minInclusive, maxExclusive)。
        /// </summary>
        public static float Wrap(float value, float minInclusive, float maxExclusive)
        {
            var length = maxExclusive - minInclusive;
            if (length <= 0f) return minInclusive;
            return minInclusive + Mathf.Repeat(value - minInclusive, length);
        }

        /// <summary>
        /// 二维点平方距离，避免不必要的开方。
        /// </summary>
        [System.CLSCompliant(false)]
        public static float SqrDistance(Vector2 a, Vector2 b)
        {
            return (a - b).sqrMagnitude;
        }

        /// <summary>
        /// 三维点平方距离，避免不必要的开方。
        /// </summary>
        [System.CLSCompliant(false)]
        public static float SqrDistance(Vector3 a, Vector3 b)
        {
            return (a - b).sqrMagnitude;
        }
    }
}