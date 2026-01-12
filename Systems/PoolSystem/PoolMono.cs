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
            if (LinkPool != null && LinkPool.Release(this)) return;
            OnDeSpawn();
            Dispose();
        }

        public abstract void OnDeSpawn();

        public virtual void Dispose()
        {
            GameObject.Destroy(gameObject);
        }
    }
}