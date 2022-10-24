using System;

namespace LinkFrameWork.DesignPatterns
{
    public abstract class SingletonBase<T> where T: class, new()
    {
        private static T _instance;

        public static T Instance => SingletonProvider<T>.Instance;

        protected SingletonBase(){}

        private static class SingletonProvider<TK> where TK: class, new()
        {
            public static TK Instance => _instance as TK;

            private static object _instance = Activator.CreateInstance(typeof(TK));
        }
    }
}