using System;
using System.Collections.Generic;
using System.Linq;

namespace UFlowFramework.DataStructure
{
    public static partial class Sort
    {
        #region Insertion

        /// <summary>
        /// 插入排序是一种简单的排序算法，平均时间复杂度为O(n^2)。通过二分查找优化插入位置，减少比较次数。它通过构建有序序列，对于未排序数据，在已排序序列中从后向前扫描，找到相应位置并插入。对于部分有序的数据，插入排序的效率较高。
        /// </summary>
        public static void InsertionSort<T>(IList<T> list, ValueMethod<T> valueMethod,
            int startIndex = 0, int length = -1)
        {
            if (!NeedSort(list, valueMethod)) return;
            if (!CheckParameters(list, startIndex, ref length)) return;
            int n = startIndex + length;

            for (int i = startIndex + 1; i < n; i++)
            {
                T keyItem = list[i];
                int keyValue = valueMethod(keyItem);
                int right = i - 1;
                var left = startIndex;
                var insertIndex = right;
                if (valueMethod(list[left]) >= keyValue)
                {
                    insertIndex = left;
                }
                else if (valueMethod(list[right]) <= keyValue)
                {
                    continue;
                }
                else
                {
                    // 二分查找插入位置
                    while (left < right)
                    {
                        var middle = left + ((right - left) >> 1);
                        var middleValue = valueMethod(list[middle]);
                        if (middleValue == keyValue)
                        {
                            insertIndex = middle;
                            break;
                        }

                        if (middleValue > keyValue)
                        {
                            right = middle;
                            insertIndex = middle;
                        }
                        else
                        {
                            left = middle + 1;
                        }
                    }
                }

                for (int j = i; j > insertIndex; j--)
                {
                    list[j] = list[j - 1];
                }

                list[insertIndex] = keyItem;
            }
        }

        /// <summary>
        /// 插入排序是一种简单的排序算法，平均时间复杂度为O(n^2)。通过二分查找优化插入位置，减少比较次数。它通过构建有序序列，对于未排序数据，在已排序序列中从后向前扫描，找到相应位置并插入。对于部分有序的数据，插入排序的效率较高。
        /// </summary>
        public static T[] InsertionSort<T>(IEnumerable<T> list, ValueMethod<T> valueMethod,
            int startIndex = 0, int length = -1)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (valueMethod == null) throw new ArgumentNullException(nameof(valueMethod));
            var array = list.ToArray();
            InsertionSort((IList<T>)array, valueMethod, startIndex, length);
            return array;
        }

        /// <summary>
        /// 插入排序是一种简单的排序算法，平均时间复杂度为O(n^2)。通过二分查找优化插入位置，减少比较次数。它通过构建有序序列，对于未排序数据，在已排序序列中从后向前扫描，找到相应位置并插入。对于部分有序的数据，插入排序的效率较高。
        /// </summary>
        public static void InsertionSort<T>(Span<T> span, ValueMethod<T> valueMethod,
            int startIndex = 0, int length = -1)
        {
            if (!NeedSort(span, valueMethod)) return;
            if (!CheckParameters(span, startIndex, ref length)) return;
            int n = startIndex + length;

            for (int i = startIndex + 1; i < n; i++)
            {
                T keyItem = span[i];
                int keyValue = valueMethod(keyItem);
                int right = i - 1;
                var left = startIndex;
                var insertIndex = right;
                if (valueMethod(span[left]) >= keyValue)
                {
                    insertIndex = left;
                }
                else if (valueMethod(span[right]) <= keyValue)
                {
                    continue;
                }
                else
                {
                    // 二分查找插入位置
                    while (left < right)
                    {
                        var middle = left + ((right - left) >> 1);
                        var middleValue = valueMethod(span[middle]);
                        if (middleValue == keyValue)
                        {
                            insertIndex = middle;
                            break;
                        }

                        if (middleValue > keyValue)
                        {
                            right = middle;
                            insertIndex = middle;
                        }
                        else
                        {
                            left = middle + 1;
                        }
                    }
                }

                for (int j = i; j > insertIndex; j--)
                {
                    span[j] = span[j - 1];
                }

                span[insertIndex] = keyItem;
            }
        }

