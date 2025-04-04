using System;

namespace PowerCellStudio
{
    public abstract class PoolObject: IPoolable, IDisposable
    {
        public LinkPool<IPoolable> LinkPool { get; set; }
        
        public abstract void OnSpawn();

        public virtual void DeSpawn()
        {
            LinkPool.Release(this);
        }

        public abstract void OnDeSpawn();

        public virtual void Dispose() { }
    }
}