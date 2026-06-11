using System;
using System.Collections.Generic;
using System.Linq;

namespace UFlowFramework.DataStructure
{
    public static partial class Sort
    {
        #region Bubble

        /// <summary>
        /// 冒泡排序是一种简单的排序算法，平均时间复杂度为O(n^2)。它通过重复地遍历要排序的元素，比较相邻的元素并交换它们的位置，直到整个序列有序。
        /// </summary>
        public static void BubbleSort<T>(IList<T> list, ValueMethod<T> valueMethod,
            int startIndex = 0, int length = -1, int takeCount = -1)
        {
            if (!NeedSort(list, valueMethod)) return;
            if (!CheckParameters(list, startIndex, ref length)) return;

            bool startToEnd = takeCount <= 0;
            var end = startIndex + length - 1;

            if (startToEnd)
            {
                for (int i = 0; i < length; i++)
                {
                    var swapped = false;
                    var preValue = valueMethod(list[startIndex]);
                    for (int j = startIndex; j < end - i; j++)
                    {
                        var bValue = valueMethod(list[j + 1]);
                        if (preValue > bValue)
                        {
                            Swap(list, j, j + 1);
                            swapped = true;
                        }
                        else
                        {
                            preValue = bValue;
                        }
                    }

                    if (!swapped) break;
                }

                return;
            }

            var operationCount = Math.Min(takeCount, length);
            if (operationCount <= 0) return;
            for (int i = 0; i < operationCount; i++)
            {
                var swapped = false;
                var preValue = valueMethod(list[end]);
                for (int j = end; j > startIndex + i; j--)
                {
                    var bValue = valueMethod(list[j - 1]);
                    if (preValue < bValue)
                    {
                        Swap(list, j, j - 1);
                        swapped = true;
                    }
                    else
                    {
                        preValue = bValue;
                    }
                }

                if (!swapped) break;
            }
        }

        /// <summary>
        /// 冒泡排序是一种简单的排序算法，平均时间复杂度为O(n^2)。它通过重复地遍历要排序的元素，比较相邻的元素并交换它们的位置，直到整个序列有序。
        /// </summary>
        public static T[] BubbleSort<T>(IEnumerable<T> list, ValueMethod<T> valueMethod,
            int startIndex = 0, int length = -1, int takeCount = -1)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (valueMethod == null) throw new ArgumentNullException(nameof(valueMethod));
            var array = list.ToArray();
            BubbleSort((IList<T>)array, valueMethod, startIndex, length, takeCount);
            return array;
        }

        /// <summary>
        /// 冒泡排序是一种简单的排序算法，平均时间复杂度为O(n^2)。它通过重复地遍历要排序的元素，比较相邻的元素并交换它们的位置，直到整个序列有序。
        /// </summary>
        public static void BubbleSort<T>(Span<T> span, ValueMethod<T> valueMethod,
            int startIndex = 0, int length = -1, int takeCount = -1)
        {
            if (!NeedSort(span, valueMethod)) return;
            if (!CheckParameters(span, startIndex, ref length)) return;

            bool startToEnd = takeCount <= 0;
            var end = startIndex + length - 1;

            if (startToEnd)
            {
                for (int i = 0; i < length; i++)
                {
                    var swapped = false;
                    var preValue = valueMethod(span[startIndex]);
                    for (int j = startIndex; j < end - i; j++)
                    {
                        var bValue = valueMethod(span[j + 1]);
                        if (preValue > bValue)
                        {
                            Swap(span, j, j + 1);
                            swapped = true;
                        }
                        else
                        {
                            preValue = bValue;
                        }
                    }

                    if (!swapped) break;
                }

                return;
            }

            var operationCount = Math.Min(takeCount, length);
            if (operationCount <= 0) return;
            for (int i = 0; i < operationCount; i++)
            {
                var swapped = false;
                var preValue = valueMethod(span[end]);
                for (int j = end; j > startIndex + i; j--)
                {
                    var bValue = valueMethod(span[j - 1]);
                    if (preValue < bValue)
                    {
                        Swap(span, j, j - 1);
                        swapped = true;
                    }
                    else
                    {
                        preValue = bValue;
                    }
                }

                if (!swapped) break;
            }
        }

        /// <summary>
        /// 冒泡排序是一种简单的排序算法，平均时间复杂度为O(n^2)。它通过重复地遍历要排序的元素，比较相邻的元素并交换它们的位置，直到整个序列有序。
        /// </summary>
        public static void BubbleSort<T>(IList<T> list, Comparison<T> comparison,
            int startIndex = 0, int length = -1, int takeCount = -1)
        {
            if (!NeedSort(list, comparison)) return;
            if (!CheckParameters(list, startIndex, ref length)) return;

            bool startToEnd = takeCount <= 0;
            var end = startIndex + length - 1;

            if (startToEnd)
            {
                for (int i = 0; i < length; i++)
                {
                    var swapped = false;
                    for (int j = startIndex; j < end - i; j++)
                    {
                        if (comparison(list[j], list[j + 1]) > 0)
                        {
                            Swap(list, j, j + 1);
                            swapped = true;
                        }
                    }

                    if (!swapped) break;
                }

                return;
            }

            var operationCount = Math.Min(takeCount, length);
            if (operationCount <= 0) return;
            for (int i = 0; i < operationCount; i++)
            {
                var swapped = false;
                for (int j = end; j > startIndex + i; j--)
                {
                    if (comparison(list[j - 1], list[j]) > 0)
                    {
                        Swap(list, j, j - 1);
                        swapped = true;
                    }
                }

                if (!swapped) break;
            }
        }

        /// <summary>
        /// 冒泡排序是一种简单的排序算法，平均时间复杂度为O(n^2)。它通过重复地遍历要排序的元素，比较相邻的元素并交换它们的位置，直到整个序列有序。
        /// </summary>
        public static T[] BubbleSort<T>(IEnumerable<T> list, Comparison<T> comparison,
            int startIndex = 0, int length = -1, int takeCount = -1)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (comparison == null) throw new ArgumentNullException(nameof(comparison));
            var array = list.ToArray();
            BubbleSort((IList<T>)array, comparison, startIndex, length, takeCount);
            return array;
        }

        /// <summary>
        /// 冒泡排序是一种简单的排序算法，平均时间复杂度为O(n^2)。它通过重复地遍历要排序的元素，比较相邻的元素并交换它们的位置，直到整个序列有序。
        /// </summary>
        public static void BubbleSort<T>(Span<T> span, Comparison<T> comparison,
            int startIndex = 0, int length = -1, int takeCount = -1)
        {
            if (!NeedSort(span, comparison)) return;
            if (!CheckParameters(span, startIndex, ref length)) return;

            bool startToEnd = takeCount <= 0;
            var end = startIndex + length - 1;

            if (startToEnd)
            {
                for (int i = 0; i < length; i++)
                {
                    var swapped = false;
                    for (int j = startIndex; j < end - i; j++)
                    {
                        if (comparison(span[j], span[j + 1]) > 0)
                        {
                            Swap(span, j, j + 1);
                            swapped = true;
                        }
                    }

                    if (!swapped) break;
                }

                return;
            }

            var operationCount = Math.Min(takeCount, length);
            if (operationCount <= 0) return;
            for (int i = 0; i < operationCount; i++)
            {
                var swapped = false;
                for (int j = end; j > startIndex + i; j--)
                {
                    if (comparison(span[j - 1], span[j]) > 0)
                    {
                        Swap(span, j, j - 1);
                        swapped = true;
                    }
                }

                if (!swapped) break;
            }
        }

        #endregion
    }
}