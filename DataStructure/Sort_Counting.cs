using System;
using System.Collections.Generic;
using System.Linq;

namespace UFlowFramework.DataStructure
{
    public static partial class Sort
    {
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
            Span<int> count = stackalloc int[range];
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
            CountingSort((IList<T>)array, valueMethod, startIndex, length);
            return array;
        }

        /// <summary>
        /// 对大量数据且范围较小的整数进行排序时，计数排序是一种非常高效的算法。它通过统计每个整数出现的次数来实现排序，而不是比较元素之间的大小关系。
        /// </summary>
        public static void CountingSort<T>(Span<T> span, ValueMethod<T> valueMethod,
            int startIndex = 0, int length = -1)
        {
            if (!NeedSort(span, valueMethod)) return;
            if (!CheckParameters(span, startIndex, ref length)) return;

            int minValue = valueMethod(span[startIndex]);
            int maxValue = minValue;
            for (int i = startIndex + 1; i < startIndex + length; i++)
            {
                int val = valueMethod(span[i]);
                if (val < minValue) minValue = val;
                if (val > maxValue) maxValue = val;
            }

            int range = maxValue - minValue + 1;
            Span<int> count = stackalloc int[range];
            var output = new T[length];

            for (int i = 0; i < length; i++)
            {
                count[valueMethod(span[startIndex + i]) - minValue]++;
            }

            for (int i = 1; i < range; i++)
            {
                count[i] += count[i - 1];
            }

            for (int i = length - 1; i >= 0; i--)
            {
                var item = span[startIndex + i];
                int itemValue = valueMethod(item);
                int position = count[itemValue - minValue] - 1;
                output[position] = item;
                count[itemValue - minValue]--;
            }

            for (int i = 0; i < length; i++)
            {
                span[startIndex + i] = output[i];
            }
        }

        #endregion
    }
}