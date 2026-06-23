using System;
using System.Collections.Generic;
using System.Linq;

namespace UFlowFramework.DataStructure
{
    public static partial class Sort
    {
        #region Quick

        /// <summary>
        /// 快速排序是一种高效的排序算法，平均时间复杂度为O(n log n)。它通过选择一个“枢轴”元素，将数组分成两部分，一部分比枢轴小，另一部分比枢轴大，然后递归地对这两部分进行排序，最终得到有序的结果。
        /// 时间复杂度：O(n log n)
        /// 空间复杂度：O(log n)（递归栈空间）
        /// </summary>
        public static void QuickSort<T>(IList<T> list, Sort.ValueMethod<T> valueMethod,
            int startIndex = 0, int length = -1)
        {
            if (!NeedSort(list, valueMethod)) return;
            if (!CheckParameters(list, startIndex, ref length)) return;
            QuickSortRange(list, valueMethod, startIndex, startIndex + length - 1);
        }

        private static void QuickSortRange<T>(IList<T> list, Sort.ValueMethod<T> valueMethod, int left, int right)
        {
            if (left >= right) return;
            // 选择中间元素作为枢轴，并将其值与最左边的元素交换
            // 这样枢轴元素就被移出了分区过程
            int pivotIndex = left + ((right - left) >> 1);
            var pivotValue = valueMethod(list[pivotIndex]);

            Swap(list, pivotIndex, left);

            int i = left + 1;
            int j = right;

            // 分区过程
            while (i <= j)
            {
                // 从左向右找到第一个大于等于 pivotValue 的元素
                while (i <= j && valueMethod(list[i]) < pivotValue)
                {
                    i++;
                }

                // 从右向左找到第一个小于等于 pivotValue 的元素
                while (i <= j && valueMethod(list[j]) > pivotValue)
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
                QuickSortRange(list, valueMethod, left, j - 1);
            }

            if (i < right)
            {
                QuickSortRange(list, valueMethod, i, right);
            }
        }

        /// <summary>
        /// 快速排序是一种高效的排序算法，平均时间复杂度为O(n log n)。它通过选择一个“枢轴”元素，将数组分成两部分，一部分比枢轴小，另一部分比枢轴大，然后递归地对这两部分进行排序，最终得到有序的结果。
        /// 时间复杂度：O(n log n)
        /// 空间复杂度：O(log n)（递归栈空间）
        /// </summary>
        public static T[] QuickSort<T>(IEnumerable<T> list, Sort.ValueMethod<T> valueMethod)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (valueMethod == null) throw new ArgumentNullException(nameof(valueMethod));
            var array = list.ToArray();
            QuickSortRange((IList<T>)array, valueMethod, 0, array.Length - 1);
            return array;
        }

        /// <summary>
        /// 快速排序是一种高效的排序算法，平均时间复杂度为O(n log n)。它通过选择一个“枢轴”元素，将数组分成两部分，一部分比枢轴小，另一部分比枢轴大，然后递归地对这两部分进行排序，最终得到有序的结果。
        /// 时间复杂度：O(n log n)
        /// 空间复杂度：O(log n)（递归栈空间）
        /// </summary>
        public static void QuickSort<T>(Span<T> span, Sort.ValueMethod<T> valueMethod,
            int startIndex = 0, int length = -1)
        {
            if (!NeedSort(span, valueMethod)) return;
            if (!CheckParameters(span, startIndex, ref length)) return;
            QuickSortRange(span, valueMethod, startIndex, startIndex + length - 1);
        }

        private static void QuickSortRange<T>(Span<T> span, Sort.ValueMethod<T> valueMethod, int left, int right)
        {
            if (left >= right) return;
            int pivotIndex = left + ((right - left) >> 1);
            var pivotValue = valueMethod(span[pivotIndex]);

            Swap(span, pivotIndex, left);

            int i = left + 1;
            int j = right;

            while (i <= j)
            {
                while (i <= j && valueMethod(span[i]) < pivotValue)
                {
                    i++;
                }

                while (i <= j && valueMethod(span[j]) > pivotValue)
                {
                    j--;
                }

                if (i <= j)
                {
                    Swap(span, i, j);
                    i++;
                    j--;
                }
            }

            Swap(span, left, j);

            if (left < j - 1)
            {
                QuickSortRange(span, valueMethod, left, j - 1);
            }

            if (i < right)
            {
                QuickSortRange(span, valueMethod, i, right);
            }
        }

        /// <summary>
        /// 快速排序是一种高效的排序算法，平均时间复杂度为O(n log n)。它通过选择一个“枢轴”元素，将数组分成两部分，一部分比枢轴小，另一部分比枢轴大，然后递归地对这两部分进行排序，最终得到有序的结果。
        /// 时间复杂度：O(n log n)
        /// 空间复杂度：O(log n)（递归栈空间）
        /// </summary>
        public static void QuickSort<T>(IList<T> list, Comparison<T> comparison,
            int startIndex = 0, int length = -1)
        {
            if (!NeedSort(list, comparison)) return;
            if (!CheckParameters(list, startIndex, ref length)) return;
            QuickSortRange(list, comparison, startIndex, startIndex + length - 1);
        }

