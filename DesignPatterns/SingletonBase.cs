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
                    LinkLog.LogError($"SingletonBase {typeof(T).Name} instance already set");
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

        public void Dispose()
        {
            if (_instance == null) return;
            if (_instance is IEventModule eventModule)
            {
                eventModule.UnRegisterEvent();
            }
            if (_instance is IOnGameResetModule module)
            {
                module.OnGameReset();
                ModuleManager.instance.RemoveModule(typeof(T));
            }
            // _instance = null;
        }
    }
}