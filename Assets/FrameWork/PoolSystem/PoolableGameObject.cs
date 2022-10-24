using LinkFrameWork.MonoInstance;
using UnityEngine;

namespace LinkFrameWork.PoolSystem
{
    public abstract class PoolableGameObject<T>: MonoBehaviour, IPoolable where T : class, IPoolable
    {
        public bool Spawned { get; set; }

        private int _spawnIndex = 1;

        public int SpawnIndex
        {
            get => _spawnIndex;
            set
            {
                _spawnIndex = value;
                var o = gameObject;
                o.name = $"{o.name}_{_spawnIndex}";
            }
        }

        public virtual TK Spawn<TK>() where TK :class, IPoolable
        {
            Spawned = true;
            gameObject.SetActive(true);
            return this as TK;
        }

        public abstract void OnSpawn();

        public virtual void DeSpawn()
        {
            Spawned = false;
            gameObject.SetActive(false);
            PoolManager.Instance.DeSpawn<T>(this as T);
        }

        public abstract void OnDeSpawn();

        public virtual IPoolable Clone()
        {
            var cloned = Instantiate(gameObject);
            return cloned.GetComponent<T>();
        }

        public virtual void DestroyNode()
        {
            Destroy(gameObject);
        }
    }
}