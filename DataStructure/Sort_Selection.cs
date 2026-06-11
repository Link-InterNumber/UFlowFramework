using System;
using System.Collections.Generic;
using System.Linq;

namespace UFlowFramework.DataStructure
{
    public static partial class Sort
    {
        #region Selection

        /// <summary>
        /// 选择排序是一种简单的排序算法，平均时间复杂度为O(n^2)。它通过不断选择剩余元素中最小（或最大）的元素，并将其放到已排序序列的末尾，直到所有元素都被排序。
        /// </summary>
        public static void SelectionSort<T>(IList<T> list, ValueMethod<T> valueMethod,
            int startIndex = 0, int length = -1, int takeCount = -1)
        {
            if (!NeedSort(list, valueMethod)) return;
            if (!CheckParameters(list, startIndex, ref length)) return;

            int lastIndex = startIndex + length - 1;
            var operationCount = length;
            if (takeCount > 0)
            {
                operationCount = Math.Min(operationCount, takeCount);
            }

            var unorderedPointer = startIndex;
            while (unorderedPointer < operationCount + startIndex)
            {
                var minValue = valueMethod(list[unorderedPointer]);
                var minIndex = unorderedPointer;
                for (int i = unorderedPointer; i <= lastIndex; i++)
                {
                    var value = valueMethod(list[i]);
                    if (value < minValue)
                    {
                        minIndex = i;
                        minValue = value;
                    }
                }

                Swap(list, unorderedPointer, minIndex);

                unorderedPointer++;
            }
        }

        /// <summary>
        /// 选择排序是一种简单的排序算法，平均时间复杂度为O(n^2)。它通过不断选择剩余元素中最小（或最大）的元素，并将其放到已排序序列的末尾，直到所有元素都被排序。
        /// </summary>
        public static T[] SelectionSort<T>(IEnumerable<T> list, ValueMethod<T> valueMethod,
            int startIndex = 0, int length = -1, int takeCount = -1)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (valueMethod == null) throw new ArgumentNullException(nameof(valueMethod));
            var array = list.ToArray();
            SelectionSort((IList<T>)array, valueMethod, startIndex, length, takeCount);
            return array;
        }

        /// <summary>
        /// 选择排序是一种简单的排序算法，平均时间复杂度为O(n^2)。它通过不断选择剩余元素中最小（或最大）的元素，并将其放到已排序序列的末尾，直到所有元素都被排序。
        /// </summary>
        public static void SelectionSort<T>(Span<T> span, ValueMethod<T> valueMethod,
            int startIndex = 0, int length = -1, int takeCount = -1)
        {
            if (!NeedSort(span, valueMethod)) return;
            if (!CheckParameters(span, startIndex, ref length)) return;

            int lastIndex = startIndex + length - 1;
            var operationCount = length;
            if (takeCount > 0)
            {
                operationCount = Math.Min(operationCount, takeCount);
            }

            var unorderedPointer = startIndex;
            while (unorderedPointer < operationCount + startIndex)
            {
                var minValue = valueMethod(span[unorderedPointer]);
                var minIndex = unorderedPointer;
                for (int i = unorderedPointer; i <= lastIndex; i++)
                {
                    var value = valueMethod(span[i]);
                    if (value < minValue)
                    {
                        minIndex = i;
                        minValue = value;
                    }
                }

                Swap(span, unorderedPointer, minIndex);

                unorderedPointer++;
            }
        }

        /// <summary>
        /// 选择排序是一种简单的排序算法，平均时间复杂度为O(n^2)。它通过不断选择剩余元素中最小（或最大）的元素，并将其放到已排序序列的末尾，直到所有元素都被排序。
        /// </summary>
        public static void SelectionSort<T>(IList<T> list, Comparison<T> comparison,
            int startIndex = 0, int length = -1, int takeCount = -1)
        {
            if (!NeedSort(list, comparison)) return;
            if (!CheckParameters(list, startIndex, ref length)) return;

            int lastIndex = startIndex + length - 1;
            var operationCount = length;
            if (takeCount > 0)
            {
                operationCount = Math.Min(operationCount, takeCount);
            }

            var unorderedPointer = startIndex;
            while (unorderedPointer < operationCount + startIndex)
            {
                var minIndex = unorderedPointer;
                for (int i = unorderedPointer; i <= lastIndex; i++)
                {
                    if (comparison(list[i], list[minIndex]) < 0)
                    {
                        minIndex = i;
                    }
                }

                Swap(list, unorderedPointer, minIndex);

                unorderedPointer++;
            }
        }

        /// <summary>
        /// 选择排序是一种简单的排序算法，平均时间复杂度为O(n^2)。它通过不断选择剩余元素中最小（或最大）的元素，并将其放到已排序序列的末尾，直到所有元素都被排序。
        /// </summary>
        public static T[] SelectionSort<T>(IEnumerable<T> list, Comparison<T> comparison,
            int startIndex = 0, int length = -1, int takeCount = -1)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (comparison == null) throw new ArgumentNullException(nameof(comparison));
            var array = list.ToArray();
            SelectionSort((IList<T>)array, comparison, startIndex, length, takeCount);
            return array;
        }

        /// <summary>
        /// 选择排序是一种简单的排序算法，平均时间复杂度为O(n^2)。它通过不断选择剩余元素中最小（或最大）的元素，并将其放到已排序序列的末尾，直到所有元素都被排序。
        /// </summary>
        public static void SelectionSort<T>(Span<T> span, Comparison<T> comparison,
            int startIndex = 0, int length = -1, int takeCount = -1)
        {
            if (!NeedSort(span, comparison)) return;
            if (!CheckParameters(span, startIndex, ref length)) return;

            int lastIndex = startIndex + length - 1;
            var operationCount = length;
            if (takeCount > 0)
            {
                operationCount = Math.Min(operationCount, takeCount);
            }

            var unorderedPointer = startIndex;
            while (unorderedPointer < operationCount + startIndex)
            {
                var minIndex = unorderedPointer;
                for (int i = unorderedPointer; i <= lastIndex; i++)
                {
                    if (comparison(span[i], span[minIndex]) < 0)
                    {
                        minIndex = i;
                    }
                }

                Swap(span, unorderedPointer, minIndex);

                unorderedPointer++;
            }
        }

        #endregion
    }
}