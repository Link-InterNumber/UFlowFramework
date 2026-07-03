using System;
using System.Collections;
using System.Collections.Generic;

namespace UFlowFramework
{
    /// <summary>
    /// List实现的二叉最大堆。
    /// Binary max heap backed by a List.
    /// </summary>
    public class MaxHeap<T> : ICollection<T>
        where T : IComparable<T>
    {
        private const int DefaultCapacity = 4;

        private readonly List<T> _items;

        public MaxHeap() : this(DefaultCapacity)
        {
        }

        public MaxHeap(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _items = new List<T>(capacity);
        }

        public MaxHeap(IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            _items = collection is ICollection<T> c ? new List<T>(c.Count) : new List<T>(DefaultCapacity);
            _items.AddRange(collection);

            Heapify();
        }

        public int Count => _items.Count;

        public bool IsReadOnly => false;

        public int Capacity
        {
            get => _items.Capacity;
            set
            {
                if (value < Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _items.Capacity = value;
            }
        }

        public T this[int index]
        {
            get
            {
                CheckIndex(index);
                return _items[index];
            }
            set
            {
                CheckIndex(index);
                _items[index] = value;
                FixHeapAt(index);
            }
        }

        public T Peek()
        {
            return _items[0];
        }

        public bool TryPeek(out T item)
        {
            if (Count == 0)
            {
                item = default;
                return false;
            }

            item = _items[0];
            return true;
        }

        public T Pop()
        {
            if (Count == 0)
            {
                throw new InvalidOperationException("Heap is empty.");
            }

            var result = _items[0];
            RemoveRoot();
            return result;
        }

        public bool TryPop(out T item)
        {
            if (Count == 0)
            {
                item = default;
                return false;
            }

            item = _items[0];
            RemoveRoot();
            return true;
        }

        public void Add(T item)
        {
            _items.Add(item);
            SiftUp(Count - 1);
        }

        public void AddRange(IEnumerable<T> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            foreach (var item in items)
            {
                _items.Add(item);
            }

            Heapify();
        }

        public void Clear()
        {
            _items.Clear();
        }

        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            _items.CopyTo(0, array, arrayIndex, Count);
        }

        /// <summary>
        /// 移除性能较差，谨慎使用。
        /// Remove is a slow operation, use with caution.
        /// </summary>
        public bool Remove(T item)
        {
            var index = IndexOf(item);
            if (index < 0)
            {
                return false;
            }

            RemoveAt(index);

            return true;
        }

        /// <summary>
        /// 重新构建堆，用于元素内部值被外部修改后主动恢复堆结构。
        /// Rebuilds the heap, allowing callers to restore heap order after mutating contained item values.
        /// </summary>
        public void RebuildHeap()
        {
            Heapify();
        }

        public int IndexOf(T item)
        {
            for (var i = 0; i < Count; i++)
            {
                if (item.Equals(_items[i]))
                {
                    return i;
                }

            }

            return -1;
        }

        public void RemoveAt(int index)
        {
            CheckIndex(index);
            var lastIndex = Count - 1;
            if (index == lastIndex)
            {
                _items.RemoveAt(lastIndex);
                return;
            }

            _items[index] = _items[lastIndex];
            _items.RemoveAt(lastIndex);
            FixHeapAt(index);
        }

        public void TrimExcess()
        {
            if (Count == _items.Capacity)
            {
                return;
            }

            _items.TrimExcess();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void RemoveRoot()
        {
            var lastIndex = Count - 1;
            if (lastIndex == 0)
            {
                _items.RemoveAt(0);
                return;
            }

            _items[0] = _items[lastIndex];
            _items.RemoveAt(lastIndex);
            SiftDown(0);
        }

        private void Heapify()
        {
            for (var i = (Count >> 1) - 1; i >= 0; i--)
            {
                SiftDown(i);
            }
        }

        private void FixHeapAt(int index)
        {
            if (index > 0 && Greater(_items[index], _items[(index - 1) >> 1]))
            {
                SiftUp(index);
            }
            else
            {
                SiftDown(index);
            }
        }

        private void SiftUp(int index)
        {
            var item = _items[index];
            while (index > 0)
            {
                var parentIndex = (index - 1) >> 1;
                var parent = _items[parentIndex];
                if (!Greater(item, parent))
                {
                    break;
                }

                _items[index] = parent;
                index = parentIndex;
            }

            _items[index] = item;
        }

        private void SiftDown(int index)
        {
            var item = _items[index];
            var half = Count >> 1;
            while (index < half)
            {
                var childIndex = index * 2 + 1;
                var rightIndex = childIndex + 1;
                var child = _items[childIndex];

                if (rightIndex < Count && Greater(_items[rightIndex], child))
                {
                    childIndex = rightIndex;
                    child = _items[childIndex];
                }

                if (!Greater(child, item))
                {
                    break;
                }

                _items[index] = child;
                index = childIndex;
            }

            _items[index] = item;
        }

        private bool Greater(T a, T b)
        {
            return a.CompareTo(b) > 0;
        }

        private void CheckIndex(int index)
        {
            if ((uint)index >= (uint)Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }
}
