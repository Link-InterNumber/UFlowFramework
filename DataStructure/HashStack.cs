using System.Collections;
using System.Collections.Generic;

namespace PowerCellStudio
{
    public class HashStack<T> : IEnumerable<T>
    {
        private HashSet<T> _hash = new HashSet<T>();
        private Stack<T> _stack = new Stack<T>();
        
        public int Count => _stack.Count;
        
        public void Push(T item)
        {
            if (_hash.Contains(item))
            {
                RemoveItemFromStack(item);
                _stack.Push(item);
                return;
            }
            _hash.Add(item);
            _stack.Push(item);
        }
        
        public T Pop()
        {
            var item = _stack.Pop();
            _hash.Remove(item);
            return item;
        }
        
        public T Peek()
        {
            return _stack.Peek();
        }
        
        public void Clear()
        {
            _hash.Clear();
            _stack.Clear();
        }
        
        public bool Contains(T item)
        {
            return _hash.Contains(item);
        }
        
        public bool Remove(T item)
        {
            if (!_hash.Contains(item))
            {
                return false;
            }
            _hash.Remove(item);
            RemoveItemFromStack(item);
            return true;
        }

        private void RemoveItemFromStack(T item)
        {
            var newStack = new Stack<T>();
            while (_stack.Count > 0)
            {
                var currentItem = _stack.Pop();
                if (currentItem.Equals(item))
                {
                    continue;
                }
                newStack.Push(currentItem);
            }
            while (newStack.Count > 0)
            {
                _stack.Push(newStack.Pop());
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _stack.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}