using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace PowerCellStudio
{
    /// <summary>
    /// UI事件宿主类，用于管理UI事件的注册和注销，确保在对象释放时清理所有事件监听器。
    /// </summary>
    public class UIEventHost : IDisposable, IPoolable
    {
        private List<UnityEventBase> _events;

        public LinkPool<IPoolable> LinkPool { get ; set ; }
        
        internal static UIEventHost Create()
        {
            var host = new UIEventHost();
            return host;
        }
        
        public void AddListener(Button button, UnityAction callback)
        {
            button.onClick.AddListener(callback);
            _events.Add(button.onClick);
        }

        public void RemoveListener(Button button, UnityAction callback)
        {
            button.onClick.RemoveListener(callback);
        }
        
        public void AddListener(Slider slider, UnityAction<float> callback)
        {
            slider.onValueChanged.AddListener(callback);
            _events.Add(slider.onValueChanged);
        }

        public void RemoveListener(Slider slider, UnityAction<float> callback)
        {
            slider.onValueChanged.RemoveListener(callback);
        }
        
        public void AddListener(Toggle toggle, UnityAction<bool> callback)
        {
            toggle.onValueChanged.AddListener(callback);
            _events.Add(toggle.onValueChanged);
        }

        public void RemoveListener(Toggle toggle, UnityAction<bool> callback)
        {
            toggle.onValueChanged.RemoveListener(callback);
        }
        
        public void AddListener(ScrollRect scrollRect, UnityAction<Vector2> callback)
        {
            scrollRect.onValueChanged.AddListener(callback);
            _events.Add(scrollRect.onValueChanged);
        }

        public void RemoveListener(ScrollRect scrollRect, UnityAction<Vector2> callback)
        {
            scrollRect.onValueChanged.RemoveListener(callback);
        }

        public void AddListener(Scrollbar scrollbar, UnityAction<float> callback)
        {
            scrollbar.onValueChanged.AddListener(callback);
            _events.Add(scrollbar.onValueChanged);
        }

        public void RemoveListener(Scrollbar scrollbar, UnityAction<float> callback)
        {
            scrollbar.onValueChanged.RemoveListener(callback);
        }

        public void AddListener(Dropdown dropdown, UnityAction<int> callback)
        {
            dropdown.onValueChanged.AddListener(callback);
            _events.Add(dropdown.onValueChanged);
        }

        public void RemoveListener(Dropdown dropdown, UnityAction<int> callback)
        {
            dropdown.onValueChanged.RemoveListener(callback);
        }

        public void AddListener(InputField inputField, UnityAction<string> callback)
        {
            inputField.onEndEdit.AddListener(callback);
            _events.Add(inputField.onEndEdit);
        }

        public void RemoveListener(InputField inputField, UnityAction<string> callback)
        {
            inputField.onEndEdit.RemoveListener(callback);
        }
        
        public void Release()
        {
            foreach (var evt in _events)
            {
                evt?.RemoveAllListeners();
            }
            _events.Clear();
        }

        public void Dispose()
        {
            Release();
            GC.SuppressFinalize(this);
        }

        public void OnSpawn()
        {
            _events = ListPool<UnityEventBase>.Get();
        }

        public void DeSpawn()
        {
            LinkPool.Release(this);
        }

        public void OnDeSpawn()
        {
            Release();
            if (_events != null) ListPool<UnityEventBase>.Release(_events);
            _events = null;
        }
    }
}