using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace RVO.JobSystem
{
    internal static class MathUtil
    {
        internal const float RvoEpsilon = 0.00001f;
        
        public static float AbsSq(float3 value)
        {
            return math.dot(value, value);
        }

        public static float LeftOf(float3 a, float3 b, float3 c)
        {
            return Det(a - c, b - a);
        }

        public static float Det(float3 lhs, float3 rhs)
        {
            return lhs.x * rhs.y - lhs.y * rhs.x;
        }

        public static float3 NormalizeSafe(float3 value)
        {
            var lengthSq = AbsSq(value);
            if (lengthSq <= RvoEpsilon * RvoEpsilon)
            {
                return float3.zero;
            }

            return value * math.rsqrt(lengthSq);
        }

        public static float DistSqPointLineSegment(float3 segmentStart, float3 segmentEnd, float3 point)
        {
            var segment = segmentEnd - segmentStart;
            var segmentLengthSq = math.max(AbsSq(segment), RvoEpsilon);
            var t = math.dot(point - segmentStart, segment) / segmentLengthSq;

            if (t < 0.0f)
            {
                return AbsSq(point - segmentStart);
            }

            if (t > 1.0f)
            {
                return AbsSq(point - segmentEnd);
            }

            return AbsSq(point - (segmentStart + t * segment));
        }

        public static float DistSqSegmentSegment(float3 p1, float3 q1, float3 p2, float3 q2)
        {
            var d1 = q1 - p1;
            var d2 = q2 - p2;
            var r = p1 - p2;
            var a = AbsSq(d1);
            var e = AbsSq(d2);
            var f = math.dot(d2, r);

            float s;
            float t;

            if (a <= RvoEpsilon && e <= RvoEpsilon)
            {
                return AbsSq(p1 - p2);
            }

            if (a <= RvoEpsilon)
            {
                s = 0.0f;
                t = math.clamp(f / math.max(e, RvoEpsilon), 0.0f, 1.0f);
            }
            else
            {
                var c = math.dot(d1, r);
                if (e <= RvoEpsilon)
                {
                    t = 0.0f;
                    s = math.clamp(-c / math.max(a, RvoEpsilon), 0.0f, 1.0f);
                }
                else
                {
                    var b = math.dot(d1, d2);
                    var denom = a * e - b * b;

                    if (math.abs(denom) > RvoEpsilon)
                    {
                        s = math.clamp((b * f - c * e) / denom, 0.0f, 1.0f);
                    }
                    else
                    {
                        s = 0.0f;
                    }

                    t = (b * s + f) / math.max(e, RvoEpsilon);
                    if (t < 0.0f)
                    {
                        t = 0.0f;
                        s = math.clamp(-c / math.max(a, RvoEpsilon), 0.0f, 1.0f);
                    }
                    else if (t > 1.0f)
                    {
                        t = 1.0f;
                        s = math.clamp((b - c) / math.max(a, RvoEpsilon), 0.0f, 1.0f);
                    }
                }
            }

            var closest1 = p1 + d1 * s;
            var closest2 = p2 + d2 * t;
            return AbsSq(closest1 - closest2);
        }
        
        private static void Swap<T>(Span<T> list, int i, int j)
        {
            var temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
        
        private static void QuickSortRange<T>(Span<T> list, int left, int right, Comparison<T> comparison)
        {
            if (left >= right) return;
            // 选择中间元素作为枢轴，并将其值与最左边的元素交换
            // 这样枢轴元素就被移出了分区过程
            int pivotIndex = left + ((right - left) >> 1);
            var pivotValue = list[pivotIndex];

            Swap(list, pivotIndex, left);

            int i = left + 1;
            int j = right;

            // 分区过程
            while (i <= j)
            {
                // 从左向右找到第一个大于等于 pivotValue 的元素
                while (i <= j && comparison(list[i], pivotValue) < 0)
                {
                    i++;
                }

                // 从右向左找到第一个小于等于 pivotValue 的元素
                while (i <= j && comparison(list[j], pivotValue) > 0)
                {
                    j--;
                }

                if (i <= j)
                {
                    Swap(list, i, j);
                    i++;
                    j--;
                }
            }

            // 将枢轴元素放回正确的位置
            Swap(list, left, j);

            // 递归地对左右两个子分区进行排序
            if (left < j - 1)
            {
                QuickSortRange(list, left, j - 1, comparison);
            }

            if (i < right)
            {
                QuickSortRange(list, i, right, comparison);
            }
        }

        /// <summary>
        /// 快速排序是一种高效的排序算法，平均时间复杂度为O(n log n)。它通过选择一个“枢轴”元素，将数组分成两部分，一部分比枢轴小，另一部分比枢轴大，然后递归地对这两部分进行排序，最终得到有序的结果。
        /// </summary>
        public static void QuickSort<T>(Span<T> list, Comparison<T> comparison)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (comparison == null) throw new ArgumentNullException(nameof(comparison));
            if (list.Length < 2) return;
            QuickSortRange(list, 0, list.Length - 1, comparison);
        }


        public static int BinarySearch(List<ManagedObstacleState> list, int id)
        {
            var left = 0;
            var right = list.Count - 1;
            if (list.Count == 0 || id < list[left].id || id > list[right].id)
            {
                return -1;
            }
            if (list[left].id == id)
            {
                return left;
            }
            if (list[right].id == id)
            {
                return right;
            }
            while (left <= right)
            {
                var mid = left + ((right - left) >> 1);
                if (list[mid].id == id)
                {
                    return mid;
                }
                if (list[mid].id < id)
                {
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }
            return -1;
        }
    }
}