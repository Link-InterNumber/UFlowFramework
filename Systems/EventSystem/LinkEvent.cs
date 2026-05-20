
namespace PowerCellStudio
{
    public class LineEventBase
    {
        public bool enable = true;

        public virtual void RemoveAllListeners(){}
    }

    public class LinkEvent : LineEventBase, IInvolke
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

        public override void RemoveAllListeners()
        {
            events = null;
        }

        public void Invoke()
        {
            if (!enable) return;
            events?.Invoke();
        }

        public int GetEventListenerCount()
        {
            return events?.GetInvocationList().Length ?? 0;
        }
    }

    public class LinkEvent<T> : LineEventBase, IInvolke<T>
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
            onceFun = (data) =>
            {
                fun.Invoke(data);
                events -= onceFun;
            };
            events += onceFun;
        }

        public void Invoke(T data1)
        {
            if (!enable) return;
            events?.Invoke(data1);
        }

        public int GetEventListenerCount()
        {
            return events?.GetInvocationList().Length ?? 0;
        }

        public override void RemoveAllListeners()
        {
            events = null;
        }
    }

    public class LinkEvent<T, TK> : LineEventBase, IInvolke<T, TK>
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
            onceFun = (data, data2) =>
            {
                fun.Invoke(data, data2);
                events -= onceFun;
            };
            events += onceFun;
        }

        public void Invoke(T data1, TK data2)
        {
            if (!enable) return;
            events?.Invoke(data1, data2);
        }

        public int GetEventListenerCount()
        {
            return events?.GetInvocationList().Length ?? 0;
        }

        public override void RemoveAllListeners()
        {
            events = null;
        }
    }

    public class LinkEvent<T, TK, TL> : LineEventBase, IInvolke<T, TK, TL>
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
            onceFun = (data, data2, data3) =>
            {
                fun.Invoke(data, data2, data3);
                events -= onceFun;
            };
            events += onceFun;
        }

        public void Invoke(T data1, TK data2, TL data3)
        {
            if (!enable) return;
            events?.Invoke(data1, data2, data3);
        }

        public int GetEventListenerCount()
        {
            return events?.GetInvocationList().Length ?? 0;
        }

        public override void RemoveAllListeners()
        {
            events = null;
        }
    }
}