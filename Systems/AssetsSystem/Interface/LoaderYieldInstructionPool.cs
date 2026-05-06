using System;
using System.Collections.Generic;

namespace PowerCellStudio
{
    internal class LoaderYieldInstructionPool
    {
        private Dictionary<Type, Stack<ILoaderYieldInstruction>> _poolDic;

        private int _maxCountInPool = 64;

        public LoaderYieldInstructionPool()
        {
            _poolDic = new Dictionary<Type, Stack<ILoaderYieldInstruction>>();
        }

        public LoaderYieldInstruction<T> Get<T>(string path) where T : class
        {
            var key = typeof(T);

            if (!_poolDic.TryGetValue(key, out var stack) || stack.Count == 0)
            {
                var instance = new LoaderYieldInstruction<T>(path);
                return instance;
            }

            var item = stack.Pop();
            var typedInstance = item as LoaderYieldInstruction<T>;
            typedInstance?.Reset(path);
            return typedInstance;
        }

        public void Release<T>(ILoaderYieldInstruction item) where T : class
        {
            item.Dispose();
            var key = typeof(T);

            if (!_poolDic.TryGetValue(key, out var stack))
            {
                stack = new Stack<ILoaderYieldInstruction>();
                _poolDic.Add(key, stack);
            }

            if (stack.Count >= _maxCountInPool)
            {
                return;
            }

            stack.Push(item);
        }
    }
}