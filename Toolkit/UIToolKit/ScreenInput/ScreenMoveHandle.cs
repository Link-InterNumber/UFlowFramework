using UnityEngine;

namespace PowerCellStudio
{
    public class ScreenMoveHandle : IScreenInputHandle
    {
        private bool _enable = false;

        public bool enable
        {
            get => _enable;
            set => _enable = value;
        }

        private event ScreenInputEventHandler _onMove;

        public void RegisterInput(ScreenInputEventHandler action)
        {
#if UNITY_EDITOR
            var allEvent = _onMove?.GetInvocationList();
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
            _onMove += action;
        }

        public void UnregisterInput(ScreenInputEventHandler action) => _onMove -= action;

        public void OnEnable()
        {
            if (_enable) return;
            _enable = true;
#if ENABLE_INPUT_SYSTEM
            UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerDown += OnFingerDown;
            UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerUp += OnFingerUp;
            UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerMove += OnFingerMove;
#endif
        }

        public void OnDisable()
        {
            _enable = false;
#if ENABLE_INPUT_SYSTEM
            UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerDown -= OnFingerDown;
            UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerUp -= OnFingerUp;
            UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerMove -= OnFingerMove;
#endif
        }

        private float _lastPressTime;
        private int _currentId;


#if ENABLE_INPUT_SYSTEM
        private void OnFingerDown(UnityEngine.InputSystem.EnhancedTouch.Finger finger)
        {
            if (_currentId > 0) return;
            var touch = finger.currentTouch;
            _onMove?.Invoke(new ScreenInputEvent
            {
                inputId = touch.touchId,
                screenPos = touch.screenPosition,
                delta = Vector2.zero,
                pressTime = 0,
                state = ScreenInputEventState.Execute
            });
            _lastPressTime = Time.unscaledTime;
            _currentId = touch.touchId;
        }

        private void OnFingerUp(UnityEngine.InputSystem.EnhancedTouch.Finger finger)
        {
            if (finger.currentTouch.touchId != _currentId) return;
            _currentId = -1;
            var touch = finger.currentTouch;
            _onMove?.Invoke(new ScreenInputEvent
            {
                inputId = touch.touchId,
                screenPos = touch.screenPosition,
                delta = touch.delta,
                pressTime = Time.unscaledTime - _lastPressTime,
                state = ScreenInputEventState.End
            });
            _lastPressTime = 0;
        }

        private void OnFingerMove(UnityEngine.InputSystem.EnhancedTouch.Finger finger)
        {
            if (finger.currentTouch.touchId != _currentId) return;

            var touch = finger.currentTouch;
            _onMove?.Invoke(new ScreenInputEvent
            {
                inputId = touch.touchId,
                screenPos = touch.screenPosition,
                delta = touch.delta,
                pressTime = Time.unscaledTime - _lastPressTime,
                state = ScreenInputEventState.Execute
            });
        }
#endif

        private bool _mouseInScreen;
        private Vector2 _lastMousePos;

        public void OnUpdate()
        {
            if (!_enable) return;
            if (Application.isMobilePlatform)
            {
#if !ENABLE_INPUT_SYSTEM
                // 单指触摸
                if (Input.touchCount == 1 && _currentId < 0)
                {
                    var t = Input.GetTouch(0);
                    _currentId = t.fingerId;
                    _lastPressTime = Time.unscaledTime;
                    _onMove?.Invoke(new ScreenInputEvent
                    {
                        inputId = t.fingerId,
                        screenPos = t.position,
                        delta = t.deltaPosition,
                        state = ScreenInputEventState.Execute
                    });
                    return;
                }
                if (Input.touchCount > 0)
                {
                    for (int i = 0; i < Input.touchCount; i++)
                    {
                        var t = Input.GetTouch(i);
                        if (t.fingerId != _currentId || t.phase != TouchPhase.Moved) continue;
                        _lastMousePos = t.position;
                        _onMove?.Invoke(new ScreenInputEvent
                        {
                            inputId = t.fingerId,
                            screenPos = t.position,
                            delta = t.deltaPosition,
                            pressTime = Time.unscaledTime - _lastPressTime,
                            state = ScreenInputEventState.Execute
                        });
                    }
                }
                else if (_lastPressTime > 0)
                {
                    _onMove?.Invoke(new ScreenInputEvent
                    {
                        inputId = _currentId,
                        screenPos = _lastMousePos,
                        delta = Vector2.zero,
                        pressTime = Time.unscaledTime - _lastPressTime,
                        state = ScreenInputEventState.End
                    });
                    _lastPressTime = 0;
                    _currentId = -1;
                }
#endif
            }
            else
            {
#if ENABLE_INPUT_SYSTEM
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse == null) return;
                var delta = mouse.delta.value;
                if (mouse.leftButton.isPressed && _lastPressTime <= 0)
                {
                    _lastPressTime = Time.unscaledTime;
                }
                else if (!mouse.leftButton.isPressed && _lastPressTime > 0)
                {
                    _lastPressTime = 0;
                }
                
                var mousePos = mouse.position.value;
                var mousePosInScreen = mousePos.x >= 0 && mousePos.y >= 0 
                                                       && mousePos.x < Screen.width && mousePos.y <= Screen.height;
                if (delta.sqrMagnitude > 1f && mousePosInScreen)
                {
                    _onMove?.Invoke(new ScreenInputEvent
                    {
                        screenPos = mouse.position.value,
                        delta = delta,
                        pressTime = _lastPressTime > 0f ? Time.unscaledTime - _lastPressTime : 0f,
                        state = ScreenInputEventState.Execute
                    });
                }

                if (_mouseInScreen && !mousePosInScreen)
                {
                    _onMove?.Invoke(new ScreenInputEvent
                    {
                        screenPos = mouse.position.value,
                        delta = delta,
                        pressTime = _lastPressTime > 0f ? Time.unscaledTime - _lastPressTime : 0f,
                        state = ScreenInputEventState.End
                    });
                }
                _mouseInScreen = mousePosInScreen;

#else
                if (!Input.mousePresent) return;
                if (Input.GetMouseButtonDown(0))
                {
                    _lastPressTime = Time.time;
                }
                else
                {
                    _lastPressTime = 0;
                }
                Vector2 mousePos = Input.mousePosition;
                var delta = mousePos - _lastMousePos;
                _lastMousePos = mousePos;
                var mousePosInScreen = mousePos.x >= 0 && mousePos.y >= 0 
                                                       && mousePos.x < Screen.width && mousePos.y <= Screen.height;
                if (delta.sqrMagnitude > 1f && mousePosInScreen)
                {
                    _onMove?.Invoke(new ScreenInputEvent
                    {
                        screenPos = mousePos,
                        delta = delta,
                        pressTime = _lastPressTime > 0f ? Time.unscaledTime - _lastPressTime : 0f,
                        state = ScreenInputEventState.Execute
                    });
                }

                if (_mouseInScreen && !mousePosInScreen)
                {
                    _onMove?.Invoke(new ScreenInputEvent
                    {
                        screenPos = mousePos,
                        delta = delta,
                        pressTime = _lastPressTime > 0f ? Time.unscaledTime - _lastPressTime : 0f,
                        state = ScreenInputEventState.End
                    });
                }
                _mouseInScreen = mousePosInScreen;
#endif
            }
        }

        public void Dispose()
        {
            OnDisable();
            _onMove = null;
            _lastPressTime = 0;
            _currentId = -1;
        }
    }
}