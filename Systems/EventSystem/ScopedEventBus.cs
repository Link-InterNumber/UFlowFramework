using System.Collections.Generic;

namespace PowerCellStudio
{
    public class ScopedEventBus
    {
        private Dictionary<int, LineEventBase> _eventBus = new Dictionary<int, LineEventBase>();

#region Listener Management
        public void AddListener(int eventId, BaseLinkAction action)
        {
            if (_eventBus.TryGetValue(eventId, out var e))
            {
                var linkEvent = e as LinkEvent;
                if (linkEvent == null)
                {
                    ModuleLog.LogError($"Event with id {eventId} is not of type LinkEvent");
                    return;
                }
                linkEvent.AddListener(action);
            }
            else
            {
                var linkEvent = new LinkEvent();
                linkEvent.AddListener(action);
                _eventBus[eventId] = linkEvent;
            }
        }

        public void AddListener<T>(int eventId, BaseLinkAction<T> action)
        {
            if (_eventBus.TryGetValue(eventId, out var e))
            {
                var linkEvent = e as LinkEvent<T>;
                if (linkEvent == null)
                {
                    ModuleLog.LogError($"Event with id {eventId} is not of type LinkEvent<{typeof(T).Name}>");
                    return;
                }
                linkEvent.AddListener(action);
            }
            else
            {
                var linkEvent = new LinkEvent<T>();
                linkEvent.AddListener(action);
                _eventBus[eventId] = linkEvent;
            }
        }

        public void AddListener<T, TK>(int eventId, BaseLinkAction<T, TK> action)
        {
            if (_eventBus.TryGetValue(eventId, out var e))
            {
                var linkEvent = e as LinkEvent<T, TK>;
                if (linkEvent == null)
                {
                    ModuleLog.LogError($"Event with id {eventId} is not of type LinkEvent<{typeof(T).Name}, {typeof(TK).Name}>");
                    return;
                }
                linkEvent.AddListener(action);
            }
            else
            {
                var linkEvent = new LinkEvent<T, TK>();
                linkEvent.AddListener(action);
                _eventBus[eventId] = linkEvent;
            }
        }

        public void AddListener<T, TK, TL>(int eventId, BaseLinkAction<T, TK, TL> action)
        {
            if (_eventBus.TryGetValue(eventId, out var e))
            {
                var linkEvent = e as LinkEvent<T, TK, TL>;
                if (linkEvent == null)
                {
                    ModuleLog.LogError($"Event with id {eventId} is not of type LinkEvent<{typeof(T).Name}, {typeof(TK).Name}, {typeof(TL).Name}>");
                    return;
                }
                linkEvent.AddListener(action);
            }
            else
            {
                var linkEvent = new LinkEvent<T, TK, TL>();
                linkEvent.AddListener(action);
                _eventBus[eventId] = linkEvent;
            }
        }
#endregion

#region Remove Listener

        public void Clear()
        {
            foreach (var e in _eventBus.Values)
            {
                e.RemoveAllListeners();
            }
            _eventBus.Clear();
        }

        public void RemoveListener(int eventId, BaseLinkAction action)
        {
            if (_eventBus.TryGetValue(eventId, out var e))
            {
                var linkEvent = e as LinkEvent;
                if (linkEvent == null)
                {
                    ModuleLog.LogError($"Event with id {eventId} is not of type LinkEvent");
                    return;
                }
                linkEvent.RemoveListener(action);
            }
        }

        public void RemoveListener<T>(int eventId, BaseLinkAction<T> action)
        {
            if (_eventBus.TryGetValue(eventId, out var e))
            {
                var linkEvent = e as LinkEvent<T>;
                if (linkEvent == null)
                {
                    ModuleLog.LogError($"Event with id {eventId} is not of type LinkEvent<{typeof(T).Name}>");
                    return;
                }
                linkEvent.RemoveListener(action);
            }
        }

        public void RemoveListener<T, TK>(int eventId, BaseLinkAction<T, TK> action)
        {
            if (_eventBus.TryGetValue(eventId, out var e))
            {
                var linkEvent = e as LinkEvent<T, TK>;
                if (linkEvent == null)
                {
                    ModuleLog.LogError($"Event with id {eventId} is not of type LinkEvent<{typeof(T).Name}, {typeof(TK).Name}>");
                    return;
                }
                linkEvent.RemoveListener(action);
            }
        }

        public void RemoveListener<T, TK, TL>(int eventId, BaseLinkAction<T, TK, TL> action)
        {
            if (_eventBus.TryGetValue(eventId, out var e))
            {
                var linkEvent = e as LinkEvent<T, TK, TL>;
                if (linkEvent == null)
                {
                    ModuleLog.LogError($"Event with id {eventId} is not of type LinkEvent<{typeof(T).Name}, {typeof(TK).Name}, {typeof(TL).Name}>");
                    return;
                }
                linkEvent.RemoveListener(action);
            }
        }

        public void RemoveAllListeners(int eventId)
        {
            if (_eventBus.TryGetValue(eventId, out var e))
            {
                e.RemoveAllListeners();
            }
            _eventBus.Remove(eventId);
        }

#endregion

#region Invoke

        public void Invoke(int eventId)
        {
            if (_eventBus.TryGetValue(eventId, out var e))
            {
                var linkEvent = e as LinkEvent;
                if (linkEvent == null)
                {
                    ModuleLog.LogError($"Event with id {eventId} is not of type LinkEvent");
                    return;
                }
                linkEvent.Invoke();
            }
            else
            {
                ModuleLog.LogError($"No event found with id {eventId} to invoke");
            }
        }

