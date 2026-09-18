using System;

namespace UFlowFramework
{
    internal class InputHandler<TKey> : IDisposable
    {
        private TKey _actionKey;
        public TKey actionKey => _actionKey;
        
        internal event Action<InputEvent<TKey>> callback;
        
        public int listenerCount => callback?.GetInvocationList().Length ?? 0;

        internal InputHandler(TKey actionKey)
        {
            _actionKey = actionKey;
        }

        internal void AddListener(Action<InputEvent<TKey>> callback)
        {
            this.callback += callback;
        }

        internal void RemoveListener(Action<InputEvent<TKey>> callback)
        {
            this.callback -= callback;
        }

        internal void RemoveAllListener()
        {
            callback = null;
        }

        internal void Invoke(InputEvent<TKey> eventData)
        {
            callback?.Invoke(eventData);
        }

        public void Dispose()
        {
            RemoveAllListener();
        }
    }
}