        /// <summary>
        /// 插入排序是一种简单的排序算法，平均时间复杂度为O(n^2)。通过二分查找优化插入位置，减少比较次数。它通过构建有序序列，对于未排序数据，在已排序序列中从后向前扫描，找到相应位置并插入。对于部分有序的数据，插入排序的效率较高。
        /// </summary>
        public static void InsertionSort<T>(IList<T> list, Comparison<T> comparison,
            int startIndex = 0, int length = -1)
        {
            if (!NeedSort(list, comparison)) return;
            if (!CheckParameters(list, startIndex, ref length)) return;
            int n = startIndex + length;

            for (int i = startIndex + 1; i < n; i++)
            {
                T keyItem = list[i];
                int right = i - 1;
                var left = startIndex;
                var insertIndex = right;
                if (comparison(list[left], keyItem) >= 0)
                {
                    insertIndex = left;
                }
                else if (comparison(list[right], keyItem) <= 0)
                {
                    continue;
                }
                else
                {
                    // 二分查找插入位置
                    while (left < right)
                    {
                        var middle = left + ((right - left) >> 1);
                        var compareValue = comparison(list[middle], keyItem);
                        if (compareValue == 0)
                        {
                            insertIndex = middle;
                            break;
                        }

                        if (compareValue > 0)
                        {
                            right = middle;
                            insertIndex = middle;
                        }
                        else
                        {
                            left = middle + 1;
                        }
                    }
                }

                for (int j = i; j > insertIndex; j--)
                {
                    list[j] = list[j - 1];
                }

                list[insertIndex] = keyItem;
            }
        }