        public void Invoke<T>(int eventId, T data)
        {
            if (_eventBus.TryGetValue(eventId, out var e))
            {
                var linkEvent = e as IInvolke<T>;
                if (linkEvent == null)
                {
                    ModuleLog.LogError($"Event with id {eventId} is not of type IInvolke<{typeof(T).Name}>");
                    return;
                }
                linkEvent.Invoke(data);
            }
            else
            {
                ModuleLog.LogError($"No event found with id {eventId} to invoke");
            }
        }

        public void Invoke<T, TK>(int eventId, T data, TK data2)
        {
            if (_eventBus.TryGetValue(eventId, out var e))
            {
                var linkEvent = e as IInvolke<T, TK>;
                if (linkEvent == null)
                {
                    ModuleLog.LogError($"Event with id {eventId} is not of type IInvolke<{typeof(T).Name}, {typeof(TK).Name}>");
                    return;
                }
                linkEvent.Invoke(data, data2);
            }
            else
            {
                ModuleLog.LogError($"No event found with id {eventId} to invoke");
            }
        }

        public void Invoke<T, TK, TL>(int eventId, T data, TK data2, TL data3)
        {
            if (_eventBus.TryGetValue(eventId, out var e))
            {
                var linkEvent = e as IInvolke<T, TK, TL>;
                if (linkEvent == null)
                {
                    ModuleLog.LogError($"Event with id {eventId} is not of type IInvolke<{typeof(T).Name}, {typeof(TK).Name}, {typeof(TL).Name}>");
                    return;
                }
                linkEvent.Invoke(data, data2, data3);
            }
            else
            {
                ModuleLog.LogError($"No event found with id {eventId} to invoke");
            }
        }

        private struct EventInvokeInfo
        {
            public int eventId;
            public object[] parameters;
        }

        private Queue<EventInvokeInfo> _laterInvokeQueue = new Queue<EventInvokeInfo>();

        /// <summary>
        /// 收集需要稍后触发的事件调用，TriggerInvoke会触发所有收集的事件调用
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="parameters"></param>
        public void CollectInvoke(int eventId, params object[] parameters)
        {
            _laterInvokeQueue.Enqueue(new EventInvokeInfo { eventId = eventId, parameters = parameters });
        }

        /// <summary>
        /// 触发所有通过CollectInvoke收集的事件调用
        /// </summary>
        public void TriggerInvoke()
        {
            while (_laterInvokeQueue.Count > 0)
            {
                var info = _laterInvokeQueue.Dequeue();
                if (info.parameters == null || info.parameters.Length == 0)
                {
                    Invoke(info.eventId);
                    continue;
                }
                switch (info.parameters.Length)
                {
                    case 1:
                        Invoke(info.eventId, info.parameters[0]);
                        break;
                    case 2:
                        Invoke(info.eventId, info.parameters[0], info.parameters[1]);
                        break;
                    case 3:
                        Invoke(info.eventId, info.parameters[0], info.parameters[1], info.parameters[2]);
                        break;
                    default:
                        ModuleLog.LogError($"Unsupported parameter count {info.parameters.Length} for event id {info.eventId}");
                        break;
                }
            }
        }

        private Stack<EventInvokeInfo> _invokeStack = new Stack<EventInvokeInfo>();

        /// <summary>
        /// 将事件调用压入栈中，等待后续通过ReleaseInvoke触发。适用于在一定时间内可能多次触发同一事件，但只希望最终触发一次的场景（如UI刷新）。ReleaseInvoke会触发栈中所有事件调用，但同一事件ID只会触发一次。
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="parameters"></param>
        public void HoldInvoke(int eventId, params object[] parameters)
        {
            _invokeStack.Push(new EventInvokeInfo { eventId = eventId, parameters = parameters });
        }

        /// <summary>
        /// 触发所有通过HoldInvoke收集的事件调用，且同一事件ID只会触发一次。适用于在一定时间内可能多次触发同一事件，但只希望最终触发一次的场景（如UI刷新）。HoldInvoke会将事件调用压入栈中，等待后续通过ReleaseInvoke触发。ReleaseInvoke会触发栈中所有事件调用，但同一事件ID只会触发一次。
        /// </summary>
        public void ReleaseInvoke()
        {
            var idSet = new HashSet<int>();
            while (_invokeStack.Count > 0)
            {
                var info = _invokeStack.Pop();
                if (idSet.Contains(info.eventId)) continue; // 避免重复触发同一事件
                idSet.Add(info.eventId);
                if (info.parameters == null || info.parameters.Length == 0)
                {
                    Invoke(info.eventId);
                    continue;
                }
                switch (info.parameters.Length)
                {
                    case 1:
                        Invoke(info.eventId, info.parameters[0]);
                        break;
                    case 2:
                        Invoke(info.eventId, info.parameters[0], info.parameters[1]);
                        break;
                    case 3:
                        Invoke(info.eventId, info.parameters[0], info.parameters[1], info.parameters[2]);
                        break;
                    default:
                        ModuleLog.LogError($"Unsupported parameter count {info.parameters.Length} for event id {info.eventId}");
                        break;
                }
            }
        }

#endregion
    }
}