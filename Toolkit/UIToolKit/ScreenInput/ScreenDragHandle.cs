using UnityEngine;

namespace PowerCellStudio
{
   public class ScreenDragHandle : IScreenInputHandle
   {
      private bool _enable = false;
      public bool enable {get => _enable; set => _enable = value;}

      private event ScreenInputEventHandler _onDrag;
      private Vector2 _lastPos;
      private bool _isDragging = false;
      private float _startTime;
      public void RegisterInput(ScreenInputEventHandler action)
      {
#if UNITY_EDITOR
         var allEvent = _onDrag?.GetInvocationList();
         if (allEvent != null && allEvent.Length > 0)
         {
            foreach (var eve in allEvent)
            {
               var fun2 = eve as ScreenInputEventHandler;
               if (fun2 != action) continue;
               LinkLogger.LogError($"重复添加监听:[{action.Method.Name}]");
               return;
            }
         }
#endif
         _onDrag += action;
      }
      public void UnregisterInput(ScreenInputEventHandler action) => _onDrag -= action;
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

      private void EndDragging()
      {
         _onDrag?.Invoke(new ScreenInputEvent
         {
            screenPos = _lastPos,
            delta = Vector2.zero,
            pressTime = Time.time - _startTime,
            state = ScreenInputEventState.End
         });
         _isDragging = false;
#if ENABLE_INPUT_SYSTEM
         _fingerDragging = false;
#endif
      }

#if ENABLE_INPUT_SYSTEM
      private bool _fingerDragging = false;
      private void OnFingerDown(UnityEngine.InputSystem.EnhancedTouch.Finger finger)
      {
         if (_fingerDragging)
         {
            EndDragging();
         }

         _lastPos = finger.screenPosition;
         _startTime = Time.unscaledTime;
      }

      private void OnFingerUp(UnityEngine.InputSystem.EnhancedTouch.Finger finger)
      {
         if (!_fingerDragging) return;
         EndDragging();
      }

      private void OnFingerMove(UnityEngine.InputSystem.EnhancedTouch.Finger finger)
      {
         if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count != 1)
         {
            if (_fingerDragging)
            {
               EndDragging();
            }
            return;
         }
         var touch = finger.currentTouch;
         _onDrag?.Invoke(new ScreenInputEvent
         {
            screenPos = touch.screenPosition,
            delta = touch.delta,
            pressTime = Time.unscaledTime - _startTime,
            state = _fingerDragging ? ScreenInputEventState.Execute : ScreenInputEventState.Start,
         });
         _lastPos = finger.screenPosition;
         _fingerDragging = true;
      }
#endif

      public void Dispose()
      {
         OnDisable();
         _onDrag = null;
         _isDragging = false;
         _lastPos = Vector2.zero;
      }

      public void OnUpdate()
      {
         if (!_enable) return;
         if (Application.isMobilePlatform)
         {
#if !ENABLE_INPUT_SYSTEM
            // 单指触摸
            if (Input.touchCount == 1)
            {
               var t = Input.GetTouch(0);
               if (t.phase == TouchPhase.Moved)
               {
                  if (!_isDragging && t.deltaPosition.sqrMagnitude < 25f) return;
                  _onDrag?.Invoke(new ScreenInputEvent 
                  { 
                     screenPos = t.position, 
                     delta = t.deltaPosition, 
                     state = _isDragging ? ScreenInputEventState.Execute : ScreenInputEventState.Start
                  });
                  _isDragging = true;
                  _lastPos = t.position;
               }
            }
            else if (_isDragging)
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
            if (mouse.leftButton.isPressed)
            {
               if (mouse.leftButton.wasPressedThisFrame)
               {
                  _startTime = Time.unscaledTime;
               }

               var delta = mouse.delta.value;
               // if (!_isDragging && delta.sqrMagnitude < 25f) return;
               if (delta.sqrMagnitude > 0.01f)
               {
                  _onDrag?.Invoke(new ScreenInputEvent
                  {
                     screenPos = mouse.position.value,
                     delta = delta,
                     pressTime = Time.unscaledTime - _startTime,
                     state = _isDragging ? ScreenInputEventState.Execute : ScreenInputEventState.Start
                  });
                  _isDragging = true;
               }
               // _lastPos = cur;
            }
            else if (_isDragging)
            {
               _onDrag?.Invoke(new ScreenInputEvent
               {
                  screenPos = mouse.position.ReadValue(),
                  delta = Vector2.zero,
                  pressTime = Time.unscaledTime - _startTime,
                  state = ScreenInputEventState.End
               });
               _isDragging = false;
            }
#else
            if (!Input.mousePresent) return;
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
               if (!_isDragging && delta.sqrMagnitude < 25f) return;
               if (delta.sqrMagnitude > 0.01f)
               {
                  _onDrag?.Invoke(new ScreenInputEvent 
                  { 
                     screenPos = cur, 
                     delta = delta ,
                     pressTime = Time.time - _startTime,
                     state = _isDragging ? ScreenInputEventState.Execute : ScreenInputEventState.Start
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
                     screenPos = Input.mousePosition, 
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
