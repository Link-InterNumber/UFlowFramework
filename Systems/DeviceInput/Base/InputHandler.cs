using System;
using System.Collections.Generic;

namespace UFlowFramework
{
    internal class InputHandler<TKey> : IDisposable
    {
        private TKey _actionKey;
        public TKey actionKey => _actionKey;

        private Stack<Action<InputEvent<TKey>>> _callbackStack;
        
        internal Action<InputEvent<TKey>> callback => _callbackStack.Count > 0 ? _callbackStack.Peek() : null;
        
        public int listenerCount => callback?.GetInvocationList().Length ?? 0;

        internal InputHandler(TKey actionKey)
        {
            _actionKey = actionKey;
            _callbackStack = new Stack<Action<InputEvent<TKey>>>();
            _callbackStack.Push(null);
        }

        internal void AddListener(Action<InputEvent<TKey>> callback)
        {
            if (_callbackStack.Count <= 0) return;
            var peekCallback = _callbackStack.Pop();
            peekCallback += callback;
            _callbackStack.Push(peekCallback);
        }

        internal void RemoveListener(Action<InputEvent<TKey>> callback)
        {
            if (_callbackStack.Count <= 0) return;
            var peekCallback = _callbackStack.Pop();
            peekCallback -= callback;
            _callbackStack.Push(peekCallback);
        }
        
        public void OverlapListener(Action<InputEvent<TKey>> callback)
        {
            _callbackStack.Push(callback);
        }
        
        public void SeparateListener()
        {
            if (_callbackStack.Count <= 0) return;
            var peekCallback = _callbackStack.Pop();
            peekCallback = null;
        }

        internal void ClearPeekListener()
        {
            if (_callbackStack.Count <= 0) return;
            var peekCallback = _callbackStack.Pop();
            peekCallback = null;
            _callbackStack.Push(null);
        }

        internal void RemoveAllListener()
        {
            while (_callbackStack.Count > 0)
            {
                var peek = _callbackStack.Pop();
                peek = null;
            }
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