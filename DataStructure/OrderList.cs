using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PowerCellStudio
{
    // 测试后，和List<IComparable>比较，插入性能慢7.4倍，二分查找性能和枚举迭代性能相当，移除性能提高约90倍
    // 目前看没有合适的使用场景
    // After testing, compared with List<IComparable>, insertion performance is 7.4 times slower, binary search & enumeration iteration performance is comparable to List<IComparable>, and removal performance is improved by about 90 times.
    // Currently, there seems to be no suitable use case.
    public class OrderList<T>: IList<T> where T : IComparable<T>
    {
        private T[] _rawData;
        private int _count;

        public OrderList(int size = 128)
        {
            _rawData = new T[size];
            _count = 0;
        }

        public OrderList(IList<T> source)
        {
            if(source == null || source.Count == 0)
            {
                _rawData = new T[128];
                _count = 0;
                return;
            }
            source = source.OrderBy(o => o).ToList();
            _rawData = new T[source.Count];
            _count = source.Count;
            for (var i = 0; i < _count; i++)
            {
                _rawData[i] = source[i];
            }
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return _rawData[index];
            }
            set => throw new NotSupportedException("Setting an item at a specific index is not supported. Use Add() to ensure order is maintained.");
        }

        public Span<T> AsSpan()
        {
            return _rawData.AsSpan(0, _count);
        }

        public int Count => _count;

        public bool IsReadOnly => false;

        public void Add(T item)
        {
            if (item == null) return;
            if (_count == _rawData.Length)
            {
                Resize(Math.Max(128, _rawData.Length));
            }
            if (_count == 0)
            {
                _rawData[0] = item;
                _count++;
                return;
            }
            if (_count > 0 && item.CompareTo(_rawData[_count - 1]) > 0)
            {
                _rawData[_count] = item;
                _count++;
                return;
            }

            int index = Array.BinarySearch(_rawData, 0, _count, item);
            if (index < 0)
            {
                index = ~index;
            }

            Array.Copy(_rawData, index, _rawData, index + 1, _count - index);
            _rawData[index] = item;
            _count++;
        }

        public void AddRange(IEnumerable<T> items)
        {
            if (items == null) return;
            foreach (var item in items)
            {
                Add(item);
            }
        }

        private void Resize(int expent = 128)
        {
            Array.Resize(ref _rawData, _rawData.Length + expent);
        }

        public void Clear()
        {
            Array.Clear(_rawData, 0, _count);
            _count = 0;
        }

        public bool Contains(T item)
        {
            int index = Array.BinarySearch(_rawData, 0, _count, item);
            return index > -1;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(_rawData, 0, array, arrayIndex, _count);
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
            {
                yield return _rawData[i];
            }
        }

        public int IndexOf(T item)
        {
            int index = Array.BinarySearch(_rawData, 0, _count, item);
            return index >= 0 ? index : -1;
        }

        [Obsolete("Inserting at a specific index is not supported. Use Add() to ensure order is maintained.")]
        public void Insert(int index, T item)
        {
            throw new NotSupportedException("Inserting at a specific index is not supported. Use Add() to ensure order is maintained.");
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index < 0)
            {
                return false;
            }

            RemoveAt(index);
            return true;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (index == _count - 1)
            {
                _rawData[_count - 1] = default;
                _count--;
                return;
            }
            Array.Copy(_rawData, index + 1, _rawData, index, _count - index - 1);
            _rawData[_count - 1] = default; // Remove reference to the last item
            _count--;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}