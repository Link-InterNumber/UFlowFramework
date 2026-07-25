using System;

namespace PowerCellStudio
{
    public class PoolableObjectPool : LinkPool<IPoolable>
    {
        public PoolableObjectPool(Func<IPoolable> createFun, int maxSize, int initSize) : base(createFun, maxSize, initSize)
        { }
        
        public override IPoolable Get()
        {
            var obj = base.Get();
            obj.LinkPool = this;
            obj.OnSpawn();
            return obj;
        }

        /// <summary>
        /// 将对象放入池中，返回false时需要会调用Dispose，外部需要置空 /
        /// Put the object into the pool. When it returns false, Dispose needs to be called, and the external reference needs to be set to null.
        /// </summary>
        /// <param name="obj">对象池道具 / Object to be released</param>
        /// <returns>是否回收成功 / Whether the object was successfully recycled</returns>
        public override bool Release(IPoolable obj)
        {
            if (obj == null) return false;
            if (_stack == null)
            {
                // obj.Dispose();
                return false;
            }
            if (IsInPool(obj)) return true;
            obj.OnDeSpawn();
            if (count >= _maxSize)
            {
                obj.Dispose();
                return true;
            }
            _stack.Push(obj);
            _set.Add(obj);
            return true;
        }

        public override void Clear()
        {
            foreach (var poolable in _stack)
            {
                poolable.Dispose();
            }
            base.Clear();
        }
    }

    public class PoolableObjectPool<T> : LinkPool<T> where T : class, IPoolable
    {
        public PoolableObjectPool(Func<T> createFun, int maxSize, int initSize) : base(createFun, maxSize, initSize)
        { }
        
        public override T Get()
        {
            var obj = base.Get();
            obj.LinkPool = this as LinkPool<IPoolable>;
            obj.OnSpawn();
            return obj;
        }

        /// <summary>
        /// 将对象放入池中，返回false时需要会调用Dispose，外部需要置空 / 
        /// Put the object into the pool. When it returns false, Dispose needs to be called, and the external reference needs to be set to null.
        /// </summary>
        /// <param name="obj">对象池道具 / Object to be released</param>
        /// <returns>是否回收成功 / Whether the object was successfully recycled</returns>
        public override bool Release(T obj)
        {
            if (obj == null) return false;
            if (_stack == null)
            {
                // obj.Dispose();
                return false;
            }
            if (IsInPool(obj)) return true;
            obj.OnDeSpawn();
            if (count >= _maxSize)
            {
                obj.Dispose();
                return true;
            }
            _stack.Push(obj);
            _set.Add(obj);
            return true;
        }

        public override void Clear()
        {
            foreach (var poolable in _stack)
            {
                poolable.Dispose();
            }
            base.Clear();
        }
    }
}