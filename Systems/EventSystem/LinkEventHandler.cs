
namespace PowerCellStudio
{
    public delegate void BaseLinkAction();

    public delegate void BaseLinkAction<T>(T data);

    public delegate void BaseLinkAction<T, TK>(T data, TK data2);

    public delegate void BaseLinkAction<T, TK, TL>(T data, TK data2, TL data3);

    public interface IInvolke
    {
        public void Invoke();

        public int GetEventListenerCount();
    }
    
    public interface IInvolke<T>
    {
        public void Invoke(T data);

        public int GetEventListenerCount();
    }
    
    public interface IInvolke<T, TK>
    {
        public void Invoke(T data, TK data2);

        public int GetEventListenerCount();
    }
    
    public interface IInvolke<T, TK, TL>
    {
        public void Invoke(T data, TK data2, TL data3);

        public int GetEventListenerCount();
    }

    public class LaterEvent : IInvolke
    {
        private event BaseLinkAction events;
        private bool _toInvoke = false;
        private bool toInvoke => _toInvoke;

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
                    ModuleLog<EventManager>.LogError("重复添加监听");
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
            EventManager.instance.InvokeLaterEvent(this);
            _toInvoke = true;
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

    public class LinkEvent : IInvolke
    {
        private event BaseLinkAction events;

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
                    ModuleLog<EventManager>.LogError("重复添加监听");
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
            events?.Invoke();
        }

        public int GetEventListenerCount()
        {
            return events?.GetInvocationList().Length ?? 0;
        }
    }

    public class LinkEvent<T, TK> :IInvolke<T, TK>
    {
        private event BaseLinkAction<T, TK> events;

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
                    ModuleLog<EventManager>.LogError("重复添加监听");
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
            onceFun = (data, data2) =>
            {
                fun.Invoke(data, data2);
                events -= onceFun;
            };
            events += onceFun;
        }

        public void Invoke(T data1, TK data2)
        {
            events?.Invoke(data1, data2);
        }

        public int GetEventListenerCount()
        {
            return events?.GetInvocationList().Length ?? 0;
        }

        public void RemoveAllListeners()
        {
            events = null;
        }
    }

    public class LinkEvent<T> : IInvolke<T>
    {
        private event BaseLinkAction<T> events;

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
                    ModuleLog<EventManager>.LogError("重复添加监听");
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
            onceFun = (data) =>
            {
                fun.Invoke(data);
                events -= onceFun;
            };
            events += onceFun;
        }

        public void Invoke(T data1)
        {
            events?.Invoke(data1);
        }

        public int GetEventListenerCount()
        {
            return events?.GetInvocationList().Length ?? 0;
        }

        public void RemoveAllListeners()
        {
            events = null;
        }
    }

    public class LinkEvent<T, TK, TL> : IInvolke<T, TK, TL>
    {
        private event BaseLinkAction<T, TK, TL> events;

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
                    ModuleLog<EventManager>.LogError("重复添加监听");
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
            onceFun = (data, data2, data3) =>
            {
                fun.Invoke(data, data2, data3);
                events -= onceFun;
            };
            events += onceFun;
        }

        public void Invoke(T data1, TK data2, TL data3)
        {
            events?.Invoke(data1, data2, data3);
        }

        public int GetEventListenerCount()
        {
            return events?.GetInvocationList().Length ?? 0;
        }

        public void RemoveAllListeners()
        {
            events = null;
        }
    }
}