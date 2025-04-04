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