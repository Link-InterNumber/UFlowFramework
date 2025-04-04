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
            _sparse = Enumerable.Repeat(0L, _pageSize * 3).ToArray();
        }

        public SparseSet(int pageSize)
        {
            _pageSize = pageSize;
            _count = 0;
            _dense = new T[_pageSize * 3];
            _sparse = Enumerable.Repeat(0L, _pageSize * 3).ToArray();
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
            // var page = GetPage(index);
            // var pageIndex = GetPageIndex(index);
            if (_sparse.Length - 1 < index)
            {
                var lengthBefore = _sparse.Length;
                Array.Resize(ref _sparse, Mathf.CeilToInt((index + 1f) / _pageSize) * _pageSize);
                for (int i = _sparse.Length - 1; i >= lengthBefore; i--)
                {
                    _sparse[i] = 0L;
                }
            }
            if(_count >= _dense.Length) Array.Resize(ref _dense, _dense.Length + _pageSize);
            var realIndex = _sparse[index];
            if (realIndex < 1)
            {
                _dense[_count] = item;
                _sparse[index] = _count;
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
            if (item == null || _count == 0) return false;
            var index = item.index;
            // var page = GetPage(index);
            if (_sparse.Length - 1 < index || _sparse[index] == 0L) return false;
            // var pageIndex = GetPageIndex(index);
            // var pageData = _sparse[page];
            // if (pageData == null) return false;
            var realIndex = _sparse[index];
            // if (realIndex < 0) return false;
            var data = _dense[realIndex];
            return data != null && data.index == index;
        }
        
        public bool Contains(int index)
        {
            // var page = GetPage(index);
            if (_sparse.Length - 1 < index || _sparse[index] == 0L) return false;
            // var pageIndex = GetPageIndex(index);
            // var pageData = _sparse[page];
            // if (pageData == null) return false;
            var realIndex = _sparse[index];
            // if (realIndex < 0) return false;
            var data = _dense[realIndex];
            return data != null && data.index == index;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            var loopCount = array.Length;
            for (int i = 0; i < loopCount; i++)
            {
                if (i > _dense.Length - 1) break;
                array[i] = _dense[i];
            }
        }

        public bool Remove(T item)
        {
            return item != null && Remove(item.index);
        }

        public bool Remove(long itemIndex)
        {
            if (itemIndex < 1 || _count == 0) return false;
            // var page = GetPage(itemIndex);
            if (_sparse.Length - 1 < itemIndex || _sparse[itemIndex] == 0L) return false;
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
            _sparse[last.index] = realIndex;
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