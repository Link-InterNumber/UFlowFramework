using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PowerCellStudio
{
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
            if (_count == _rawData.Length)
            {
                Resize();
            }

            int index = Array.BinarySearch(_rawData, 0, _count, item);
            if (index < 0)
            {
                index = ~index;
            }

            // Shift elements to accommodate the new item
            Array.Copy(_rawData, index, _rawData, index + 1, _count - index);
            _rawData[index] = item;
            _count++;
        }

        public void AddRange(IEnumerable<T> items)
        {
            if (items == null) return;
            var list = items.ToList();
            list.Sort();
            if (_count < _rawData.Length + list.Count)
            {
                Resize(Math.Max(128, list.Count));
            }

            int index = Array.BinarySearch(_rawData, 0, _count, list[0]);
            if (index < 0)
            {
                index = ~index;
            }

            // Shift elements to accommodate the new item
            Array.Copy(_rawData, index, _rawData, index + list.Count, _count - index);

            for (var i = index; i < list.Count; i++)
            {
                _rawData[i] = list[i - index];
            }
            _count += list.Count;
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