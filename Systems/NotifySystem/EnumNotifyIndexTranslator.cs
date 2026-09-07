using System;
using System.Collections.Generic;

namespace PowerCellStudio
{
    public class EnumNotifyIndexTranslator<T> : INotifyIndexTranslator<T>
        where T : Enum
    {
        private readonly T[] _values;
        private readonly Dictionary<T, int> _indexMap;
        
        public EnumNotifyIndexTranslator()
        {
            _values = (T[])Enum.GetValues(typeof(T));
            _indexMap = new Dictionary<T, int>(_values.Length);

            for (int i = 0; i < _values.Length; i++)
            {
                if (_indexMap.ContainsKey(_values[i]))
                    continue;
                _indexMap.Add(_values[i], i);
            }
        }   
        
        public int ToIndex(T data)
        {
            if (_indexMap.TryGetValue(data, out int index))
                return index;
            throw new ArgumentOutOfRangeException(nameof(data), data, $"Enum value '{data}' is not defined in {typeof(T).FullName}.");
        }

        public T ByIndex(int index)
        {
            if ((uint)index >= (uint)_values.Length)
                throw new ArgumentOutOfRangeException(nameof(index), index, $"Index must be in range [0, {_values.Length - 1}].");
            return _values[index];
        }

        public int GetNotifyCount()
        {
            return _values.Length;
        }
    }
}