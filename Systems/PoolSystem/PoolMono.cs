using System;
using UnityEngine;

namespace PowerCellStudio
{
    public abstract class PoolMono : MonoBehaviour, IPoolable, IDisposable
    {
        public LinkPool<IPoolable> LinkPool { get; set; }

        public abstract void OnSpawn();

        public virtual void DeSpawn()
        {
            LinkPool.Release(this);
        }

        public abstract void OnDeSpawn();

        public virtual void Dispose()
        {
        }
    }
}