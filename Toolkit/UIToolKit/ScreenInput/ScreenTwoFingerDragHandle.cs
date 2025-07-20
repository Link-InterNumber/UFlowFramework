using UnityEngine;
namespace PowerCellStudio
{
   public class ScreenTwoFingerDragHandle : IScreenInputHandle
   {
      private event ScreenInputEventHandler _onTwoFingerDrag;
      private Vector2 _lastMousePosition;
      private bool _isMouseDragging = false;
      private float _startTime;
      public void RegisterInput(ScreenInputEventHandler action)
      {
#if UNITY_EDITOR
         var allEvent = _onTwoFingerDrag?.GetInvocationList();
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
         _onTwoFingerDrag += action;
      }
      public void UnregisterInput(ScreenInputEventHandler action) => _onTwoFingerDrag -= action;

      public void OnEnable()
      {
         _isMouseDragging = false; _lastMousePosition = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
         UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerDown += OnFingerDown;
         UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerUp += OnFingerUp;
         UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerMove += OnFingerMove;
#endif
      }
      public void OnDisable()
      {
         _isMouseDragging = false; _lastMousePosition = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
         UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerDown -= OnFingerDown;
         UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerUp -= OnFingerUp;
         UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerMove -= OnFingerMove;
#endif
      }

      private void EndDragging()
      {
         _onTwoFingerDrag?.Invoke(new ScreenInputEvent
         {
            screenPos = _lastMousePosition,
            delta = Vector2.zero,
            state = ScreenInputEventState.End,
            pressTime = Time.time - _startTime
         });
         _isMouseDragging = false;
      }

#if ENABLE_INPUT_SYSTEM
      private void OnFingerDown(UnityEngine.InputSystem.EnhancedTouch.Finger finger)
      {
         if (!_isMouseDragging) return;
         EndDragging();
      }

      private void OnFingerUp(UnityEngine.InputSystem.EnhancedTouch.Finger finger)
      {
         if (!_isMouseDragging) return;
         EndDragging();
      }

      private void OnFingerMove(UnityEngine.InputSystem.EnhancedTouch.Finger finger)
      {
         if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count != 2) return;

         var touch0 = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0];
         var touch1 = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[1];
         Vector2 delta0 = touch0.delta;
         Vector2 delta1 = touch1.delta;
         if (delta0.sqrMagnitude > 0.01f
            && delta1.sqrMagnitude > 0.01f
            && Vector2.Dot(delta0.normalized, delta1.normalized) > 0.9f)
         {
            Vector2 avgDelta = (delta0 + delta1) / 2f;
            _lastMousePosition = (touch0.screenPosition + touch1.screenPosition) / 2f;
            if (!_isMouseDragging)
            {
               _startTime = Time.time;
            }
            _onTwoFingerDrag?.Invoke(new ScreenInputEvent
            {
               screenPos = _lastMousePosition,
               delta = avgDelta,
               state = _isMouseDragging ? ScreenInputEventState.Execute : ScreenInputEventState.Start,
               pressTime = Time.time - _startTime
            });
            _isMouseDragging = true;
         }
      }
#endif

      public void Dispose()
      {
         _onTwoFingerDrag = null;
         _isMouseDragging = false;
         _lastMousePosition = Vector2.zero;
      }
      public void OnUpdate()
      {
         if (Application.isMobilePlatform)
         {
#if !ENABLE_INPUT_SYSTEM
            if (Input.touchCount == 2)
            {
               var t0 = Input.GetTouch(0);
               var t1 = Input.GetTouch(1);
               Vector2 delta0 = t0.deltaPosition;
               Vector2 delta1 = t1.deltaPosition;
               if (delta0.sqrMagnitude > 0.01f 
                  && delta1.sqrMagnitude > 0.01f 
                  && Vector2.Dot(delta0.normalized, delta1.normalized) > 0.9f)
               {
                  Vector2 avgDelta = (delta0 + delta1) / 2f;
                  if (!_isMouseDragging)
                  {
                     _startTime = Time.time;
                  }
                  _lastMousePosition = (t0.position + t1.position) / 2f;
                  _onTwoFingerDrag?.Invoke(new ScreenInputEvent
                  {
                     screenPos = _lastMousePosition,
                     delta = avgDelta,
                     state = _isMouseDragging ? ScreenInputEventState.Execute : ScreenInputEventState.Start,
                     pressTime = Time.time - _startTime
                  });
                  _isMouseDragging = true;
               }
            }
            else if (_isMouseDragging)
            {
               EndDragging();
            }
#endif
         }
         else
         {
#if ENABLE_INPUT_SYSTEM
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null) return;
            if (mouse.middleButton.isPressed)
            {
               Vector2 currentPos = mouse.position.ReadValue();
               Vector2 delta = mouse.delta.ReadValue();
               if (delta.sqrMagnitude > 0.01f)
               {
                  _onTwoFingerDrag?.Invoke(new ScreenInputEvent
                  {
                     screenPos = currentPos,
                     delta = delta,
                     state = _isMouseDragging ? ScreenInputEventState.Execute : ScreenInputEventState.Start,
                     pressTime = Time.time - _startTime
                  });
                  _lastMousePosition = currentPos;
                  _isMouseDragging = true;
               }
            }
            else if (_isMouseDragging)
            {
               EndDragging();
            }
#else
            if (Input.GetMouseButton(2))
            {
               Vector2 currentPos = Input.mousePosition;
               Vector2 delta = currentPos - _lastMousePosition;
               if (delta.sqrMagnitude > 0.01f)
               {
                  _onTwoFingerDrag?.Invoke(new ScreenInputEvent
                  {
                     screenPos = currentPos,
                     delta = delta,
                     state = _isMouseDragging ? ScreenInputEventState.Execute : ScreenInputEventState.Start,
                     pressTime = Time.time - _startTime
                  });
                  _lastMousePosition = currentPos;
                  _isMouseDragging = true;
               }
            }
            else if (_isMouseDragging)
            {
               EndDragging();
            }
#endif
         }
      }
   }
}
