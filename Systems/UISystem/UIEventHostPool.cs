namespace PowerCellStudio
{
    public static class UIEventHostPool
    {
        private static PoolableObjectPool<UIEventHost> _eventHostPool;

        public static UIEventHost Get()
        {
            if (_eventHostPool == null)
                _eventHostPool = new PoolableObjectPool<UIEventHost>(UIEventHost.Create, 16, 2);
            return _eventHostPool.Get();
        }

        public static void Release(UIEventHost item)
        {
            if (_eventHostPool == null)
                _eventHostPool = new PoolableObjectPool<UIEventHost>(UIEventHost.Create, 16, 2);
            _eventHostPool.Release(item);
        }

        public static void Clear()
        {
            _eventHostPool?.Clear();
        }
    }
}