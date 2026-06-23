using System;
using System.Collections.Generic;
using System.Linq;

namespace UFlowFramework.DataStructure
{
    public static partial class Sort
    {
        #region Radix

        /// <summary>
        /// 对大量数据且位数较小的整数进行排序时，基数排序是一种非常高效的算法。它通过将整数分解为不同的位来进行排序，从最低有效位到最高有效位依次进行排序，最终得到有序的结果。
        /// 时间复杂度：O(nk)，其中 n 是元素数量，k 是最大值的位数
        /// 空间复杂度：O(n + k)
        /// </summary>
        public static void RadixSort<T>(IList<T> list, ValueMethod<T> valueMethod,
            int startIndex = 0, int length = -1)
        {
            if (!NeedSort(list, valueMethod)) return;
            if (!CheckParameters(list, startIndex, ref length)) return;

            // 1. 将负数和非负数分离
            var pivotValue = 0;
            var left = startIndex;
            var right = startIndex + length - 1;

            int maxValue = 0;
            int negativeMaxValue = 0;
            while (left < right)
            {
                while (left <= right)
                {
                    var leftValue = valueMethod(list[left]);

                    if (leftValue > maxValue) maxValue = leftValue;
                    if (leftValue < negativeMaxValue) negativeMaxValue = leftValue;

                    if (leftValue < pivotValue)
                        left++;
                    else
                        break;
                }

                while (left < right)
                {
                    var rightValue = valueMethod(list[right]);

                    if (rightValue > maxValue) maxValue = rightValue;
                    if (rightValue < negativeMaxValue) negativeMaxValue = rightValue;

                    if (rightValue >= pivotValue)
                        right--;
                    else
                        break;
                }

                if (left < right)
                {
                    Swap(list, left, right);
                    left++;
                    right--;
                }
            }

            // 非负数的起始索引
            var boundaryIndex = valueMethod(list[right]) > pivotValue ? right : right + 1;
            // 3.初始化桶并进行排序
            var output = new Queue<T>[10];
            for (int i = 0; i < 10; i++)
            {
                output[i] = new Queue<T>();
            }

            // 4. 对负数和非负数分别进行基数排序
            RadixSortPositive(list, valueMethod, boundaryIndex, startIndex + length - boundaryIndex, maxValue, output);
            RadixSortNegative(list, valueMethod, startIndex, boundaryIndex - startIndex, negativeMaxValue, output);
        }

        private static void RadixSortPositive<T>(IList<T> list, ValueMethod<T> valueMethod, int startIndex, int length,
            int maxValue, Queue<T>[] output)
        {
            // 根据最大值的位数，从个位开始循环排序
            for (int exp = 1; maxValue / exp > 0; exp *= 10)
            {
                for (int i = 0; i < length; i++)
                {
                    var bucketIndex = valueMethod(list[startIndex + i]) / exp % 10;
                    output[bucketIndex].Enqueue(list[startIndex + i]);
                }

                var pushIndex = startIndex;
                for (int i = 0; i < 10; i++)
                {
                    var queue = output[i];
                    while (queue.Count > 0)
                    {
                        list[pushIndex] = queue.Dequeue();
                        pushIndex++;
                    }
                }
            }
        }

        private static void RadixSortNegative<T>(IList<T> list, ValueMethod<T> valueMethod, int startIndex, int length,
            int negativeMaxValue, Queue<T>[] output)
        {
            // 根据最大值的位数，从个位开始循环排序
            for (int exp = 1; -negativeMaxValue / exp > 0; exp *= 10)
            {
                for (int i = 0; i < length; i++)
                {
                    var bucketIndex = (-valueMethod(list[startIndex + i]) / exp) % 10;
                    output[bucketIndex].Enqueue(list[startIndex + i]);
                }

                var pushIndex = startIndex;
                for (int i = 9; i >= 0; i--)
                {
                    var queue = output[i];
                    while (queue.Count > 0)
                    {
                        list[pushIndex] = queue.Dequeue();
                        pushIndex++;
                    }
                }
            }
        }

