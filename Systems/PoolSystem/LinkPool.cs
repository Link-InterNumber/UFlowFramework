using System;
using System.Collections.Generic;

namespace PowerCellStudio
{
    public class LinkPool<T>: IDisposable where T: class
    {
        protected Stack<T> _stack = new Stack<T>();
        protected HashSet<T> _set = new HashSet<T>();
        protected Func<T> _createFun;
        protected int _maxSize;
        /// <summary>
        /// 池内对象数量
        /// </summary>
        public int count => _stack.Count;
        /// <summary>
        /// 最大数量
        /// </summary>
        public int maxSize => _maxSize;

        /// <summary>
        /// 对象池
        /// </summary>
        /// <param name="createFun">生成方法</param>
        /// <param name="maxSize">最大数量</param>
        /// <param name="initSize">初始数量</param>
        public LinkPool(Func<T> createFun, int maxSize, int initSize)
        {
            _createFun = createFun;
            _maxSize = maxSize;
            for (int i = 0; i < initSize; i++)
            {
                _stack.Push(_createFun());
            }
        }

        protected LinkPool() { }

        /// <summary>
        /// 从对象池中获取对象
        /// </summary>
        /// <returns></returns>
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
        /// </summary>
        /// <param name="item">对象池道具</param>
        /// <returns>是否回收成功</returns>
        public virtual bool Release(T obj)
        {
            if (IsInPool(obj)) return true;
            if (count == _maxSize) return false;
            _stack.Push(obj);
            _set.Add(obj);
            return true;
        }

        /// <summary>
        /// 清除对象池内的对象
        /// </summary>
        public virtual void Clear()
        {
            _stack.Clear();
            _set.Clear();
        }

        /// <summary>
        /// 销毁对象池
        /// </summary>
        public virtual void Dispose()
        {
            Clear();
            _stack = null;
            _set = null;
        }
        
        /// <summary>
        /// 对象是否在池内
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool IsInPool(T item)
        {
            return _set.Contains(item);
        }

        /// <summary>
        /// 是否可以放入池中
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public virtual bool CanPool(T item)
        {
            return !item.GetType().IsSubclassOf(typeof(T));
        }
    }
}