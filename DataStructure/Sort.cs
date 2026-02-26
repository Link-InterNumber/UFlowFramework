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
                    var swaped = false;
                    for (int j = startIndex; j < end - i; j++)
                    {
                        var aValue = valueMethod(list[j]);
                        var bValue = valueMethod(list[j + 1]);
                        if (aValue > bValue)
                        {
                            Swap(list, j, j + 1);
                            swaped = true;
                        }
                    }
                    if (!swaped) break;
                }
                return;
            }

            var operationCount = Math.Min(takeCount, length);
            if (operationCount <= 0) return;
            for (int i = 0; i < operationCount; i++)
            {
                var swaped = false;
                for (int j = end; j > startIndex + i; j--)
                {
                    var aValue = valueMethod(list[j]);
                    var bValue = valueMethod(list[j - 1]);
                    if (aValue < bValue)
                    {
                        Swap(list, j, j - 1);
                        swaped = true;
                    }
                }
                if (!swaped) break;
            }
        }

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
                int j = i - 1;
                while (j >= startIndex && valueMethod(list[j]) > keyValue)
                {
                    list[j + 1] = list[j];
                    j--;
                }

                list[j + 1] = keyItem;
            }
        }

        public static T[] InsertionSort<T>(IEnumerable<T> list, ValueMethod<T> valueMethod,
            int startIndex = 0, int length = -1)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (valueMethod == null) throw new ArgumentNullException(nameof(valueMethod));
            var array = list.ToArray();
            InsertionSort(array, valueMethod, startIndex, length);
            return array;
        }

        #endregion

        #region Quick

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

        public static T[] QuickSort<T>(IEnumerable<T> list, ValueMethod<T> valueMethod)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (valueMethod == null) throw new ArgumentNullException(nameof(valueMethod));
            var array = list.ToArray();
            QuickSortRange(array, valueMethod, 0, array.Length - 1);
            return array;
        }

        #endregion

        #region Heap

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
            var temp = endIndex - key;
            var left = endIndex - temp * 2 - 1;
            var right = endIndex - temp * 2 - 2;
            var rootKey = valueMethod(list[root]);
            if (left >= start)
            {
                var leftKey = valueMethod(list[left]);
                if (leftKey < rootKey)
                {
                    root = left;
                    rootKey = leftKey;
                }
            }
            
            if (right >= start)
            {
                var rightKey = valueMethod(list[right]);
                if (rightKey < rootKey)
                {
                    root = right;
                }
            }

            if (root != key)
            {
                Swap(list, key, root);
                SiftDownMin(list, endIndex, start, root, valueMethod);
            }
        }

        public static T[] HeapSort<T>(IEnumerable<T> list, ValueMethod<T> valueMethod, int takeCount = -1)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (valueMethod == null) throw new ArgumentNullException(nameof(valueMethod));
            var array = list.ToArray();
            HeapSort(array, valueMethod, takeCount);
            return array;
        }

        #endregion
    }
}