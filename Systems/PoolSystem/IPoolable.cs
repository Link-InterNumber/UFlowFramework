namespace PowerCellStudio
{
    public interface IPoolable
    {
        // public bool Spawned { set; get; }
        //
        // public int SpawnIndex { set; get; }
        public LinkPool<IPoolable> LinkPool { set; get; }
        
        public void OnSpawn();

        public void DeSpawn();

        public void OnDeSpawn();

        public void Dispose();

    }
}