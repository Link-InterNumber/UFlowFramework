using System;
using System.Collections.Generic;

namespace PowerCellStudio
{
    public class IndexGetter : SingletonBase<IndexGetter>
    {
        private Dictionary<Type, long> _cache = new Dictionary<Type, long>();

        public long Get<T>()
        {
            var t = typeof(T);
            if (_cache.TryGetValue(t, out var cur))
            {
                if (cur == long.MaxValue) cur = 0;
                cur++;
                _cache[t] = cur;
                return cur;
            }
            _cache.Add(t, 1);
            return 1;
        }
        
        public long Get(Type t)
        {
            if (_cache.TryGetValue(t, out var cur))
            {
                if (cur == long.MaxValue) cur = 0;
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