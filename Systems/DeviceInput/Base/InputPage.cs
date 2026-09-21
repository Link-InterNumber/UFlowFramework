using System;
using System.Collections.Generic;
using UnityEngine;

namespace UFlowFramework
{
    /// <summary>UI 页面输入响应范围。仅栈顶页面接收输入。</summary>
    public class InputPage<TKey> : IInputPage<TKey>
    {
        private Dictionary<TKey, InputHandler<TKey>> _handlers;
        private Dictionary<TKey, float> _inputStartTime;

        public InputPage()
        {
            _handlers = new Dictionary<TKey, InputHandler<TKey>>();
            _inputStartTime = new Dictionary<TKey, float>();
        }
        
        public bool isEmpty => _handlers == null || _handlers.Count == 0;

        public void Handle(InputEvent<TKey> input)
        {
            if (_handlers == null) throw new ObjectDisposedException(nameof(InputPage<TKey>));
            if (!_handlers.TryGetValue(input.actionKey, out var handler)) return;
            if (input.state == InputEventState.ReleaseThisFrame)
            {
                if (_inputStartTime.Remove(input.actionKey, out var startTime))
                {
                    var holdTime = Time.unscaledTime - startTime;
                    input.SetHoldTime(holdTime);
                }
            }
            else if (input.state != InputEventState.Released)
            {
                if (_inputStartTime.TryGetValue(input.actionKey, out var startTime))
                {
                    var holdTime = Time.unscaledTime - startTime;
                    input.SetHoldTime(holdTime);
                }
                else
                {
                    _inputStartTime[input.actionKey] = Time.unscaledTime;
                }
            }
            handler.Invoke(input);
        }

        public bool HasListener(TKey actionKey)
        {
            if (_handlers == null) throw new ObjectDisposedException(nameof(InputPage<TKey>));
            if (_handlers.TryGetValue(actionKey, out var handler))
                return handler.hasListener;
            return false;
        }

        public void AddListener(TKey actionKey, Action<InputEvent<TKey>> callback)
        {
            if (_handlers == null) throw new ObjectDisposedException(nameof(InputPage<TKey>));
            if (_handlers.TryGetValue(actionKey, out var handler))
            {
                handler.AddListener(callback);
            }
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
                if (!handler.hasListener)
                {
                    _inputStartTime.Remove(actionKey);
                    handler.Dispose();
                    _handlers.Remove(actionKey);
                }
            }
        }
        
        public void RemoveAllListener(TKey actionKey)
        {
            if (_handlers == null) throw new ObjectDisposedException(nameof(InputPage<TKey>));
            _inputStartTime.Remove(actionKey);
            if (_handlers.TryGetValue(actionKey, out var handler))
            {
                handler.RemoveAllListener();
                handler.Dispose();
                _handlers.Remove(actionKey);
            }
        }

        public void ClearAll()
        {
            _inputStartTime?.Clear();
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
            _inputStartTime = null;
        }
    }
}