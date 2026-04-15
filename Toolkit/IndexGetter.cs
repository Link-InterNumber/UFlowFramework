using System;
using System.Collections.Generic;

namespace PowerCellStudio
{
    public class IndexGetter : SingletonBase<IndexGetter>
    {
        private Dictionary<Type, int> _cache = new Dictionary<Type, int>();

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

        public void Reset<T>()
        {
            _cache[typeof(T)] = 0;
        }

        public void Reset(Type t)
        {
            _cache[t] = 0;
        }
        
        public void ResetAll()
        {
            _cache.Clear();
        }
    }
}