        /// <summary>
        /// 对大量数据且位数较小的整数进行排序时，基数排序是一种非常高效的算法。它通过将整数分解为不同的位来进行排序，从最低有效位到最高有效位依次进行排序，最终得到有序的结果。
        /// 时间复杂度：O(nk)，其中 n 是元素数量，k 是最大值的位数
        /// 空间复杂度：O(n + k)
        /// </summary>
        public static T[] RadixSort<T>(IEnumerable<T> list, ValueMethod<T> valueMethod,
            int startIndex = 0, int length = -1)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (valueMethod == null) throw new ArgumentNullException(nameof(valueMethod));
            var array = list.ToArray();
            RadixSort((IList<T>)array, valueMethod, startIndex, length);
            return array;
        }

        /// <summary>
        /// 对大量数据且位数较小的整数进行排序时，基数排序是一种非常高效的算法。它通过将整数分解为不同的位来进行排序，从最低有效位到最高有效位依次进行排序，最终得到有序的结果。
        /// 时间复杂度：O(nk)，其中 n 是元素数量，k 是最大值的位数
        /// 空间复杂度：O(n + k)
        /// </summary>
        public static void RadixSort<T>(Span<T> span, ValueMethod<T> valueMethod,
            int startIndex = 0, int length = -1)
        {
            if (!NeedSort(span, valueMethod)) return;
            if (!CheckParameters(span, startIndex, ref length)) return;

            var pivotValue = 0;
            var left = startIndex;
            var right = startIndex + length - 1;

            int maxValue = 0;
            int negativeMaxValue = 0;
            while (left < right)
            {
                while (left <= right)
                {
                    var leftValue = valueMethod(span[left]);

                    if (leftValue > maxValue) maxValue = leftValue;
                    if (leftValue < negativeMaxValue) negativeMaxValue = leftValue;

                    if (leftValue < pivotValue)
                        left++;
                    else
                        break;
                }

                while (left < right)
                {
                    var rightValue = valueMethod(span[right]);

                    if (rightValue > maxValue) maxValue = rightValue;
                    if (rightValue < negativeMaxValue) negativeMaxValue = rightValue;

                    if (rightValue >= pivotValue)
                        right--;
                    else
                        break;
                }

                if (left < right)
                {
                    Swap(span, left, right);
                    left++;
                    right--;
                }
            }

            var boundaryIndex = valueMethod(span[right]) > pivotValue ? right : right + 1;

            var output = new Queue<T>[10];
            for (int i = 0; i < 10; i++)
            {
                output[i] = new Queue<T>();
            }

            RadixSortPositive(span, valueMethod, boundaryIndex, startIndex + length - boundaryIndex, maxValue, output);
            RadixSortNegative(span, valueMethod, startIndex, boundaryIndex - startIndex, negativeMaxValue, output);
        }

        private static void RadixSortPositive<T>(Span<T> span, ValueMethod<T> valueMethod, int startIndex, int length,
            int maxValue, Queue<T>[] output)
        {
            for (int exp = 1; maxValue / exp > 0; exp *= 10)
            {
                for (int i = 0; i < length; i++)
                {
                    var bucketIndex = valueMethod(span[startIndex + i]) / exp % 10;
                    output[bucketIndex].Enqueue(span[startIndex + i]);
                }

                var pushIndex = startIndex;
                for (int i = 0; i < 10; i++)
                {
                    var queue = output[i];
                    while (queue.Count > 0)
                    {
                        span[pushIndex] = queue.Dequeue();
                        pushIndex++;
                    }
                }
            }
        }

        private static void RadixSortNegative<T>(Span<T> span, ValueMethod<T> valueMethod, int startIndex, int length,
            int negativeMaxValue, Queue<T>[] output)
        {
            for (int exp = 1; -negativeMaxValue / exp > 0; exp *= 10)
            {
                for (int i = 0; i < length; i++)
                {
                    var bucketIndex = (-valueMethod(span[startIndex + i]) / exp) % 10;
                    output[bucketIndex].Enqueue(span[startIndex + i]);
                }

                var pushIndex = startIndex;
                for (int i = 9; i >= 0; i--)
                {
                    var queue = output[i];
                    while (queue.Count > 0)
                    {
                        span[pushIndex] = queue.Dequeue();
                        pushIndex++;
                    }
                }
            }
        }

        #endregion
    }
}