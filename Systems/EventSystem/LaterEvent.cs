namespace PowerCellStudio
{
    public class LaterEvent : IInvolke
    {
        private event BaseLinkAction events;
        private bool _toInvoke = false;

        public bool enable = true;

        public void AddListener(BaseLinkAction fun)
        {
#if UNITY_EDITOR
            var allEvent = events?.GetInvocationList();
            if (allEvent != null && allEvent.Length > 0)
            {
                foreach (var eve in allEvent)
                {
                    var fun2 = eve as BaseLinkAction;
                    if (fun2 != fun) continue;
                    ModuleLog.LogError<EventManager>($"重复添加监听:[{fun.Method.Name}]");
                    return;
                }
            }
#endif
            events += fun;
        }

        public void RemoveListener(BaseLinkAction fun)
        {
            events -= fun;
        }
        
        public void AddListenerOnce(BaseLinkAction fun)
        {
            BaseLinkAction onceFun = null;
            onceFun = () =>
            {
                fun.Invoke();
                events -= onceFun;
            };
            events += onceFun;
        }

        public void RemoveAllListeners()
        {
            events = null;
        }
        
        public void Invoke()
        {
            if (_toInvoke || !enable) return;
            _toInvoke = true;
            EventManager.instance.InvokeLaterEvent(this);
        }

        public int GetEventListenerCount()
        {
            return events?.GetInvocationList().Length ?? 0;
        }

        void IInvolke.Invoke()
        {
            events?.Invoke();
            _toInvoke = false;
        }
    }

    public class LaterEvent<T> : IInvolke
    {
        private event BaseLinkAction<T> events;
        private bool _toInvoke = false;

        public bool enable = true;

        public void AddListener(BaseLinkAction<T> fun)
        {
#if UNITY_EDITOR
            var allEvent = events?.GetInvocationList();
            if (allEvent != null && allEvent.Length > 0)
            {
                foreach (var eve in allEvent)
                {
                    var fun2 = eve as BaseLinkAction<T>;
                    if (fun2 != fun) continue;
                    ModuleLog.LogError<EventManager>($"重复添加监听:[{fun.Method.Name}]");
                    return;
                }
            }
#endif
            events += fun;
        }

        public void RemoveListener(BaseLinkAction<T> fun)
        {
            events -= fun;
        }

        public void AddListenerOnce(BaseLinkAction<T> fun)
        {
            BaseLinkAction<T> onceFun = null;
            onceFun = (T a) =>
            {
                fun.Invoke(a);
                events -= onceFun;
            };
            events += onceFun;
        }

        public void RemoveAllListeners()
        {
            events = null;
        }

        private T _data;
        public void Invoke(T data)
        {
            _data = data;
            if (_toInvoke || !enable) return;
            _toInvoke = true;
            EventManager.instance.InvokeLaterEvent(this);
        }

        public int GetEventListenerCount()
        {
            return events?.GetInvocationList().Length ?? 0;
        }

        void IInvolke.Invoke()
        {
            events?.Invoke(_data);
            _toInvoke = false;
        }
    }

    public class LaterEvent<T, TK> : IInvolke
    {
        private event BaseLinkAction<T, TK> events;
        private bool _toInvoke = false;

        public bool enable = true;

        public void AddListener(BaseLinkAction<T, TK> fun)
        {
#if UNITY_EDITOR
            var allEvent = events?.GetInvocationList();
            if (allEvent != null && allEvent.Length > 0)
            {
                foreach (var eve in allEvent)
                {
                    var fun2 = eve as BaseLinkAction<T, TK>;
                    if (fun2 != fun) continue;
                    ModuleLog.LogError<EventManager>($"重复添加监听:[{fun.Method.Name}]");
                    return;
                }
            }
#endif
            events += fun;
        }

        public void RemoveListener(BaseLinkAction<T, TK> fun)
        {
            events -= fun;
        }

        public void AddListenerOnce(BaseLinkAction<T, TK> fun)
        {
            BaseLinkAction<T, TK> onceFun = null;
            onceFun = (T a, TK b) =>
            {
                fun.Invoke(a, b);
                events -= onceFun;
            };
            events += onceFun;
        }

        public void RemoveAllListeners()
        {
            events = null;
        }

        private T _data;
        private TK _data2;
        public void Invoke(T data, TK data2)
        {
            _data = data;
            _data2 = data2;
            if (_toInvoke || !enable) return;
            _toInvoke = true;
            EventManager.instance.InvokeLaterEvent(this);
        }

        public int GetEventListenerCount()
        {
            return events?.GetInvocationList().Length ?? 0;
        }

        void IInvolke.Invoke()
        {
            events?.Invoke(_data, _data2);
            _toInvoke = false;
        }
    }

    public class LaterEvent<T, TK, TL> : IInvolke
    {
        private event BaseLinkAction<T, TK, TL> events;
        private bool _toInvoke = false;

        public bool enable = true;

        public void AddListener(BaseLinkAction<T, TK, TL> fun)
        {
#if UNITY_EDITOR
            var allEvent = events?.GetInvocationList();
            if (allEvent != null && allEvent.Length > 0)
            {
                foreach (var eve in allEvent)
                {
                    var fun2 = eve as BaseLinkAction<T, TK, TL>;
                    if (fun2 != fun) continue;
                    ModuleLog.LogError<EventManager>($"重复添加监听:[{fun.Method.Name}]");
                    return;
                }
            }
#endif
            events += fun;
        }

        public void RemoveListener(BaseLinkAction<T, TK, TL> fun)
        {
            events -= fun;
        }

        public void AddListenerOnce(BaseLinkAction<T, TK, TL> fun)
        {
            BaseLinkAction<T, TK, TL> onceFun = null;
            onceFun = (T a, TK b, TL c) =>
            {
                fun.Invoke(a, b, c);
                events -= onceFun;
            };
            events += onceFun;
        }

        public void RemoveAllListeners()
        {
            events = null;
        }

        private T _data;
        private TK _data2;
        private TL _data3;
        public void Invoke(T data, TK data2, TL data3)
        {
            _data = data;
            _data2 = data2;
            _data3 = data3;
            if (_toInvoke || !enable) return;
            _toInvoke = true;
            EventManager.instance.InvokeLaterEvent(this);
        }

        public int GetEventListenerCount()
        {
            return events?.GetInvocationList().Length ?? 0;
        }

        void IInvolke.Invoke()
        {
            events?.Invoke(_data, _data2, _data3);
            _toInvoke = false;
        }
    }
}