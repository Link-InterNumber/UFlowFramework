using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        
        private T[] _dense;
        private long[] _sparse; 
        private int _pageSize = 128;
        private int _count;

        public SparseSet()
        {
            _count = 0;
            _dense = new T[_pageSize * 3];
            _sparse = new long[_pageSize * 3];
        }

        public SparseSet(int pageSize)
        {
            _pageSize = pageSize;
            _count = 0;
            _dense = new T[_pageSize * 3];
            _sparse = new long[_pageSize * 3];
        }

        // private int GetPage(int index)
        // {
        //     return Mathf.FloorToInt(index * 1f / _pageSize);
        // }
        //
        // private int GetPageIndex(int index)
        // {
        //     return index % _pageSize;
        // }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
            {
                yield return _dense[i];
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
            if (index < 0) throw new ArgumentOutOfRangeException();
            
            if (index >= _sparse.Length)
            {
                int newSize = Mathf.CeilToInt((index + 1f) / _pageSize) * _pageSize;
                Array.Resize(ref _sparse, newSize);
            }
            
            if (_count >= _dense.Length)
                Array.Resize(ref _dense, _dense.Length + _pageSize);
            
            long existing = _sparse[index];
            if (existing == 0)
            {
                _dense[_count] = item;
                _sparse[index] = _count + 1; // 存储索引+1
                _count++;
            }
            else
            {
                _dense[existing - 1] = item; // 使用存储的索引-1
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
            if (item == null || _count == 0) return false;
            var index = item.index;
            return Contains((int)index);
        }
        
        public bool Contains(int index)
        {
            if (index < 0) return false;
            return index < _sparse.Length && 
                _sparse[index] != 0 && 
                _dense[_sparse[index] - 1].index == index;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException();
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException();
            if (array.Length - arrayIndex < _count) throw new ArgumentException();
            
            Array.Copy(_dense, 0, array, arrayIndex, _count);
        }

        public bool Remove(T item)
        {
            return item != null && Remove(item.index);
        }

        public bool Remove(long itemIndex)
        {
            if (itemIndex < 0 || _count == 0) return false;
            if (itemIndex >= _sparse.Length) return false;
            
            long storedIndex = _sparse[itemIndex];
            if (storedIndex == 0) return false;
            // var pageIndex = GetPageIndex(itemIndex);
            var realIndex = _sparse[itemIndex];
            if (realIndex < 0 || _dense.Length - 1 < realIndex) return false;
            
            if (_count == 1)
            {
                _dense[0] = default;
                _sparse[itemIndex] = 0L;
                _count--;
                return true;
            }
            
            var last = _dense[_count - 1];
            _dense[realIndex] = last;
            _dense[_count - 1] = default;
            _sparse[itemIndex] = 0L;
            // var lastPage = GetPage(last.Index);
            // var lastPageIndex = GetPageIndex(last.Index);
            _sparse[last.index] = realIndex + 1;
            _count--;
            return true;
        }

        public T FindOrDefault(long itemIndex)
        {
            if (itemIndex < 1) return default;
            // var page = GetPage(itemIndex);
            if (_sparse.Length - 1 < itemIndex || _sparse[itemIndex] == 0L) return default;
            // var pageIndex = GetPageIndex(itemIndex);
            var realIndex = _sparse[itemIndex];
            return realIndex < 1L ? default : _dense[realIndex];
        }

        public T this[int index] => FindOrDefault(index);

        public int Count => _count;

        public bool IsReadOnly => false;
    }
}