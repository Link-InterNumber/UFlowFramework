using System;
using System.Collections.Generic;
using System.Linq;
using Random = System.Random;

namespace PowerCellStudio
{
    /// <summary>
    /// 随机化工具类，包含各种随机化方法。
    /// Randomizer utility class containing various methods for randomization.
    /// </summary>
    public class Randomizer
    {
        private static Random _random = new Random();
        
        /// <summary>
        /// 设置随机种子。
        /// Set random seed for the random number generator.
        /// </summary>
        /// <param name="seed">种子值 / Seed value</param>
        public static void SetSeed(int seed)
        {
            _random = new Random(seed);
        }

        /// <summary>
        /// 获取一个介于0和1之间的随机值。
        /// Get a random value between 0 and 1.
        /// </summary>
        /// <returns>随机值 / Random value</returns>
        public static float Value01()
        {
            return (float)_random.NextDouble();
        }

        /// <summary>
        /// 获取一个介于min和max之间的随机浮点数。
        /// Get a random float between <paramref name="min"/> and <paramref name="max"/>.
        /// </summary>
        /// <param name="min">最小值 / Minimum value</param>
        /// <param name="max">最大值 / Maximum value</param>
        /// <returns>随机浮点数 / Random float</returns>
        public static float Range(float min, float max)
        {
            if (min == max) return min;
            if (min > max)
            {
                var tmp = min;
                min = max;
                max = tmp;
            }
            return min + (float)_random.NextDouble() * (max - min);
        }
        
        /// <summary>
        /// 获取一个介于min和max之间的随机整数。
        /// Get a random integer between <paramref name="min"/> and <paramref name="max"/>.
        /// </summary>
        /// <param name="min">最小值 / Minimum value</param>
        /// <param name="max">最大值 / Maximum value</param>
        /// <returns>随机整数 / Random integer</returns>
        public static int Range(int min, int max)
        {
            return _random.Next(min, max);
        }
        
        /// <summary>
        /// 获取一个介于min和max之间的随机长整型数。
        /// Get a random long between <paramref name="min"/> and <paramref name="max"/>.
        /// </summary>
        /// <param name="min">最小值 / Minimum value</param>
        /// <param name="max">最大值 / Maximum value</param>
        /// <returns>随机长整型数 / Random long</returns>
        public static long Range(long min, long max)
        {
            if (min == max) return min;
            if (min > max)
            {
                long tmp = min;
                min = max;
                max = tmp;
            }

            byte[] buffer = new byte[8];
            _random.NextBytes(buffer);
            double randomDouble = (double)BitConverter.ToUInt64(buffer, 0) / ulong.MaxValue;

            ulong range = (ulong)(max - min);
            return min + (long)(randomDouble * range);
        }
        
        /// <summary>
        /// 生成一个随机长整数。
        /// Generate a random long integer.
        /// </summary>
        /// <returns>随机长整数 / Random long</returns>
        public static long RandomLong()
        {
            byte[] buffer = new byte[8];
            _random.NextBytes(buffer);
        
            long randomLong = BitConverter.ToInt64(buffer, 0);
            return randomLong;
        }

        /// <summary>
        /// 生成一个随机整数。
        /// Generate a random integer.
        /// </summary>
        /// <returns>随机整数 / Random int</returns>
        public static int RandomInt()
        {
            byte[] buffer = new byte[4];
            _random.NextBytes(buffer);
        
            int randomInt = BitConverter.ToInt32(buffer, 0);
            return randomInt;
        }

        /// <summary>
        /// 在[0f, 1f]范围内进行判断。
        /// Check whether a value meets a random condition within [0f, 1f].
        /// </summary>
        /// <param name="val">要判断的值 / Value to check against</param>
        /// <returns>结果，是否符合 / Result, whether it meets the condition</returns>
        public static bool True(float val)
        {
            return val >= Value01();
        }
        