        private static void QuickSortRange<T>(IList<T> list, Comparison<T> comparison, int left, int right)
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
                QuickSortRange(list, comparison, left, j - 1);
            }

            if (i < right)
            {
                QuickSortRange(list, comparison, i, right);
            }
        }

        /// <summary>
        /// 快速排序是一种高效的排序算法，平均时间复杂度为O(n log n)。它通过选择一个“枢轴”元素，将数组分成两部分，一部分比枢轴小，另一部分比枢轴大，然后递归地对这两部分进行排序，最终得到有序的结果。
        /// 时间复杂度：O(n log n)
        /// 空间复杂度：O(log n)（递归栈空间）
        /// </summary>
        public static T[] QuickSort<T>(IEnumerable<T> list, Comparison<T> comparison,
            int startIndex = 0, int length = -1)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (comparison == null) throw new ArgumentNullException(nameof(comparison));
            var array = list.ToArray();
            QuickSort((IList<T>)array, comparison, startIndex, length);
            return array;
        }

        /// <summary>
        /// 快速排序是一种高效的排序算法，平均时间复杂度为O(n log n)。它通过选择一个“枢轴”元素，将数组分成两部分，一部分比枢轴小，另一部分比枢轴大，然后递归地对这两部分进行排序，最终得到有序的结果。
        /// 时间复杂度：O(n log n)
        /// 空间复杂度：O(log n)（递归栈空间）
        /// </summary>
        public static void QuickSort<T>(Span<T> span, Comparison<T> comparison,
            int startIndex = 0, int length = -1)
        {
            if (!NeedSort(span, comparison)) return;
            if (!CheckParameters(span, startIndex, ref length)) return;
            QuickSortRange(span, comparison, startIndex, startIndex + length - 1);
        }

        private static void QuickSortRange<T>(Span<T> span, Comparison<T> comparison, int left, int right)
        {
            if (left >= right) return;
            int pivotIndex = left + ((right - left) >> 1);
            var pivotValue = span[pivotIndex];

            Swap(span, pivotIndex, left);

            int i = left + 1;
            int j = right;

            while (i <= j)
            {
                while (i <= j && comparison(span[i], pivotValue) < 0)
                {
                    i++;
                }

                while (i <= j && comparison(span[j], pivotValue) > 0)
                {
                    j--;
                }

                if (i <= j)
                {
                    Swap(span, i, j);
                    i++;
                    j--;
                }
            }

            Swap(span, left, j);

            if (left < j - 1)
            {
                QuickSortRange(span, comparison, left, j - 1);
            }

            if (i < right)
            {
                QuickSortRange(span, comparison, i, right);
            }
        }

        /// <summary>
        /// 快速排序是一种高效的排序算法，平均时间复杂度为O(n log n)。它通过选择一个“枢轴”元素，将数组分成两部分，一部分比枢轴小，另一部分比枢轴大，然后递归地对这两部分进行排序，最终得到有序的结果。
        /// 时间复杂度：O(n log n)
        /// 空间复杂度：O(log n)（递归栈空间）
        /// </summary>
        public static void QuickSort<T>(IList<T> list, int startIndex = 0, int length = -1)
            where T : IComparable<T>
        {
            if (!NeedSort(list)) return;
            if (!CheckParameters(list, startIndex, ref length)) return;
            QuickSortRange(list, startIndex, startIndex + length - 1);
        }

        private static void QuickSortRange<T>(IList<T> list, int left, int right)
            where T : IComparable<T>
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
                while (i <= j && list[i].CompareTo(pivotValue) < 0)
                {
                    i++;
                }

                // 从右向左找到第一个小于等于 pivotValue 的元素
                while (i <= j && list[j].CompareTo(pivotValue) > 0)
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
                QuickSortRange(list, left, j - 1);
            }

            if (i < right)
            {
                QuickSortRange(list, i, right);
            }
        }

        /// <summary>
        /// 快速排序是一种高效的排序算法，平均时间复杂度为O(n log n)。它通过选择一个“枢轴”元素，将数组分成两部分，一部分比枢轴小，另一部分比枢轴大，然后递归地对这两部分进行排序，最终得到有序的结果。
        /// 时间复杂度：O(n log n)
        /// 空间复杂度：O(log n)（递归栈空间）
        /// </summary>
        public static T[] QuickSort<T>(IEnumerable<T> list)
            where T : IComparable<T>
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            var array = list.ToArray();
            QuickSortRange(array, 0, array.Length - 1);
            return array;
        }

        /// <summary>
        /// 快速排序是一种高效的排序算法，平均时间复杂度为O(n log n)。它通过选择一个“枢轴”元素，将数组分成两部分，一部分比枢轴小，另一部分比枢轴大，然后递归地对这两部分进行排序，最终得到有序的结果。
        /// 时间复杂度：O(n log n)
        /// 空间复杂度：O(log n)（递归栈空间）
        /// </summary>
        public static void QuickSort<T>(Span<T> span, int startIndex = 0, int length = -1)
            where T : IComparable<T>
        {
            if (!NeedSort(span)) return;
            if (!CheckParameters(span, startIndex, ref length)) return;
            QuickSortRange(span, startIndex, startIndex + length - 1);
        }

        private static void QuickSortRange<T>(Span<T> span, int left, int right)
            where T : IComparable<T>
        {
            if (left >= right) return;
            int pivotIndex = left + ((right - left) >> 1);
            var pivotValue = span[pivotIndex];

            Swap(span, pivotIndex, left);

            int i = left + 1;
            int j = right;

            while (i <= j)
            {
                while (i <= j && span[i].CompareTo(pivotValue) < 0)
                {
                    i++;
                }

                while (i <= j && span[j].CompareTo(pivotValue) > 0)
                {
                    j--;
                }

                if (i <= j)
                {
                    Swap(span, i, j);
                    i++;
                    j--;
                }
            }

            Swap(span, left, j);

            if (left < j - 1)
            {
                QuickSortRange(span, left, j - 1);
            }

            if (i < right)
            {
                QuickSortRange(span, i, right);
            }
        }

        #endregion
    }
}