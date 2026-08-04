using System;
using System.Collections.Generic;

namespace PowerCellStudio
{
    public class IndexGetter
    {
        private static IndexGetter _instance;

        public static IndexGetter instance => _instance ??= new IndexGetter();
        
        private Dictionary<Type, int> _cache = new Dictionary<Type, int>();

        /// <summary>
        /// 获取类型T的索引，从1开始，每次调用都会递增，直到达到int.MaxValue后重置
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public int Get<T>()
        {
            var t = typeof(T);
            if (_cache.TryGetValue(t, out var cur))
            {
                if (cur == int.MaxValue) cur = 0;
                cur++;
                _cache[t] = cur;
                return cur;
            }
            _cache.Add(t, 1);
            return 1;
        }
        
        /// <summary>
        /// 获取类型T的索引，从1开始，每次调用都会递增，直到达到int.MaxValue后重置
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public int Get(Type t)
        {
            if (_cache.TryGetValue(t, out var cur))
            {
                if (cur == int.MaxValue) cur = 0;
                cur++;
                _cache[t] = cur;
                return cur;
            }
            _cache.Add(t, 1);
            return 1;
        }

        /// <summary>
        /// 重置类型T的索引
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void Reset<T>()
        {
            _cache[typeof(T)] = 0;
        }

        /// <summary>
        /// 重置类型t的索引
        /// </summary>
        /// <param name="t"></param>
        public void Reset(Type t)
        {
            _cache[t] = 0;
        }
        
        /// <summary>
        /// 重置所有类型的索引
        /// </summary>
        public void ResetAll()
        {
            _cache.Clear();
        }
    }
}