using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;
using UnityRandom = UnityEngine.Random;

namespace PowerCellStudio
{
    public class Randomizer
    {
        private static Random _random = new Random();
        
        public static void SetSeed(int seed)
        {
            UnityRandom.InitState(seed);
        }

        public static float Value01()
        {
            return UnityRandom.value;
        }

        public static float Range(float min, float max)
        {
            return UnityRandom.Range(min, max);
        }
        
        public static int Range(int min, int max)
        {
            return UnityRandom.Range(min, max);
        }
        
        public static long Range(long min, long max)
        {
            if (min >= max)
            {
                throw new ArgumentException("max 必须大于 min");
            }

            // 生成一个介于 [0, 1) 的随机比例
            byte[] buffer = new byte[8];
            _random.NextBytes(buffer);
            double randomDouble = (double)BitConverter.ToUInt64(buffer, 0) / ulong.MaxValue;

            // 将比例映射到 [min, max] 范围
            long range = max - min;
            return (long)(randomDouble * range) + min;
        }
        
        public static long RandomLong()
        {
            // 生成8字节的随机数组
            byte[] buffer = new byte[8];
            _random.NextBytes(buffer);
        
            // 将字节数组转换为 long
            long randomLong = BitConverter.ToInt64(buffer, 0);
            return randomLong;
        }

        /// <summary>
        /// [0f, 1f]内判断
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static bool True(float val)
        {
            return val >= UnityRandom.value;
        }
        
        public static bool True(float weight, float total)
        {
            if (total <= weight) return true;
            return weight >= UnityRandom.Range(0f, total);
        }
        
        public static bool True(int weight, int total)
        {
            if (total <= weight) return true;
            return weight >= UnityRandom.Range(0f, total);
        }

        /// <summary>
        /// 随机抽取
        /// </summary>
        /// <param name="elements">抽取池</param>
        /// <returns></returns>
        public static T RandomSelection<T>(T[] elements)
        {
            if (elements == null || elements.Length == 0) return default;
            if (elements.Length == 1) return elements[0];
            int randomIndex = UnityRandom.Range(0, elements.Length);
            return elements[randomIndex];
        }
        
        /// <summary>
        /// 随机抽取
        /// </summary>
        /// <param name="elements">抽取池</param>
        /// <returns></returns>
        public static T RandomSelection<T>(IList<T> elements)
        {
            if (elements == null || elements.Count == 0) return default;
            if (elements.Count == 1) return elements[0];
            int randomIndex = UnityRandom.Range(0, elements.Count);
            return elements[randomIndex];
        }

        /// <summary>
        /// 带权重的随机抽取
        /// </summary>
        /// <param name="elements">抽取池</param>
        /// <param name="weights">对应权重</param>
        /// <returns>被抽中元素</returns>
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
        /// 带权重的随机抽取
        /// </summary>
        /// <param name="elements">抽取池</param>
        /// <param name="weights">对应权重</param>
        /// <returns>被抽中元素</returns>
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
        /// 带权重的随机抽取
        /// </summary>
        /// <param name="itemWeightPair">抽取池和对应权重</param>
        /// <returns>被抽中元素</returns>
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
        /// 不重复抽取
        /// </summary>
        /// <param name="elements">抽取池</param>
        /// <param name="count">抽取数</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
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
        /// 不重复抽取
        /// </summary>
        /// <param name="elements">抽取池</param>
        /// <param name="count">抽取数</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
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

            public WeightedElement(T element, int weight)
            {
                Element = element;
                Weight = weight;
            }
        }

        /// <summary>
        /// 带权重不重复抽取
        /// </summary>
        /// <param name="ItemWeightsPair">元素与权重</param>
        /// <param name="count">抽取数</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static List<T> WeightedRandomSelectionWithoutDuplicates<T>(Dictionary<T, int> ItemWeightsPair, int count)
        {
            if (ItemWeightsPair == null || ItemWeightsPair.Count <= 0 || count <= 0) return new List<T>();
            if (count > ItemWeightsPair.Count) return ItemWeightsPair.Keys.ToList();
            List<T> result = new List<T>();
            // 创建一个带权重的元素列表
            List<WeightedElement<T>> weightedElements = ItemWeightsPair.Select(item => new WeightedElement<T>(item.Key, item.Value)).ToList();
            // 按权重从高到低排序
            weightedElements.Sort((a, b) => b.Weight.CompareTo(a.Weight));
            // 随机抽取元素
            for (int i = 0; i < count; i++)
            {
                WeightedElement<T> selected = weightedElements[0];
                // 计算总权重
                float totalWeight = weightedElements.Aggregate<WeightedElement<T>, float>(0, (current, element) => current + element.Weight);
                // 随机选择一个元素
                float randomValue = UnityRandom.Range(0, totalWeight);
                // 根据随机值确定选中的元素
                foreach (WeightedElement<T> element in weightedElements)
                {
                    randomValue -= element.Weight;
                    if (randomValue <= 0)
                    {
                        selected = element;
                        break;
                    }
                }
                // 添加选中的元素到结果列表，并从权重列表中移除
                result.Add(selected.Element);
                weightedElements.Remove(selected);
            }
            return result;
        }
    }
}