        /// <summary>
        /// 在给定权重和总计范围内进行判断。
        /// Check whether a condition is met based on weight and total range.
        /// </summary>
        /// <param name="weight">权重值 / Weight value</param>
        /// <param name="total">总计范围 / Total range</param>
        /// <returns>结果，是否符合 / Result, whether it meets the condition</returns>
        public static bool True(float weight, float total)
        {
            if (total <= weight) return true;
            return weight >= Range(0f, total);
        }
        
        /// <summary>
        /// 在给定整数权重和总计范围内进行判断。
        /// Check whether a condition is met based on integer weight and total range.
        /// </summary>
        /// <param name="weight">权重值 / Weight value</param>
        /// <param name="total">总计范围 / Total range</param>
        /// <returns>结果，是否符合 / Result, whether it meets the condition</returns>
        public static bool True(int weight, int total)
        {
            if (total <= weight) return true;
            return weight >= Range(0, total);
        }

        /// <summary>
        /// 从数组中随机选择一个元素。
        /// Randomly select an element from an array.
        /// </summary>
        /// <typeparam name="T">元素类型 / Type of element</typeparam>
        /// <param name="elements">抽取池 / Pool of elements to select from</param>
        /// <returns>选中的元素 / Selected element</returns>
        public static T RandomSelection<T>(T[] elements)
        {
            if (elements == null || elements.Length == 0) return default;
            if (elements.Length == 1) return elements[0];
            int randomIndex = _random.Next(0, elements.Length);
            return elements[randomIndex];
        }
        
        /// <summary>
        /// 从列表中随机选择一个元素。
        /// Randomly select an element from a list.
        /// </summary>
        /// <typeparam name="T">元素类型 / Type of element</typeparam>
        /// <param name="elements">抽取池 / Pool of elements to select from</param>
        /// <returns>选中的元素 / Selected element</returns>
        public static T RandomSelection<T>(IList<T> elements)
        {
            if (elements == null || elements.Count == 0) return default;
            if (elements.Count == 1) return elements[0];
            int randomIndex = _random.Next(0, elements.Count);
            return elements[randomIndex];
        }

        /// <summary>
        /// 根据权重从数组中随机选择一个元素。
        /// Randomly select an element from an array based on weights.
        /// </summary>
        /// <typeparam name="T">元素类型 / Type of element</typeparam>
        /// <param name="elements">抽取池 / Pool of elements to select from</param>
        /// <param name="weights">对应权重 / Corresponding weights</param>
        /// <returns>选中的元素 / Selected element</returns>
        public static T WeightedRandomSelection<T>(T[] elements, int[] weights)
        {
            if (elements == null || elements.Length == 0 || weights == null) return default;
            if (weights.Length == 0) return RandomSelection(elements);
            if (elements.Length == 1) return elements[0];

            int totalWeight = 0;
            int n = elements.Length;
            for (int i = 0; i < n; i++)
            {
                int w = i < weights.Length ? weights[i] : 0;
                if (w > 0) totalWeight += w;
            }
            if (totalWeight <= 0) return default;
            int randomNumber = _random.Next(0, totalWeight);
            int cumulativeWeight = 0;
            for (int i = 0; i < elements.Length; i++)
            {
                var w = 0;
                if (weights.Length > i) 
                    w = weights[i];
                cumulativeWeight += w;
                if (randomNumber < cumulativeWeight)
                    return elements[i];
            }
            return default(T);
        }
        
