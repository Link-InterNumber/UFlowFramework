using System;

namespace LinkState
{
    public class ExecuteBehavior<T> where T: class
    {
        private Action<T, float> update;
        
        public ExecuteBehavior(Action<T, float> update)
        {
            this.update = update;
        }

        public void Execute(T dataSource, float deltaTime)
        {
            update?.Invoke(dataSource, deltaTime);
        }
    }
}