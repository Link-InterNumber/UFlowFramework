using System;

namespace LinkState
{
    public class TriggerBehavior<T> where T: class
    {
        private TriggerPriority priority;
        private Func<T, bool> condition;
        private Func<T, int> transfer;
        // private Action<T, float> update;

        public TriggerPriority Priority => priority;
        
        public TriggerBehavior(Func<T, bool> conditionFun, Func<T, int> transferFunc, TriggerPriority prior = TriggerPriority.Default)
        {
            // update = updateFun;
            condition = conditionFun;
            transfer = transferFunc;
            priority = prior;
        }

        // public void Execute(T dataSource, float deltaTime)
        // {
        //     update?.Invoke(dataSource, deltaTime);
        // }

        public bool Check(T dataSource)
        {
            return condition?.Invoke(dataSource) ?? false;
        }

        public int DoTransfer(T dataSource)
        {
            return transfer?.Invoke(dataSource) ?? 0;
        }
    }
}