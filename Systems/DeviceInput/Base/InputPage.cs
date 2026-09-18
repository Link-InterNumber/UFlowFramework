using System;
using System.Collections.Generic;

namespace UFlowFramework
{
    /// <summary>UI 页面输入响应范围。仅栈顶页面接收输入。</summary>
    public class InputPage<TKey> : IInputPage<TKey>
    {
        private Dictionary<TKey, InputHandler<TKey>> _handlers;

        public InputPage()
        {
            _handlers = new Dictionary<TKey, InputHandler<TKey>>();
        }
        
        public bool isEmpty => _handlers == null || _handlers.Count == 0;

        public void Handle(in InputEvent<TKey> input)
        {
            if (_handlers == null) throw new ObjectDisposedException(nameof(InputPage<TKey>));
            if (_handlers.TryGetValue(input.actionKey, out var handler))
                handler.Invoke(input);
        }

        public bool HasListener(TKey actionKey)
        {
            if (_handlers == null) throw new ObjectDisposedException(nameof(InputPage<TKey>));
            if (_handlers.TryGetValue(actionKey, out var handler))
                return handler.listenerCount > 0;
            return false;
        }

        public void AddListener(TKey actionKey, Action<InputEvent<TKey>> callback)
        {
            if (_handlers == null) throw new ObjectDisposedException(nameof(InputPage<TKey>));
            if (_handlers.TryGetValue(actionKey, out var handler))
                handler.AddListener(callback);
            else
            {
                var newHandler = new InputHandler<TKey>(actionKey);
                newHandler.AddListener(callback);
                _handlers.Add(actionKey, newHandler);
            }
        }
        
        public void RemoveListener(TKey actionKey, Action<InputEvent<TKey>> callback)
        {
            if (_handlers == null) throw new ObjectDisposedException(nameof(InputPage<TKey>));
            if (_handlers.TryGetValue(actionKey, out var handler))
            {
                handler.RemoveListener(callback);
                if (handler.listenerCount == 0)
                {
                    handler.Dispose();
                    _handlers.Remove(actionKey);
                }
            }
        }
        
        public void RemoveAllListener(TKey actionKey)
        {
            if (_handlers == null) throw new ObjectDisposedException(nameof(InputPage<TKey>));
            if (_handlers.TryGetValue(actionKey, out var handler))
            {
                handler.RemoveAllListener();
                handler.Dispose();
                _handlers.Remove(actionKey);
            }
        }

        public void ClearAll()
        {
            if (_handlers == null) throw new ObjectDisposedException(nameof(InputPage<TKey>));
            foreach (var handlersValue in _handlers.Values)
            {
                handlersValue.Dispose();
            }
            _handlers.Clear();
        }

        public void Dispose()
        {
            ClearAll();
            _handlers = null;
        }
    }
}