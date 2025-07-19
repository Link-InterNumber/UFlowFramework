using UnityEngine;
namespace PowerCellStudio
{
   public class ScreenDragHandle : IScreenInputHandle
   {
      private event ScreenInputEventHandler _onDrag;
      private Vector2 _lastPos;
      private bool _isDragging = false;
      private float _startTime;
      public void RegisterInput(ScreenInputEventHandler action) => _onDrag += action;
      public void UnregisterInput(ScreenInputEventHandler action) => _onDrag -= action;
      public void OnEnable()
      {
#if ENABLE_INPUT_SYSTEM
         UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerDown += OnFingerDown;
         UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerUp += OnFingerUp;
         UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerMove += OnFingerMove;
#endif
      }
      public void OnDisable()
      {
#if ENABLE_INPUT_SYSTEM
         UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerDown -= OnFingerDown;
         UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerUp -= OnFingerUp;
         UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerMove -= OnFingerMove;
#endif
      }

      private void EenDragging()
      {
         _onDrag?.Invoke(new ScreenInputEvent
         {
            position = _lastPos,
            delta = Vector2.zero,
            pressTime = Time.time - _startTime,
            state = ScreenInputEventState.End
         });
         _isDragging = false;
      }

#if ENABLE_INPUT_SYSTEM
      private void OnFingerDown(UnityEngine.InputSystem.EnhancedTouch.Finger finger)
      {
         if (!_isDragging) return;
         EenDragging();
      }

      private void OnFingerUp(UnityEngine.InputSystem.EnhancedTouch.Finger finger)
      {
         if (!_isDragging) return;
         EenDragging();
      }

      private void OnFingerMove(UnityEngine.InputSystem.EnhancedTouch.Finger finger)
      {
         if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count != 1)
         {
            return;
         }
         var touch = finger.currentTouch;
         _onDrag?.Invoke(new ScreenInputEvent
         {
            position = touch.screenPosition,
            delta = touch.delta,
            pressTime = (float)touch.time,
            state = _isDragging ? ScreenInputEventState.Start : ScreenInputEventState.Execute,
         });
         _lastPos = finger.screenPosition;
         _isDragging = false;
      }
#endif

      public void Dispose()
      {
#if ENABLE_INPUT_SYSTEM
         UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerDown -= OnFingerDown;
         UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerUp -= OnFingerUp;
         UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerMove -= OnFingerMove;
#endif
         _onDrag = null;
         _isDragging = false;
         _lastPos = Vector2.zero;
      }

      public void OnUpdate()
      {
         if (Application.isMobilePlatform)
         {
#if !ENABLE_INPUT_SYSTEM
            // 单指触摸
            if (Input.touchCount == 1)
            {
               var t = Input.GetTouch(0);
               if (t.phase == TouchPhase.Moved)
               {
                  _onDrag?.Invoke(new ScreenInputEvent 
                  { 
                     position = t.position, 
                     delta = t.deltaPosition 
                     state = _isDragging ? ScreenInputEventState.Start : ScreenInputEventState.Execute
                  });
                  _isDragging = true;
                  _lastPos = t.position;
               }
            }
            else if (_isDragging)
            {
               _onDrag?.Invoke(new ScreenInputEvent 
               { 
                  position = _lastPos, 
                  delta = Vector2.zero, 
                  pressTime = Time.time - _startTime, 
                  state = ScreenInputEventState.End
               });
               _isDragging = false;
            }
#endif
         }
         else
         {
#if ENABLE_INPUT_SYSTEM
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null) return;
            if (mouse.leftButton.isPressed)
            {
               Vector2 cur = mouse.position.ReadValue();
               Vector2 delta = cur - _lastPos;
               if (delta.sqrMagnitude > 0.01f)
               {
                  _onDrag?.Invoke(new ScreenInputEvent
                  {
                     position = cur,
                     delta = delta,
                     pressTime = Time.time - _startTime,
                     state = _isDragging ? ScreenInputEventState.Start : ScreenInputEventState.Execute
                  });
                  _isDragging = true;
               }
               _lastPos = cur;
            }
            else if (mouse.leftButton.wasReleasedThisFrame && _isDragging)
            {
               _onDrag?.Invoke(new ScreenInputEvent
               {
                  position = mouse.position.ReadValue(),
                  delta = Vector2.zero,
                  pressTime = Time.time - _startTime,
                  state = ScreenInputEventState.End
               });
               _isDragging = false;
            }
#else
            if (Input.GetMouseButtonDown(0))
            {
               _lastPos = Input.mousePosition;
               _isDragging = false;
               _startTime = Time.time;
            }
            if (Input.GetMouseButton(0))
            {
               Vector2 cur = Input.mousePosition;
               Vector2 delta = cur - _lastPos;
               if (delta.sqrMagnitude > 0.01f)
               {
                  _onDrag?.Invoke(new ScreenInputEvent 
                  { 
                     position = cur, 
                     delta = delta ,
                     pressTime = Time.time - _startTime,
                     state = _isDragging ? ScreenInputEventState.Start : ScreenInputEventState.Execute
                  });
                  _isDragging = true;
               }
               _lastPos = cur;
            }
            if (Input.GetMouseButtonUp(0))
            {
               if (_isDragging)
                  _onDrag?.Invoke(new ScreenInputEvent 
                  { 
                     position = Input.mousePosition, 
                     delta = (Vector2)Input.mousePosition - _lastPos,
                     pressTime = Time.time - _startTime,
                     state = ScreenInputEventState.End
                  });
               _isDragging = false;
            }
#endif
         }
      }
   }
}