        /// <summary>
        /// 根据权重从列表中随机选择一个元素。
        /// Randomly select an element from a list based on weights.
        /// </summary>
        /// <typeparam name="T">元素类型 / Type of element</typeparam>
        /// <param name="elements">抽取池 / Pool of elements to select from</param>
        /// <param name="weights">对应权重 / Corresponding weights</param>
        /// <returns>选中的元素 / Selected element</returns>
        public static T WeightedRandomSelection<T>(List<T> elements, List<int> weights)
        {
            if (elements == null || elements.Count == 0 || weights == null) return default;
            if (weights.Count == 0) return RandomSelection(elements);
            if (elements.Count == 1) return elements[0];

            int totalWeight = 0;
            int n = elements.Count;
            for (int i = 0; i < n; i++)
            {
                int w = i < weights.Count ? weights[i] : 0;
                if (w > 0) totalWeight += w;
            }
            if (totalWeight <= 0) return default;
            int randomNumber = _random.Next(0, totalWeight);
            int cumulativeWeight = 0;
            for (int i = 0; i < elements.Count; i++)
            {
                var w = 0;
                if (weights.Count > i) 
                    w = weights[i];
                cumulativeWeight += w;
                if (randomNumber < cumulativeWeight)
                    return elements[i];
            }
            return default(T);
        }

        /// <summary>
        /// 根据权重从字典中随机选择一个元素。
        /// Randomly select an element from a dictionary based on weights.
        /// </summary>
        /// <typeparam name="T">元素类型 / Type of element</typeparam>
        /// <param name="itemWeightPair">元素与权重 / Element-weight pairs</param>
        /// <returns>选中的元素 / Selected element</returns>
        public static T WeightedRandomSelection<T>(Dictionary<T, int> itemWeightPair)
        {
            if (itemWeightPair == null || itemWeightPair.Count == 0) return default;

            int totalWeight = itemWeightPair.Values.Sum();
            if (totalWeight <= 0) return default;
            int randomNumber = _random.Next(0, totalWeight);
            int cumulativeWeight = 0;
            foreach (var keyValuePair in itemWeightPair)
            {
                cumulativeWeight += keyValuePair.Value;
                if (randomNumber < cumulativeWeight)
                    return keyValuePair.Key;
            }
            return default(T);
        }
        
        /// <summary>
        /// 从列表中随机选择多个元素，选取元素不重复。
        /// Randomly select multiple elements from a list, without duplication.
        /// </summary>
        /// <typeparam name="T">元素类型 / Type of element</typeparam>
        /// <param name="elements">抽取的元素池 / Pool of elements to select from</param>
        /// <param name="count">抽取的数量 / Number of elements to select</param>
        /// <returns>选中的元素集合 / List of selected elements</returns>
        public static List<T> RandomSelectionWithoutDuplicates<T>(IList<T> elements, int count)
        {
            if (elements == null || elements.Count == 0) return default;
            if (count <= 0) return new List<T>();
            int n = elements.Count;
            if (count >= n) return new List<T>(elements);

            // Fisher-Yates 部分洗牌：只进行前 count 次交换
            T[] buffer = new T[n];
            for (int i = 0; i < n; i++) buffer[i] = elements[i];

            for (int i = 0; i < count; i++)
            {
                int j = _random.Next(i, n); // 随机选一个索引 j∈[i,n)
                // swap buffer[i] and buffer[j]
                T tmp = buffer[i];
                buffer[i] = buffer[j];
                buffer[j] = tmp;
            }

            List<T> result = new List<T>(count);
            for (int i = 0; i < count; i++)
                result.Add(buffer[i]);
            return result;
        }
        
        /// <summary>
        /// 从数组中随机选择多个元素，选取元素不重复。
        /// Randomly select multiple elements from an array, without duplication.
        /// </summary>
        /// <typeparam name="T">元素类型 / Type of element</typeparam>
        /// <param name="elements">抽取的元素池 / Pool of elements to select from</param>
        /// <param name="count">抽取的数量 / Number of elements to select</param>
        /// <returns>选中的元素集合 / List of selected elements</returns>
        public static List<T> RandomSelectionWithoutDuplicates<T>(T[] elements, int count)
        {
            if (elements == null || elements.Length == 0) return new List<T>();
            int n = elements.Length;
            if (count <= 0) return new List<T>();
            if (count >= n) return new List<T>(elements);

            // 部分 Fisher-Yates
            T[] buffer = new T[n];
            Array.Copy(elements, buffer, n);
            for (int i = 0; i < count; i++)
            {
                int j = _random.Next(i, n);
                T tmp = buffer[i];
                buffer[i] = buffer[j];
                buffer[j] = tmp;
            }

            List<T> result = new List<T>(count);
            for (int i = 0; i < count; i++) result.Add(buffer[i]);
            return result;
        }
        
