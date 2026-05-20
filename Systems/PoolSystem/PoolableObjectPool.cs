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

        public override bool Release(IPoolable obj)
        {
            if (_stack == null)
            {
                obj.Dispose();
                return false;
            }
            if (IsInPool(obj)) return true;
            obj.OnDeSpawn();
            if (count >= _maxSize)
            {
                obj.Dispose();
                return false;
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

        public override bool Release(T obj)
        {
            if (_stack == null)
            {
                obj.Dispose();
                return false;
            }
            if (IsInPool(obj)) return true;
            obj.OnDeSpawn();
            if (count >= _maxSize)
            {
                obj.Dispose();
                return false;
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