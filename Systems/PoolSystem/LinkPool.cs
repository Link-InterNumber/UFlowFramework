using System;
using System.Collections.Generic;

namespace PowerCellStudio
{
    public class LinkPool<T> : IDisposable where T : class
    {
        protected Stack<T> _stack = new Stack<T>();
        protected HashSet<T> _set = new HashSet<T>();
        protected Func<T> _createFun;
        protected int _maxSize = 10;

        /// <summary>
        /// 池内对象数量
        /// Number of objects in the pool.
        /// </summary>
        public int count => _stack.Count;

        /// <summary>
        /// 最大数量
        /// Maximum number of objects in the pool.
        /// </summary>
        public int maxSize => _maxSize;

        /// <summary>
        /// 设置池的最大数量
        /// Set the maximum size of the pool.
        /// </summary>
        /// <param name="newValue">新的最大值 / New maximum value</param>
        public void SetMaxSize(int newValue)
        {
            _maxSize = Math.Max(1, newValue);
        }

        /// <summary>
        /// 对象池的构造函数
        /// Constructor for the object pool.
        /// </summary>
        /// <param name="createFun">生成方法 / Method to create objects</param>
        /// <param name="maxSize">最大数量 / Maximum size of the pool</param>
        /// <param name="initSize">初始数量 / Initial size of the pool</param>
        public LinkPool(Func<T> createFun, int maxSize, int initSize)
        {
            _createFun = createFun;
            _maxSize = maxSize;
            for (int i = 0; i < initSize; i++)
            {
                _stack.Push(_createFun());
            }
        }

        protected LinkPool() { _createFun = () => new T(); }

        /// <summary>
        /// 从对象池中获取对象
        /// Get an object from the pool.
        /// </summary>
        /// <returns>对象实例 / Instance of the object</returns>
        public virtual T Get()
        {
            if (_stack.Count == 0)
            {
                var obj = _createFun();
                return obj;
            }
            var poped = _stack.Pop();
            _set.Remove(poped);
            return poped;
        }

        /// <summary>
        /// 将对象放入池中，返回false时需要手动销毁
        /// Release an object back to the pool. Returns false if manual destruction is needed.
        /// </summary>
        /// <param name="obj">对象池道具 / Object to be released</param>
        /// <returns>是否回收成功 / Whether the object was successfully recycled</returns>
        public virtual bool Release(T obj)
        {
            if (_stack == null) return false;
            if (IsInPool(obj)) return true;
            if (count >= _maxSize) return false;
            _stack.Push(obj);
            _set.Add(obj);
            return true;
        }

        /// <summary>
        /// 清除对象池内的对象
        /// Clear all objects in the pool.
        /// </summary>
        public virtual void Clear()
        {
            _stack.Clear();
            _set.Clear();
        }

        /// <summary>
        /// 销毁对象池
        /// Dispose of the object pool.
        /// </summary>
        public virtual void Dispose()
        {
            Clear();
            _stack = null;
            _set = null;
        }

        /// <summary>
        /// 对象是否在池内
        /// Check if an object is in the pool.
        /// </summary>
        /// <param name="item">待检测对象 / Object to check</param>
        /// <returns>是否在池内 / Whether the object is in the pool</returns>
        public bool IsInPool(T item)
        {
            return _set.Contains(item);
        }

        /// <summary>
        /// 是否可以放入池中
        /// Determine if an object can be placed in the pool.
        /// </summary>
        /// <param name="item">待检测对象 / Object to check</param>
        /// <returns>是否可以放入池 / Whether the object can be placed in the pool</returns>
        public virtual bool CanPool(T item)
        {
            return !item.GetType().IsSubclassOf(typeof(T));
        }
    }
}