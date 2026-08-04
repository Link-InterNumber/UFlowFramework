using System;

namespace PowerCellStudio
{
    public abstract class SingletonBase<T> : IDisposable where T: class, new()
    {
        protected SingletonBase(){}

        private static class SingletonProvider<TK> where TK: class, new()
        {
            public static TK instance => _instance as TK;

            private static object _instance = Activator.CreateInstance(typeof(TK));
        }
        
        private static T _instance;

        public static T instance
        {
            get => _instance;
            set
            {
                if (_instance != null)
                {
                    LinkLogger.LogError($"SingletonBase {typeof(T).Name} instance already set");
                    return;
                }
                _instance = value;
                // if (_instance is IModule module)
                // {
                //     module.OnInit();
                //     ModuleManager.instance.AddModule(typeof(T), module);
                // }
                // if (_instance is IEventModule eventModule)
                // {
                //     eventModule.RegisterEvent();
                // }
            }
        }
        
        protected virtual void Deinit(){}

        public void Dispose()
        {
            Deinit();
            if (_instance == null) return;
            _instance = null;
        }
    }
}