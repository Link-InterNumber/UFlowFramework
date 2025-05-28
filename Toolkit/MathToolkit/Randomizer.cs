using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;
using UnityRandom = UnityEngine.Random;

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
        /// Set random seed for Unity's random number generator.
        /// </summary>
        /// <param name="seed">种子值 / Seed value</param>
        public static void SetSeed(int seed)
        {
            UnityRandom.InitState(seed);
        }

        /// <summary>
        /// 获取一个介于0和1之间的随机值。
        /// Get a random value between 0 and 1.
        /// </summary>
        /// <returns>随机值 / Random value</returns>
        public static float Value01()
        {
            return UnityRandom.value;
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
            return UnityRandom.Range(min, max);
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
            return UnityRandom.Range(min, max);
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
                var temp = min;
                min = max;
                max = min;
            }

            byte[] buffer = new byte[8];
            _random.NextBytes(buffer);
            double randomDouble = (double)BitConverter.ToUInt64(buffer, 0) / ulong.MaxValue;

            long range = max - min;
            return (long)(randomDouble * range) + min;
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
        /// 在[0f, 1f]范围内进行判断。
        /// Check whether a value meets a random condition within [0f, 1f].
        /// </summary>
        /// <param name="val">要判断的值 / Value to check against</param>
        /// <returns>结果，是否符合 / Result, whether it meets the condition</returns>
        public static bool True(float val)
        {
            return val >= UnityRandom.value;
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
            return weight >= UnityRandom.Range(0f, total);
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
            return weight >= UnityRandom.Range(0, total);
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
            int randomIndex = UnityRandom.Range(0, elements.Length);
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
            int randomIndex = UnityRandom.Range(0, elements.Count);
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

            int totalWeight = weights.Sum();
            int randomNumber = UnityRandom.Range(0, totalWeight);
            int cumulativeWeight = 0;
            for (int i = 0; i < elements.Length; i++)
            {
                var w = 0;
                if(weights.Length > i) 
                    w = weights[i];
                cumulativeWeight += w;
                if (randomNumber > cumulativeWeight) continue;
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

            int totalWeight = weights.Sum();
            int randomNumber = UnityRandom.Range(0, totalWeight);
            int cumulativeWeight = 0;
            for (int i = 0; i < elements.Count; i++)
            {
                var w = 0;
                if(weights.Count > i) 
                    w = weights[i];
                cumulativeWeight += w;
                if (randomNumber > cumulativeWeight) continue;
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
            int randomNumber = UnityRandom.Range(0, totalWeight);
            int cumulativeWeight = 0;
            foreach (var keyValuePair in itemWeightPair)
            {
                cumulativeWeight += keyValuePair.Value;
                if (randomNumber > cumulativeWeight) continue;
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
            if (elements.Count == 1) return new List<T>(elements);

            List<T> result = new List<T>();
            if (count > elements.Count)
            {
                Debug.LogWarning("Count exceeds the number of elements!");
                return result;
            }

            List<T> remainingElements = elements.ToList();
            for (int i = 0; i < count; i++)
            {
                if(remainingElements.Count == 0) break;
                int randomIndex = UnityRandom.Range(0, remainingElements.Count);
                result.Add(remainingElements[randomIndex]);
                remainingElements.RemoveAt(randomIndex);
            }
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
            List<T> result = new List<T>();
            if (count > elements.Length)
            {
                Debug.LogWarning("Count exceeds the number of elements!");
                return result;
            }

            List<T> remainingElements = elements.ToList();
            for (int i = 0; i < count; i++)
            {
                if(remainingElements.Count == 0) break;
                int randomIndex = UnityRandom.Range(0, remainingElements.Count);
                result.Add(remainingElements[randomIndex]);
                remainingElements.RemoveAt(randomIndex);
            }
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
        public static List<T> WeightedRandomSelectionWithoutDuplicates<T>(Dictionary<T, int> ItemWeightsPair, int count)
        {
            if (ItemWeightsPair == null || ItemWeightsPair.Count <= 0 || count <= 0) return new List<T>();
            if (count > ItemWeightsPair.Count) return ItemWeightsPair.Keys.ToList();

            List<T> result = new List<T>();
            List<WeightedElement<T>> weightedElements = ItemWeightsPair.Select(item => new WeightedElement<T>(item.Key, item.Value)).ToList();
            weightedElements.Sort((a, b) => b.Weight.CompareTo(a.Weight));

            for (int i = 0; i < count; i++)
            {
                WeightedElement<T> selected = weightedElements[0];
                float totalWeight = weightedElements.Aggregate<WeightedElement<T>, float>(0, (current, element) => current + element.Weight);
                float randomValue = UnityRandom.Range(0, totalWeight);

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