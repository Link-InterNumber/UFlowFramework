using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UFlowFramework.DataStructure
{
    /// <summary>
    /// 将具有相同分组条件的元素归并为连续区间的工具。
    /// Utility for placing elements with the same grouping condition into contiguous ranges.
    /// </summary>
    public static class CollectionDivider
    {
        public delegate bool GroupMethod<T>(T item);
        
        /// <summary>
        /// 将指定列表区间原地分为两组，满足分组条件的元素位于第二个分组。返回值为第一个分组的末尾索引。
        /// Partitions the specified list range in place into two groups, with elements matching the group condition in the second group. Returns the end index of the first group.
        /// </summary>
        public static int GroupBy<T>(IList<T> list, GroupMethod<T> valueMethod,
            int startIndex = 0, int length = -1)
        {
            if (!CheckParameters(list, valueMethod, startIndex, ref length)) return startIndex;
            return GroupRange(list, valueMethod, startIndex, startIndex + length - 1);
        }

        /// <summary>
        /// 将序列分为两组并返回新数组，满足分组条件的元素位于第二个分组。返回值为第一个分组的末尾索引。
        /// Partitions a sequence into two groups and returns a new array without modifying the source sequence, with elements matching the group condition in the second group. Returns the end index of the first group.
        /// </summary>
        public static T[] GroupBy<T>(IEnumerable<T> collection, GroupMethod<T> valueMethod, out int groupStartIndex)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (valueMethod == null) throw new ArgumentNullException(nameof(valueMethod));

            var array = collection.ToArray();
            if (array.Length == 0)
            {
                groupStartIndex = 0;
                return array;
            }
            groupStartIndex = GroupRange((IList<T>)array, valueMethod, 0, array.Length - 1);
            return array;
        }

        /// <summary>
        /// 将指定 Span 区间原地分为两组，满足分组条件的元素位于第二个分组。返回值为第一个分组的末尾索引。
        /// Partitions the specified Span range in place into two groups, with elements matching the group condition in the second group. Returns the end index of the first group.
        /// </summary>
        public static int GroupBy<T>(Span<T> span, GroupMethod<T> valueMethod,
            int startIndex = 0, int length = -1)
        {
            if (!CheckParameters(span, valueMethod, startIndex, ref length)) return startIndex;
            return GroupRange(span, valueMethod, startIndex, startIndex + length - 1);
        }

        private static int GroupRange<T>(IList<T> list, GroupMethod<T> valueMethod, int startIndex, int endIndex)
        {
            var left = startIndex;
            var right = endIndex;
            while (left <= right)
            {
                var valueLeft = valueMethod(list[left]);
                if (valueLeft)
                {
                    Swap(list, left, right);
                    right--;
                }
                else
                {
                    left++;
                }
            }
            return right;
        }

        private static int GroupRange<T>(Span<T> span, GroupMethod<T> valueMethod, int startIndex, int endIndex)
        {
            var left = startIndex;
            var right = endIndex;
            while (left <= right)
            {
                var valueLeft = valueMethod(span[left]);
                if (valueLeft)
                {
                    Swap(span, left, right);
                    right--;
                }
                else
                {
                    left++;
                }
            }
            return right;
        }

        private static bool CheckParameters<T>(IList<T> list, GroupMethod<T> valueMethod, int startIndex, ref int length)
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

            return CheckRange(list.Count, startIndex, ref length);
        }

        private static bool CheckParameters<T>(Span<T> span, GroupMethod<T> valueMethod, int startIndex, ref int length)
        {
            if (valueMethod == null)
            {
                Debug.LogError("valueMethod is null");
                return false;
            }

            return CheckRange(span.Length, startIndex, ref length);
        }

        private static bool CheckRange(int count, int startIndex, ref int length)
        {
            if (startIndex < 0 || startIndex >= count)
            {
                Debug.LogError("startIndex is out of range");
                return false;
            }

            if (length < 0)
            {
                length = count - startIndex;
            }
            else
            {
                length = Math.Min(length, count - startIndex);
            }

            if (length == 0)
            {
                return false;
            }

            return true;
        }

        private static void Swap<T>(IList<T> list, int firstIndex, int secondIndex)
        {
            T value = list[firstIndex];
            list[firstIndex] = list[secondIndex];
            list[secondIndex] = value;
        }

        private static void Swap<T>(Span<T> span, int firstIndex, int secondIndex)
        {
            T value = span[firstIndex];
            span[firstIndex] = span[secondIndex];
            span[secondIndex] = value;
        }
    }
}