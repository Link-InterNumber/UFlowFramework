using System;

namespace PowerCellStudio
{
    public abstract class PoolObject: IPoolable, IDisposable
    {
        public LinkPool<IPoolable> LinkPool { get; set; }

        public bool spawned => (!LinkPool?.IsInPool(this)) ?? false;
        
        public abstract void OnSpawn();

        public virtual void DeSpawn()
        {
            if (LinkPool != null && LinkPool.Release(this)) return;
            OnDeSpawn();
            Dispose();
        }

        public abstract void OnDeSpawn();

        public virtual void Dispose()
        {
            LinkPool = null;
        }
    }
}