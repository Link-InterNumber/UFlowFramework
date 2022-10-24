namespace LinkFrameWork.PoolSystem
{
    public interface IPoolable
    {
        public bool Spawned { set; get; }

        public int SpawnIndex { set; get; }

        public T Spawn<T>() where T : class, IPoolable;

        public void OnSpawn();

        public void DeSpawn();

        public void OnDeSpawn();

        public IPoolable Clone();

        public void DestroyNode();
    }

    public interface IPoolCarrier
    {
        public int MaxCount { set; get; }

        public void InitPool(IPoolable prefab, int maxCount);

        public IPoolable GetPrefab();

        public IPoolable GetNode();

        public void PushNode(IPoolable node);

        public void Clear();
    }
}