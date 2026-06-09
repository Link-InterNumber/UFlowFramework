using System;
using System.Collections;
using System.Collections.Generic;

namespace RVO.JobSystem
{
    internal class SparseSet : ICollection<ManagedAgentState>
    {
        /// <summary>
        /// 存放元素的数组
        /// </summary>
        private ManagedAgentState[] _dense;

        /// <summary>
        /// 稀疏数组，存放元素在_dense中的索引
        /// </summary>
        private int[] _sparse;

        private int _pageSize = 128;
        private int _count;

        public SparseSet()
        {
            _count = 0;
            _dense = new ManagedAgentState[Math.Max(4, _pageSize / 2)];
            _sparse = new int[_pageSize];
        }

        public SparseSet(int pageSize)
        {
            _pageSize = pageSize;
            _count = 0;
            _dense = new ManagedAgentState[Math.Max(4, _pageSize / 2)];
            _sparse = new int[_pageSize];
        }

        /// <summary>
        /// 需要极限控制内存时，可以设置密实数组长度，例如 最大Id范围 / 3 来初始化密实数组
        /// 这种方式会比直接使用 T[最大Id范围] 要节省内存
        /// </summary>
        public SparseSet(int denseSize, int sparseSize, int pageSize)
        {
            _pageSize = pageSize;
            _count = 0;
            _dense = new ManagedAgentState[denseSize];
            _sparse = new int[sparseSize];
        }

        public IEnumerator<ManagedAgentState> GetEnumerator()
        {
            if (_count == 0)
                yield break;
            for (int i = 0; i < _count; i++)
            {
                yield return _dense[i + 1];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(ManagedAgentState item)
        {
            if (item == null) return;
            var index = item.id;
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
                    Array.Resize(ref _dense, _dense.Length * 2);
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

        public void Clear()
        {
            _count = 0;
            Array.Clear(_dense, 0, _dense.Length);
            Array.Clear(_sparse, 0, _sparse.Length);
        }

        public bool Contains(ManagedAgentState item)
        {
            if (item == null) return false;
            var index = item.id;
            return Contains(index);
        }

        public bool Contains(int itemIndex)
        {
            if (itemIndex < 0) return false;
            if (itemIndex >= _sparse.Length) return false;
            if (_count == 0) return false;
            var realIndex = _sparse[itemIndex];
            if (realIndex == 0 || realIndex > _count) return false;
            return true; // _dense[realIndex] != null && _dense[realIndex].index == index;
        }

        public void CopyTo(ManagedAgentState[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException();
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException();
            if (array.Length - arrayIndex < _count) throw new ArgumentException();

            Array.Copy(_dense, 1, array, arrayIndex, _count);
        }

        public bool Remove(ManagedAgentState item)
        {
            return item != null && Remove(item.id);
        }

        public bool Remove(int itemIndex)
        {
            if (itemIndex < 0 || _count == 0) return false;
            if (itemIndex >= _sparse.Length) return false;
            var realIndex = _sparse[itemIndex];
            if (realIndex == 0) return false;
            _sparse[itemIndex] = 0;
            // 末尾元素可以直接移除，不要操作数据移动
            if (realIndex == _count)
            {
                _dense[_count] = null;
                _count--;
                return true;
            }

            // 使用末尾元素填充空位，并消除末尾元素引用
            var lastItem = _dense[_count];
            _dense[realIndex] = lastItem;
            _sparse[lastItem.id] = realIndex;

            _dense[_count] = null;
            _count--;
            return true;
        }

        public ManagedAgentState FindOrDefault(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex >= _sparse.Length) return null;
            var realIndex = _sparse[itemIndex];
            if (realIndex == 0 || realIndex > _count) return null;
            return _dense[realIndex];
        }

        public int DenseIndexOf(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex >= _sparse.Length) return -1;
            var realIndex = _sparse[itemIndex];
            if (realIndex == 0 || realIndex > _count) return -1;
            return realIndex - 1;
        }

        /// <summary>
        /// 按照密实数组实际的索引位置
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public ManagedAgentState this[int index]
        {
            get
            {
                if (index < 0 || index >= _count) throw new KeyNotFoundException();
                return _dense[index + 1];
            }
        }

        public int Count => _count;

        public bool IsReadOnly => false;
    }
}