        private class WeightedElement<T>
        {
            public T Element;
            public int Weight;

            /// <summary>
            /// 初始化带权重的元素。
            /// Initialize a weighted element.
            /// </summary>
            /// <param name="element">元素 / Element</param>
            /// <param name="weight">权重 / Weight</param>
            public WeightedElement(T element, int weight)
            {
                Element = element;
                Weight = weight;
            }
        }

        /// <summary>
        /// 带权重随机选择多个元素，不重复。
        /// Randomly select multiple weighted elements without duplication.
        /// </summary>
        /// <typeparam name="T">元素类型 / Type of element</typeparam>
        /// <param name="ItemWeightsPair">元素与权重的字典 / Dictionary of elements and weights</param>
        /// <param name="count">要选取的数量 / Number of elements to select</param>
        /// <returns>选中的元素集合 / List of selected elements</returns>
        public static List<T> WeightedRandomSelectionWithoutDuplicates<T>(IList<T> items, IList<int> weights, int count)
        {
            if (items == null || items.Count <= 0 || weights == null || weights.Count <= 0 || count <= 0) return new List<T>();
            while (items.Count > weights.Count)
            {
                weights.Add(0);
            }
            List<WeightedElement<T>> weightedElements = new List<WeightedElement<T>>();

            for (int i = 0; i < items.Count; i++)
            {
                var w = 0;
                if (weights.Count > i && weights[i] > 0)
                    w = weights[i];
                weightedElements.Add(new WeightedElement<T>(items[i], w));
            }
            return WeightedRandomSelectionWithoutDuplicatesHandler<T>(weightedElements, count);
        }

        /// <summary>
        /// 带权重随机选择多个元素，不重复。
        /// Randomly select multiple weighted elements without duplication.
        /// </summary>
        /// <typeparam name="T">元素类型 / Type of element</typeparam>
        /// <param name="ItemWeightsPair">元素与权重的字典 / Dictionary of elements and weights</param>
        /// <param name="count">要选取的数量 / Number of elements to select</param>
        /// <returns>选中的元素集合 / List of selected elements</returns>
        public static List<T> WeightedRandomSelectionWithoutDuplicates<T>(Dictionary<T, int> ItemWeightsPair, int count)
        {
            if (ItemWeightsPair == null || ItemWeightsPair.Count <= 0 || count <= 0) return new List<T>();
            if (count > ItemWeightsPair.Count) return ItemWeightsPair.Keys.ToList();

            List<WeightedElement<T>> weightedElements = ItemWeightsPair.Select(item => new WeightedElement<T>(item.Key, Math.Max(0, item.Value))).ToList();
            return WeightedRandomSelectionWithoutDuplicatesHandler<T>(weightedElements, count);
        }

        private static List<T> WeightedRandomSelectionWithoutDuplicatesHandler<T>(List<WeightedElement<T>> weightedElements, int count)
        {
            List<T> result = new List<T>();
            weightedElements.Sort((a, b) => b.Weight.CompareTo(a.Weight));
            for (int pick = 0; pick < count; pick++)
            {
                int totalWeight = 0;
                for (int i = 0; i < weightedElements.Count; i++)
                    totalWeight += weightedElements[i].Weight;
                if (totalWeight <= 0) break;

                double rnd = _random.NextDouble();
                int randomValue = (int)(rnd * totalWeight);

                WeightedElement<T> selected = weightedElements[0];
                foreach (WeightedElement<T> element in weightedElements)
                {
                    randomValue -= element.Weight;
                    if (randomValue <= 0)
                    {
                        selected = element;
                        break;
                    }
                }
                result.Add(selected.Element);
                weightedElements.Remove(selected);
            }
            return result;
        }
    }
}