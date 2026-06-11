using System;
using System.Collections.Generic;
using System.Linq;

namespace UFlowFramework.DataStructure
{
    public static partial class Sort
    {
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

        /// <summary>
        /// 堆排序最坏情况也能保证效率。它首先将待排序的元素构建成一个最大堆（或最小堆），然后依次将堆顶元素与最后一个元素交换，并对剩余的元素重新调整为堆，直到所有元素都被排序。
        /// </summary>
        public static void HeapSort<T>(IList<T> list, Comparison<T> comparison,
            int startIndex = 0, int length = -1, int takeCount = -1)
        {
            if (!NeedSort(list, comparison)) return;
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
                SiftDownMin(list, lastIndex, startIndex, i, comparison);
            }

            // 依次把堆顶交换到开头，然后对缩小后的堆做下滤
            for (int i = 0; i < n; i++)
            {
                Swap(list, startIndex + i, lastIndex);
                SiftDownMin(list, lastIndex, startIndex + i + 1, lastIndex, comparison);
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

        // 从list的反方向构建最小堆
        private static void SiftDownMin<T>(IList<T> list, int endIndex, int start, int key, Comparison<T> comparison)
        {
            var root = key;

            while (true)
            {
                var smallest = root;
                var temp = endIndex - root;
                var left = endIndex - temp * 2 - 1;
                var right = endIndex - temp * 2 - 2;
                if (left >= start && comparison(list[left], list[smallest]) < 0)
                {
                    smallest = left;
                }

                if (right >= start && comparison(list[right], list[smallest]) < 0)
                {
                    smallest = right;
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
            HeapSort((IList<T>)array, valueMethod, takeCount: takeCount);
            return array;
        }

        /// <summary>
        /// 堆排序最坏情况也能保证效率。它首先将待排序的元素构建成一个最大堆（或最小堆），然后依次将堆顶元素与最后一个元素交换，并对剩余的元素重新调整为堆，直到所有元素都被排序。
        /// </summary>
        public static void HeapSort<T>(Span<T> span, ValueMethod<T> valueMethod,
            int startIndex = 0, int length = -1, int takeCount = -1)
        {
            if (!NeedSort(span, valueMethod)) return;
            if (!CheckParameters(span, startIndex, ref length)) return;
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
                SiftDownMin(span, lastIndex, startIndex, i, valueMethod);
            }

            // 依次把堆顶交换到开头，然后对缩小后的堆做下滤
            for (int i = 0; i < n; i++)
            {
                Swap(span, startIndex + i, lastIndex);
                SiftDownMin(span, lastIndex, startIndex + i + 1, lastIndex, valueMethod);
            }
        }

        /// <summary>
        /// 堆排序最坏情况也能保证效率。它首先将待排序的元素构建成一个最大堆（或最小堆），然后依次将堆顶元素与最后一个元素交换，并对剩余的元素重新调整为堆，直到所有元素都被排序。
        /// </summary>
        public static T[] HeapSort<T>(IEnumerable<T> list, Comparison<T> comparison, int takeCount = -1)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (comparison == null) throw new ArgumentNullException(nameof(comparison));
            var array = list.ToArray();
            HeapSort((IList<T>)array, comparison, takeCount: takeCount);
            return array;
        }

        /// <summary>
        /// 堆排序最坏情况也能保证效率。它首先将待排序的元素构建成一个最大堆（或最小堆），然后依次将堆顶元素与最后一个元素交换，并对剩余的元素重新调整为堆，直到所有元素都被排序。
        /// </summary>
        public static void HeapSort<T>(Span<T> span, Comparison<T> comparison,
            int startIndex = 0, int length = -1, int takeCount = -1)
        {
            if (!NeedSort(span, comparison)) return;
            if (!CheckParameters(span, startIndex, ref length)) return;
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
                SiftDownMin(span, lastIndex, startIndex, i, comparison);
            }

            // 依次把堆顶交换到开头，然后对缩小后的堆做下滤
            for (int i = 0; i < n; i++)
            {
                Swap(span, startIndex + i, lastIndex);
                SiftDownMin(span, lastIndex, startIndex + i + 1, lastIndex, comparison);
            }
        }

        // 从Span的反方向构建最小堆
        private static void SiftDownMin<T>(Span<T> span, int endIndex, int start, int key, ValueMethod<T> valueMethod)
        {
            var root = key;

            while (true)
            {
                var smallest = root;
                var temp = endIndex - root;
                var left = endIndex - temp * 2 - 1;
                var right = endIndex - temp * 2 - 2;
                var smallValue = valueMethod(span[root]);
                if (left >= start)
                {
                    var leftKey = valueMethod(span[left]);
                    if (leftKey < smallValue)
                    {
                        smallest = left;
                        smallValue = leftKey;
                    }
                }

                if (right >= start)
                {
                    var rightKey = valueMethod(span[right]);
                    if (rightKey < smallValue)
                    {
                        smallest = right;
                    }
                }

                if (root != smallest)
                {
                    Swap(span, smallest, root);
                    root = smallest;
                }
                else
                {
                    break;
                }
            }
        }

        // 从Span的反方向构建最小堆
        private static void SiftDownMin<T>(Span<T> span, int endIndex, int start, int key, Comparison<T> comparison)
        {
            var root = key;

            while (true)
            {
                var smallest = root;
                var temp = endIndex - root;
                var left = endIndex - temp * 2 - 1;
                var right = endIndex - temp * 2 - 2;
                if (left >= start && comparison(span[left], span[smallest]) < 0)
                {
                    smallest = left;
                }

                if (right >= start && comparison(span[right], span[smallest]) < 0)
                {
                    smallest = right;
                }

                if (root != smallest)
                {
                    Swap(span, smallest, root);
                    root = smallest;
                }
                else
                {
                    break;
                }
            }
        }

        #endregion
    }
}