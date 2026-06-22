using System;
using System.Collections;
using System.Collections.Generic;

namespace UFlowFramework
{
    /// <summary>
    /// 数组实现的二叉最小堆。
    /// Binary min heap backed by a compact array.
    /// </summary>
    public class MinHeap<T> : IList<T>
        where T : IComparable<T>
    {
        private const int DefaultCapacity = 4;

        private readonly List<T> _items;

        public MinHeap() : this(DefaultCapacity, (IComparer<T>)null)
        {
        }

        public MinHeap(int capacity) : this(capacity, (IComparer<T>)null)
        {
        }

        public MinHeap(IComparer<T> comparer) : this(DefaultCapacity, comparer)
        {
        }

        public MinHeap(Comparison<T> comparison) : this(DefaultCapacity, comparison == null ? null : Comparer<T>.Create(comparison))
        {
        }

        public MinHeap(int capacity, Comparison<T> comparison) : this(capacity, comparison == null ? null : Comparer<T>.Create(comparison))
        {
        }

        public MinHeap(int capacity, IComparer<T> comparer)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _items = new List<T>(capacity);
        }

        public MinHeap(IEnumerable<T> collection) : this(collection, null)
        {
        }

        public MinHeap(IEnumerable<T> collection, IComparer<T> comparer)
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
            if (Count == 0)
            {
                throw new InvalidOperationException("Heap is empty.");
            }

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

            if (items is ICollection<T> c)
            {
                EnsureCapacity(Count + c.Count);
            }

            foreach (var item in items)
            {
                Add(item);
            }
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

        public int IndexOf(T item)
        {
            var comparer = EqualityComparer<T>.Default;
            for (var i = 0; i < Count; i++)
            {
                if (comparer.Equals(_items[i], item))
                {
                    return i;
                }
            }

            return -1;
        }

        public void Insert(int index, T item)
        {
            if (index < 0 || index > Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            Add(item);
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

        public void EnsureCapacity(int capacity)
        {
            if (_items.Capacity >= capacity)
            {
                return;
            }

            var oldCapacity = _items.Capacity;
            var newCapacity = oldCapacity == 0 ? DefaultCapacity : oldCapacity * 2;
            if (newCapacity < 0)
            {
                newCapacity = int.MaxValue;
            }
            if (newCapacity < capacity)
            {
                newCapacity = capacity;
            }

            Capacity = newCapacity;
        }

        public void TrimExcess()
        {
            if (Count == _items.Capacity)
            {
                return;
            }

            _items.TrimExcess();
        }

        public T[] ToArray()
        {
            if (Count == 0)
            {
                return Array.Empty<T>();
            }

            return _items.ToArray();
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
            if (index > 0 && Less(_items[index], _items[(index - 1) >> 1]))
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
                if (!Less(item, parent))
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

                if (rightIndex < Count && Less(_items[rightIndex], child))
                {
                    childIndex = rightIndex;
                    child = _items[childIndex];
                }

                if (!Less(child, item))
                {
                    break;
                }

                _items[index] = child;
                index = childIndex;
            }

            _items[index] = item;
        }

        private bool Less(T a, T b)
        {
            return a.CompareTo(b) < 0;
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
