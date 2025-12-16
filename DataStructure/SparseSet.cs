using System;
using System.Collections;
using System.Collections.Generic;

namespace PowerCellStudio
{
    public interface IIndex
    {
        public long index { get;  }
    }

    public class SparseSet<T> : ICollection<T> where T : IIndex
    {
        public static SparseSet<T> Empty()
        {
            return new SparseSet<T>(1);
        }

        /// <summary>
        /// 存放元素的数组
        /// </summary>
        private T[] _dense;
        /// <summary>
        /// 稀疏数组，存放元素在_dense中的索引
        /// </summary>
        private long[] _sparse; 
        private int _pageSize = 128;
        private int _count;

        public SparseSet()
        {
            _count = 0;
            _dense = new T[_pageSize];
            _sparse = new long[_pageSize];
        }

        public SparseSet(int pageSize)
        {
            _pageSize = pageSize;
            _count = 0;
            _dense = new T[_pageSize];
            _sparse = new long[_pageSize];
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (_count == 0)
                yield break;
            for (int i = 0; i < _count; i++)
            {
                yield return _dense[i+1];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            if (item == null) return;
            var index = item.index;
            if (index < 0) return;
            if (index >= _sparse.Length)
            {
                var newSize = ((int)(index / _pageSize) + 1) * _pageSize;
                Array.Resize(ref _sparse, newSize);
            }
            var realIndex = _sparse[index];
            if (realIndex == 0)
            {
                if (_count + 1 >= _dense.Length)
                {
                    Array.Resize(ref _dense, _dense.Length + _pageSize);
                }
                _dense[_count + 1] = item;
                _sparse[index] = _count + 1;
                _count++;
            }
            else
            {
                _dense[realIndex] = item;
            }
        }

        public void  Clear()
        {
            _count = 0;
            Array.Clear(_dense, 0, _dense.Length);
            Array.Clear(_sparse, 0, _sparse.Length);
        }

        public bool Contains(T item)
        {
            if (item == null ) return false;
            var index = item.index;
            return Contains((int)index);
        }
        
        public bool Contains(long index)
        {
            if (index < 0) return false;
            if (index >= _sparse.Length) return false;
            if (_count == 0) return false;
            var realIndex = _sparse[index];
            if (realIndex == 0 || realIndex > _count) return false;
            return true;// _dense[realIndex] != null && _dense[realIndex].index == index;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException();
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException();
            if (array.Length - arrayIndex < _count) throw new ArgumentException();
            
            Array.Copy(_dense, 1, array, arrayIndex, _count);
        }

        public bool Remove(T item)
        {
            return item != null && Remove(item.index);
        }

        public bool Remove(long itemIndex)
        {
            if (itemIndex < 0 || _count == 0) return false;
            if (itemIndex >= _sparse.Length) return false;
            var realIndex = _sparse[itemIndex];
            if (realIndex == 0) return false;
            _sparse[itemIndex] = 0;

            var lastItem = _dense[_count];
            _dense[realIndex] = lastItem;

            _sparse[lastItem.index] = realIndex;

            _dense[_count] = default;
            _count--;
            return true;
        }

        public T FindOrDefault(long itemIndex)
        {
            if (itemIndex < 0 || itemIndex >= _sparse.Length) return default;
            var realIndex = _sparse[itemIndex];
            if (realIndex == 0 || realIndex > _count) return default;
            return _dense[realIndex];
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _sparse.Length) throw new ArgumentOutOfRangeException();
                var realIndex = _sparse[index];
                if (realIndex == 0 || realIndex > _count) throw new KeyNotFoundException();
                return _dense[realIndex];
            }
        } 
        public int Count => _count;

        public bool IsReadOnly => false;
    }
}