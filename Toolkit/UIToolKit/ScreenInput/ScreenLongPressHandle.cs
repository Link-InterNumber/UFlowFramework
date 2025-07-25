using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public class ScreenLongPressHandle : IScreenInputHandle
    {
        private bool _enable = false;
        public bool enable {get => _enable; set => _enable = value;}
        private event ScreenInputEventHandler _onLongPress;

        private Dictionary<int, float> _idToStartTime = new Dictionary<int, float>();
        private HashSet<int> _invokedId = new HashSet<int>();

        private float _invokeMinTime = 0.5f;
        private float _mouseStartTime;
        private bool _mousePressInvoked;

        public void RegisterInput(ScreenInputEventHandler action)
        {
#if UNITY_EDITOR
            var allEvent = _onLongPress?.GetInvocationList();
            if (allEvent != null && allEvent.Length > 0)
            {
                foreach (var eve in allEvent)
                {
                    var fun2 = eve as ScreenInputEventHandler;
                    if (fun2 != action) continue;
                    LinkLog.LogError($"重复添加监听:[{action.Method.Name}]");
                    return;
                }
            }
#endif
            _onLongPress += action;
        }

        public void UnregisterInput(ScreenInputEventHandler action)
        {
            _onLongPress -= action;
        }

        public void OnEnable()
        {
            _enable = true;
        }

        public void OnDisable()
        {
            _enable = false;
        }

        private bool CheckCanInvoke(int id)
        {
            return _idToStartTime.TryGetValue(id, out var startTime) &&
                   Time.time - startTime - _invokeMinTime > 0;
        }

        private void RemoveInput(int id)
        {
            _idToStartTime.Remove(id);
            _invokedId.Remove(id);
            _mouseStartTime = 0;
            _mousePressInvoked = false;
        }

        public void OnUpdate()
        {
            if (!_enable) return;
            if (Application.isMobilePlatform)
            {
#if ENABLE_INPUT_SYSTEM
                var touches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
                if (touches.Count == 0) return;
                for (int i = 0; i < touches.Count; i++)
                {
                    var touch = touches[i];
                    var touchId = touch.touchId;
                    switch (touch.phase)
                    {
                        case UnityEngine.InputSystem.TouchPhase.Began:
                            _idToStartTime[touchId] = Time.time;
                            break;
                        case UnityEngine.InputSystem.TouchPhase.Moved:
                            if (touch.delta.sqrMagnitude > 5f)
                            {
                                _onLongPress?.Invoke(new ScreenInputEvent
                                {
                                    inputId = touchId,
                                    screenPos = touch.screenPosition,
                                    pressTime = Time.time - _idToStartTime[touchId],
                                    tapCount = 1,
                                    state = ScreenInputEventState.End
                                });
                                RemoveInput(touchId);
                            }
                            break;
                        case UnityEngine.InputSystem.TouchPhase.Ended:
                            if (CheckCanInvoke(touchId))
                            {
                                _onLongPress?.Invoke(new ScreenInputEvent
                                {
                                    inputId = touchId,
                                    screenPos = touch.screenPosition,
                                    pressTime = Time.time - _idToStartTime[touchId],
                                    tapCount = 1,
                                    state = ScreenInputEventState.End
                                });
                                RemoveInput(touchId);
                            }
                            break;
                        case UnityEngine.InputSystem.TouchPhase.Stationary:
                            if (CheckCanInvoke(touchId))
                            {
                                var eventData = new ScreenInputEvent
                                {
                                    inputId = touchId,
                                    screenPos = touch.screenPosition,
                                    pressTime = Time.time - _idToStartTime[touchId],
                                    tapCount = 1,
                                    state = _invokedId.Contains(touchId)
                                        ? ScreenInputEventState.Execute
                                        : ScreenInputEventState.Start,
                                };
                                _onLongPress?.Invoke(eventData);
                                if (eventData.state == ScreenInputEventState.Start)
                                    _invokedId.Add(touchId);
                            }
                            break;
                        default:
                            RemoveInput(touchId);
                            break;
                    }
                }
#else
                if (Input.touchCount == 0) return;
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    var fingerId = t.fingerId;
                    switch (t.phase)
                    {
                        case TouchPhase.Began:
                            _idToStartTime[fingerId] = Time.time;
                            break;
                        case TouchPhase.Moved:
                            if (t.deltaPosition.sqrMagnitude > 5f)
                            {
                                _onLongPress?.Invoke(new ScreenInputEvent
                                {
                                    inputId = fingerId,
                                    screenPos = t.position,
                                    pressTime = Time.time - _idToStartTime[fingerId],
                                    tapCount = 1,
                                    state = ScreenInputEventState.End
                                });
                                RemoveInput(fingerId);
                            }
                            break;
                        case TouchPhase.Stationary:
                            if (CheckCanInvoke(fingerId))
                            {
                                var eventData = new ScreenInputEvent
                                {
                                    inputId = fingerId,
                                    screenPos = t.position,
                                    pressTime = Time.time - _idToStartTime[fingerId],
                                    tapCount = 1,
                                    state = _invokedId.Contains(fingerId)
                                        ? ScreenInputEventState.Execute
                                        : ScreenInputEventState.Start,
                                };
                                _onLongPress?.Invoke(eventData);
                                if (eventData.state == ScreenInputEventState.Start)
                                    _invokedId.Add(fingerId);
                            }
                            break;
                        case TouchPhase.Ended:
                            if (CheckCanInvoke(fingerId))
                            {
                                _onLongPress?.Invoke(new ScreenInputEvent
                                {
                                    inputId = fingerId,
                                    screenPos = t.position,
                                    pressTime = Time.time - _idToStartTime[fingerId],
                                    tapCount = 1,
                                    state = ScreenInputEventState.End
                                });
                                RemoveInput(fingerId);
                            }
                            break;
                        default:
                            RemoveInput(fingerId);
                            break;
                    }
                }
#endif
            }
            else
            {
                var currentTime = Time.time;
#if ENABLE_INPUT_SYSTEM
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse == null) return;
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    _mouseStartTime = currentTime;
                }
                else if (mouse.leftButton.wasReleasedThisFrame)
                {
                    if (_mouseStartTime > 0f 
                        && currentTime - _mouseStartTime - _invokeMinTime > 0)
                        _onLongPress?.Invoke(new ScreenInputEvent
                        {
                            inputId = 1,
                            screenPos = mouse.position.value,
                            pressTime = Time.time - _mouseStartTime - _invokeMinTime,
                            tapCount = 1,
                            state = ScreenInputEventState.End
                        });
                    RemoveInput(1);
                }
                else if (_mouseStartTime > 0 && mouse.delta.ReadValue().sqrMagnitude > 5f)
                {
                    if (_mouseStartTime > 0f 
                        && currentTime - _mouseStartTime - _invokeMinTime > 0)
                        _onLongPress?.Invoke(new ScreenInputEvent
                        {
                            inputId = 1,
                            screenPos = mouse.position.value,
                            pressTime = Time.time - _mouseStartTime - _invokeMinTime,
                            tapCount = 1,
                            state = ScreenInputEventState.End
                        });
                    RemoveInput(1);
                }
                else if (_mouseStartTime > 0 && currentTime - _mouseStartTime - _invokeMinTime > 0)
                {
                    _onLongPress?.Invoke(new ScreenInputEvent
                    {
                        inputId = 1,
                        screenPos = mouse.position.value,
                        pressTime = Time.time - _mouseStartTime - _invokeMinTime,
                        tapCount = 1,
                        state = _mousePressInvoked ? ScreenInputEventState.Execute: ScreenInputEventState.Start,
                    });
                    _mousePressInvoked = true;
                }
