using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UFlowFramework.DataStructure
{
    public class Sort
    {
        public delegate int ValueMethod<T>(T a);

        private static bool NeedSort<T>(IList<T> list, ValueMethod<T> valueMethod)
        {
            if (list == null)
            {
                Debug.LogError("list is null");
                return false;
            }

            if (valueMethod == null)
            {
                Debug.LogError("valueMethod is null");
                return false;
            }

            int n = list.Count;
            if (n < 2) return false;
            return true;
        }

        private static bool NeedSort<T>(IList<T> list)
            where T : IComparable<T>
        {
            if (list == null)
            {
                Debug.LogError("list is null");
                return false;
            }

            int n = list.Count;
            if (n < 2) return false;
            return true;
        }

        private static void Swap<T>(IList<T> list, int i, int j)
        {
            var temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }

        private static bool CheckParameters<T>(IList<T> list, int startIndex, ref int length)
        {
            if (startIndex < 0 || startIndex >= list.Count)
            {
                Debug.LogError("startIndex is out of range");
                return false;
            }

            if (length < 0)
            {
                length = list.Count - startIndex;
            }
            else
            {
                length = Math.Min(length, list.Count - startIndex);
            }

            if (length == 0)
            {
                Debug.LogError("length is 0");
                return false;
            }

            return true;
        }

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
            BubbleSort(array, valueMethod, startIndex, length, takeCount);
            return array;
        }

        #endregion

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
            SelectionSort(array, valueMethod, startIndex, length, takeCount);
            return array;
        }

        #endregion

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
            InsertionSort(array, valueMethod, startIndex, length);
            return array;
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

        #endregion

        #region Quick

        /// <summary>
        /// 快速排序是一种高效的排序算法，平均时间复杂度为O(n log n)。它通过选择一个“枢轴”元素，将数组分成两部分，一部分比枢轴小，另一部分比枢轴大，然后递归地对这两部分进行排序，最终得到有序的结果。
        /// </summary>
        public static void QuickSort<T>(IList<T> list, ValueMethod<T> valueMethod,
            int startIndex = 0, int length = -1)
        {
            if (!NeedSort(list, valueMethod)) return;
            if (!CheckParameters(list, startIndex, ref length)) return;
            QuickSortRange(list, valueMethod, startIndex, startIndex + length - 1);
        }

        private static void QuickSortRange<T>(IList<T> list, ValueMethod<T> valueMethod, int left, int right)
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
        /// </summary>
        public static T[] QuickSort<T>(IEnumerable<T> list, ValueMethod<T> valueMethod)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (valueMethod == null) throw new ArgumentNullException(nameof(valueMethod));
            var array = list.ToArray();
            QuickSortRange(array, valueMethod, 0, array.Length - 1);
            return array;
        }

        /// <summary>
        /// 快速排序是一种高效的排序算法，平均时间复杂度为O(n log n)。它通过选择一个“枢轴”元素，将数组分成两部分，一部分比枢轴小，另一部分比枢轴大，然后递归地对这两部分进行排序，最终得到有序的结果。
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
        /// </summary>
        public static T[] QuickSort<T>(IEnumerable<T> list)
            where T : IComparable<T>
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            var array = list.ToArray();
            QuickSortRange(array, 0, array.Length - 1);
            return array;
        }

        #endregion

        #region Heap

        /// <summary>
        /// 堆排序最坏情况也能保证效率。它首先将待排序的元素构建成一个最大堆（或最小堆），然后依次将堆顶元素与最后一个元素交换，并对剩余的元素重新调整为堆，直到所有元素都被排序。
        /// </summary>
        public static void HeapSort<T>(IList<T> list, ValueMethod<T> valueMethod,
            int startIndex = 0, int length = -1, int takeCount = -1)
        {
            if (!NeedSort(list, valueMethod)) return;
            if (!CheckParameters(list, startIndex, ref length)) return;
            int n = length;
            var lastIndex = startIndex + length - 1;
            if (takeCount > 0)
            {
                n = Math.Min(n, takeCount);
            }

            // 构建最小堆
            var heapStartIndex = lastIndex - startIndex - length / 2 + 1;
            for (int i = heapStartIndex; i <= lastIndex; i++)
            {
                SiftDownMin(list, lastIndex, startIndex, i, valueMethod);
            }

            // 依次把堆顶交换到开头，然后对缩小后的堆做下滤
            for (int i = 0; i < n; i++)
            {
                Swap(list, startIndex + i, lastIndex);
                SiftDownMin(list, lastIndex, startIndex + i + 1, lastIndex, valueMethod);
            }
        }

        // 最大堆的下滤
        private static void SiftDown<T>(IList<T> list, int startIndex, int key, int end, ValueMethod<T> valueMethod)
        {
            var root = key;
            var left = 2 * (key - startIndex) + 1;
            var right = 2 * (key - startIndex) + 2;
            var rootKey = valueMethod(list[root]);
            if (left <= end)
            {
                var leftKey = valueMethod(list[left]);
                if (leftKey > rootKey)
                {
                    root = left;
                    rootKey = leftKey;
                }
            }

            if (right <= end)
            {
                var rightKey = valueMethod(list[right]);
                if (rightKey > rootKey)
                {
                    root = right;
                }
            }

            if (root != key)
            {
                Swap(list, key, root);
                SiftDown(list, startIndex, root, end, valueMethod);
            }
        }

        // 从list的反方向构建最小堆
        private static void SiftDownMin<T>(IList<T> list, int endIndex, int start, int key, ValueMethod<T> valueMethod)
        {
            var root = key;

            while (true)
            {
                var smallest = root;
                var temp = endIndex - root;
                var left = endIndex - temp * 2 - 1;
                var right = endIndex - temp * 2 - 2;
                var smallValue = valueMethod(list[root]);
                if (left >= start)
                {
                    var leftKey = valueMethod(list[left]);
                    if (leftKey < smallValue)
                    {
                        smallest = left;
                        smallValue = leftKey;
                    }
                }

                if (right >= start)
                {
                    var rightKey = valueMethod(list[right]);
                    if (rightKey < smallValue)
                    {
                        smallest = right;
                    }
                }

                if (root != smallest)
                {
                    Swap(list, smallest, root);
                    root = smallest;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 堆排序最坏情况也能保证效率。它首先将待排序的元素构建成一个最大堆（或最小堆），然后依次将堆顶元素与最后一个元素交换，并对剩余的元素重新调整为堆，直到所有元素都被排序。
        /// </summary>
        public static T[] HeapSort<T>(IEnumerable<T> list, ValueMethod<T> valueMethod, int takeCount = -1)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (valueMethod == null) throw new ArgumentNullException(nameof(valueMethod));
            var array = list.ToArray();
            HeapSort(array, valueMethod, takeCount);
            return array;
        }

        #endregion

        #region Radix

        /// <summary>
        /// 对大量数据且位数较小的整数进行排序时，基数排序是一种非常高效的算法。它通过将整数分解为不同的位来进行排序，从最低有效位到最高有效位依次进行排序，最终得到有序的结果。
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
        /// </summary>
        public static T[] RadixSort<T>(IEnumerable<T> list, ValueMethod<T> valueMethod,
            int startIndex = 0, int length = -1)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (valueMethod == null) throw new ArgumentNullException(nameof(valueMethod));
            var array = list.ToArray();
            RadixSort(array, valueMethod, startIndex, length);
            return array;
        }

        #endregion

        #region Counting

        /// <summary>
        /// 对大量数据且范围较小的整数进行排序时，计数排序是一种非常高效的算法。它通过统计每个整数出现的次数来实现排序，而不是比较元素之间的大小关系。
        /// </summary>
        public static void CountingSort<T>(IList<T> list, ValueMethod<T> valueMethod,
            int startIndex = 0, int length = -1)
        {
            if (!NeedSort(list, valueMethod)) return;
            if (!CheckParameters(list, startIndex, ref length)) return;

            // 1. 找出范围内的最大值和最小值
            int minValue = valueMethod(list[startIndex]);
            int maxValue = minValue;
            for (int i = startIndex + 1; i < startIndex + length; i++)
            {
                int val = valueMethod(list[i]);
                if (val < minValue) minValue = val;
                if (val > maxValue) maxValue = val;
            }

            int range = maxValue - minValue + 1;
            var count = new int[range];
            var output = new T[length];

            // 2. 统计每个元素的频率
            for (int i = 0; i < length; i++)
            {
                count[valueMethod(list[startIndex + i]) - minValue]++;
            }

            // 3. 计算累积计数
            for (int i = 1; i < range; i++)
            {
                count[i] += count[i - 1];
            }

            // 4. 构建输出数组 (从后向前保证稳定性)
            for (int i = length - 1; i >= 0; i--)
            {
                var item = list[startIndex + i];
                int itemValue = valueMethod(item);
                int position = count[itemValue - minValue] - 1;
                output[position] = item;
                count[itemValue - minValue]--;
            }

            // 5. 将排序后的结果复制回原列表
            for (int i = 0; i < length; i++)
            {
                list[startIndex + i] = output[i];
            }
        }

        /// <summary>
        /// 对大量数据且范围较小的整数进行排序时，计数排序是一种非常高效的算法。它通过统计每个整数出现的次数来实现排序，而不是比较元素之间的大小关系。
        /// </summary>
        public static T[] CountingSort<T>(IEnumerable<T> list, ValueMethod<T> valueMethod,
            int startIndex = 0, int length = -1)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (valueMethod == null) throw new ArgumentNullException(nameof(valueMethod));
            var array = list.ToArray();
            CountingSort(array, valueMethod, startIndex, length);
            return array;
        }

        #endregion
    }
}