        /// <summary>
        /// 插入排序是一种简单的排序算法，平均时间复杂度为O(n^2)。通过二分查找优化插入位置，减少比较次数。它通过构建有序序列，对于未排序数据，在已排序序列中从后向前扫描，找到相应位置并插入。对于部分有序的数据，插入排序的效率较高。
        /// </summary>
        public static T[] InsertionSort<T>(IEnumerable<T> list, Comparison<T> comparison,
            int startIndex = 0, int length = -1)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (comparison == null) throw new ArgumentNullException(nameof(comparison));
            var array = list.ToArray();
            InsertionSort((IList<T>)array, comparison, startIndex, length);
            return array;
        }

        /// <summary>
        /// 插入排序是一种简单的排序算法，平均时间复杂度为O(n^2)。通过二分查找优化插入位置，减少比较次数。它通过构建有序序列，对于未排序数据，在已排序序列中从后向前扫描，找到相应位置并插入。对于部分有序的数据，插入排序的效率较高。
        /// </summary>
        public static void InsertionSort<T>(Span<T> span, Comparison<T> comparison,
            int startIndex = 0, int length = -1)
        {
            if (!NeedSort(span, comparison)) return;
            if (!CheckParameters(span, startIndex, ref length)) return;
            int n = startIndex + length;

            for (int i = startIndex + 1; i < n; i++)
            {
                T keyItem = span[i];
                int right = i - 1;
                var left = startIndex;
                var insertIndex = right;
                if (comparison(span[left], keyItem) >= 0)
                {
                    insertIndex = left;
                }
                else if (comparison(span[right], keyItem) <= 0)
                {
                    continue;
                }
                else
                {
                    // 二分查找插入位置
                    while (left < right)
                    {
                        var middle = left + ((right - left) >> 1);
                        var compareValue = comparison(span[middle], keyItem);
                        if (compareValue == 0)
                        {
                            insertIndex = middle;
                            break;
                        }

                        if (compareValue > 0)
                        {
                            right = middle;
                            insertIndex = middle;
                        }
                        else
                        {
                            left = middle + 1;
                        }
                    }
                }

                for (int j = i; j > insertIndex; j--)
                {
                    span[j] = span[j - 1];
                }

                span[insertIndex] = keyItem;
            }
        }

        /// <summary>
        /// 插入排序是一种简单的排序算法，平均时间复杂度为O(n^2)。通过二分查找优化插入位置，减少比较次数。它通过构建有序序列，对于未排序数据，在已排序序列中从后向前扫描，找到相应位置并插入。对于部分有序的数据，插入排序的效率较高。
        /// </summary>
        public static void InsertionSort<T>(IList<T> list, int startIndex = 0, int length = -1)
            where T : IComparable<T>
        {
            if (!NeedSort(list)) return;
            if (!CheckParameters(list, startIndex, ref length)) return;
            int n = startIndex + length;
            for (int i = startIndex + 1; i < n; i++)
            {
                T keyItem = list[i];
                int right = i - 1;
                var left = startIndex;
                var insertIndex = right;
                if (list[left].CompareTo(keyItem) >= 0)
                {
                    insertIndex = left;
                }
                else if (list[right].CompareTo(keyItem) <= 0)
                {
                    continue;
                }
                else
                {
                    // 二分查找插入位置
                    while (left < right)
                    {
                        var middle = left + ((right - left) >> 1);
                        var compareValue = list[middle].CompareTo(keyItem);
                        if (compareValue == 0)
                        {
                            insertIndex = middle;
                            break;
                        }

                        if (compareValue > 0)
                        {
                            right = middle;
                            insertIndex = middle;
                        }
                        else
                        {
                            left = middle + 1;
                        }
                    }
                }

                for (int j = i; j > insertIndex; j--)
                {
                    list[j] = list[j - 1];
                }

                list[insertIndex] = keyItem;
            }
        }

        /// <summary>
        /// 插入排序是一种简单的排序算法，平均时间复杂度为O(n^2)。通过二分查找优化插入位置，减少比较次数。它通过构建有序序列，对于未排序数据，在已排序序列中从后向前扫描，找到相应位置并插入。对于部分有序的数据，插入排序的效率较高。
        /// </summary>
        public static T[] InsertionSort<T>(IEnumerable<T> list, int startIndex = 0, int length = -1)
            where T : IComparable<T>
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            var array = list.ToArray();
            InsertionSort(array, startIndex, length);
            return array;
        }

        /// <summary>
        /// 插入排序是一种简单的排序算法，平均时间复杂度为O(n^2)。通过二分查找优化插入位置，减少比较次数。它通过构建有序序列，对于未排序数据，在已排序序列中从后向前扫描，找到相应位置并插入。对于部分有序的数据，插入排序的效率较高。
        /// </summary>
        public static void InsertionSort<T>(Span<T> span, int startIndex = 0, int length = -1)
            where T : IComparable<T>
        {
            if (!NeedSort(span)) return;
            if (!CheckParameters(span, startIndex, ref length)) return;
            int n = startIndex + length;
            for (int i = startIndex + 1; i < n; i++)
            {
                T keyItem = span[i];
                int right = i - 1;
                var left = startIndex;
                var insertIndex = right;
                if (span[left].CompareTo(keyItem) >= 0)
                {
                    insertIndex = left;
                }
                else if (span[right].CompareTo(keyItem) <= 0)
                {
                    continue;
                }
                else
                {
                    // 二分查找插入位置
                    while (left < right)
                    {
                        var middle = left + ((right - left) >> 1);
                        var compareValue = span[middle].CompareTo(keyItem);
                        if (compareValue == 0)
                        {
                            insertIndex = middle;
                            break;
                        }

                        if (compareValue > 0)
                        {
                            right = middle;
                            insertIndex = middle;
                        }
                        else
                        {
                            left = middle + 1;
                        }
                    }
                }

                for (int j = i; j > insertIndex; j--)
                {
                    span[j] = span[j - 1];
                }

                span[insertIndex] = keyItem;
            }
        }

        #endregion
    }
}