using System;
using System.Collections.Generic;

namespace UFlowFramework
{
    /// <summary>UI 页面输入响应范围。仅栈顶页面接收输入。</summary>
    public class InputPage<TKey> : IDisposable
    {
        private Dictionary<TKey, InputHandler<TKey>> _handlers;

        public InputPage()
        {
            _handlers = new Dictionary<TKey, InputHandler<TKey>>();
        }

        public void Handle(in InputEvent<TKey> input)
        {
            if (_handlers.TryGetValue(input.actionKey, out var handler))
                handler.Invoke(input);
        }

        public void AddListener(TKey actionKey, Action<InputEvent<TKey>> callback)
        {
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
            if (_handlers.TryGetValue(actionKey, out var handler))
                handler.RemoveListener(callback);
        }
        
        public void RemoveAllListener(TKey actionKey)
        {
            if (_handlers.TryGetValue(actionKey, out var handler))
                handler.RemoveAllListener();
        }

        public void ClearAll()
        {
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