using System;
using System.Collections.Generic;

namespace PowerCellStudio
{
    public class LoaderYieldInstructionPool
    {
        private Dictionary<Type, Queue<ILoaderYieldInstruction>> _poolDic;

        private int _maxCountInPool = 64;

        public LoaderYieldInstructionPool()
        {
            _poolDic = new Dictionary<Type, Queue<ILoaderYieldInstruction>>();
        }

        public LoaderYieldInstruction<T> Get<T>(string path) where T : class
        {
            var key = typeof(T);

            if (!_poolDic.TryGetValue(key, out var stack) || stack.Count == 0)
            {
                var instance = new LoaderYieldInstruction<T>(path);
                return instance;
            }

            var item = stack.Dequeue();
            var typedItem = (LoaderYieldInstruction<T>)item;
            typedItem.Reset(path);
            return typedItem;
        }

        public void Release<T>(ILoaderYieldInstruction item) where T : class
        {
            item.Dispose();
            var key = typeof(T);

            if (!_poolDic.TryGetValue(key, out var stack))
            {
                stack = new Queue<ILoaderYieldInstruction>();
                _poolDic.Add(key, stack);
            }

            if (stack.Count >= _maxCountInPool)
            {
                return;
            }

            stack.Enqueue(item);
        }
    }
}