#else
                // Input API的逻辑
                if (Input.GetMouseButtonDown(0))
                {
                    _mouseStartTime = currentTime;
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    if (_mouseStartTime > 0f 
                        && currentTime - _mouseStartTime - _invokeMinTime > 0)
                        _onLongPress?.Invoke(new ScreenInputEvent
                        {
                            inputId = 1,
                            screenPos = Input.mousePosition,
                            pressTime = Time.time - _mouseStartTime - _invokeMinTime,
                            tapCount = 1,
                            state = ScreenInputEventState.End
                        });
                    RemoveInput(1);
                }
                else if (_mouseStartTime > 0 && (Input.GetAxis("Mouse X") != 0f || Input.GetAxis("Mouse Y") != 0f))
                {
                    if (_mouseStartTime > 0f 
                        && currentTime - _mouseStartTime - _invokeMinTime > 0)
                        _onLongPress?.Invoke(new ScreenInputEvent
                        {
                            inputId = 1,
                            screenPos = Input.mousePosition,
                            pressTime = Time.time - _mouseStartTime - _invokeMinTime,
                            tapCount = 1,
                            state = ScreenInputEventState.End
                        });
                    RemoveInput(1);
                }
                else if (Input.GetMouseButton(0) && _mouseStartTime > 0 && currentTime - _mouseStartTime - _invokeMinTime > 0)
                {
                    _onLongPress?.Invoke(new ScreenInputEvent
                    {
                        inputId = 1,
                        screenPos = Input.mousePosition,
                        pressTime = Time.time - _mouseStartTime - _invokeMinTime,
                        tapCount = 1,
                        state = _mousePressInvoked ? ScreenInputEventState.Execute: ScreenInputEventState.Start,
                    });
                    _mousePressInvoked = true;
                }
#endif
            }
        }

        public void Dispose()
        {
            _enable = false;
            _onLongPress = null;
            _idToStartTime.Clear();
            _idToStartTime = null;
            _invokedId.Clear();
            _invokedId = null;
        }
    }
}