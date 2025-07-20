using UnityEngine;
namespace PowerCellStudio
{
   public class ScreenTapHandle : IScreenInputHandle
   {
      private event ScreenInputEventHandler _onTap;
      private bool _isDragging = false;
      private float _startTime;
      private float _mouseReleaseLastTime;
      private int _mouseTapCount;
      public void RegisterInput(ScreenInputEventHandler action)
      {
#if UNITY_EDITOR
         var allEvent = _onTap?.GetInvocationList();
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
         _onTap += action;
      }
      public void UnregisterInput(ScreenInputEventHandler action) => _onTap -= action;
      public void OnEnable()
      {
#if ENABLE_INPUT_SYSTEM
         // UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerDown += OnFingerDown;
         UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerUp += OnFingerUp;
         // UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerMove += OnFingerMove;
#endif
      }
      public void OnDisable()
      {
#if ENABLE_INPUT_SYSTEM
         // UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerDown -= OnFingerDown;
         UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerUp -= OnFingerUp;
         // UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerMove -= OnFingerMove;
#endif
      }

#if ENABLE_INPUT_SYSTEM
      // private void OnFingerDown(UnityEngine.InputSystem.EnhancedTouch.Finger finger)
      // {
      //    if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count != 1)
      //    {
      //       _isDragging = false;
      //       return;
      //    }
      //    _lastPos = finger.currentTouch.screenPosition;
      //    _isDragging = false;
      //    _startTime = Time.time;
      // }

      private void OnFingerUp(UnityEngine.InputSystem.EnhancedTouch.Finger finger)
      {
         var touch = finger.currentTouch;
         if (!touch.isTap) return;
         _onTap?.Invoke(new ScreenInputEvent
         {
            inputId = touch.touchId,
            screenPos = touch.screenPosition,
            pressTime = (float)touch.time,
            tapCount = touch.tapCount
         });
      }

      // private void OnFingerMove(UnityEngine.InputSystem.EnhancedTouch.Finger finger)
      // {
      //    if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count != 1)
      //    {
      //       return;
      //    }
      //    var touch = finger.currentTouch;
      //    _onDrag?.Invoke(new ScreenInputEvent
      //    {
      //       screenPos = touch.screenPosition,
      //       delta = touch.delta,
      //       pressTime = (float)touch.time,
      //       state = _isDragging ? ScreenInputEventState.Start : ScreenInputEventState.Execute,
      //    });
      //    _lastPos = finger.screenPosition;
      //    _isDragging = false;
      // }
#endif

      public void Dispose()
      {
#if ENABLE_INPUT_SYSTEM
         UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerUp -= OnFingerUp;
#endif
         _onTap = null;
         _isDragging = false;
         _startTime = 0f;
      }
      public void OnUpdate()
      {
         if (Application.isMobilePlatform)
         {
#if !ENABLE_INPUT_SYSTEM
            if (Input.touchCount == 0) return;
            for (int i = 0; i < Input.touchCount; i++)
            {
               var t = Input.GetTouch(i);
               if (t.phase == TouchPhase.Began)
               {
                  _startTime = Time.time;
                  _isDragging = false;
               }
               if (t.phase == TouchPhase.Moved)
               {
                  if (t.deltaPosition.sqrMagnitude > 25f)
                     _isDragging = true;
               }
               if (t.phase == TouchPhase.Ended 
                  && !_isDragging
                  && (Time.time - _startTime) < 0.2f)
               {
                  _onTap?.Invoke(new ScreenInputEvent 
                  {
                     inputId = t.fingerId,
                     screenPos = t.position, 
                     pressTime = Time.time - _startTime, 
                     tapCount = t.tapCount 
                  });
               }
            }
#endif
         }
         else
         {
#if ENABLE_INPUT_SYSTEM
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame)
            {
               _startTime = Time.time;
               _isDragging = false;
               if (_startTime - _mouseReleaseLastTime > 0.3f)
               {
                  _mouseTapCount = 0;
               }
            }

            if (_startTime > 0 && mouse.delta.ReadValue().sqrMagnitude > 25f)
            {
               _isDragging = true;
            }

            if (mouse.leftButton.wasReleasedThisFrame && !_isDragging && Time.time - _startTime < 0.2f)
            {
               _mouseTapCount++;
               _onTap?.Invoke(new ScreenInputEvent
               {
                  screenPos = mouse.position.ReadValue(),
                  pressTime = Time.time - _startTime,
                  tapCount = _mouseTapCount
               });
               _startTime = 0;
               _mouseReleaseLastTime = Time.time;
            }
#else
            // Input API的逻辑
            if (Input.GetMouseButtonDown(0))
            {
               _startTime = Time.time;
               _isDragging = false;
               if (_startTime - _mouseReleaseLastTime > 0.3f)
               {
                  _mouseTapCount = 0;
               }
            }
            if (_startTime > 0 && (Input.GetAxis("Mouse X") != 0f || Input.GetAxis("Mouse Y") != 0f))
            {
               _isDragging = true;
            }
            if (Input.GetMouseButtonUp(0) && !_isDragging && Time.time - _startTime < 0.2f)
            {
               _mouseTapCount++;
               _onTap?.Invoke(new ScreenInputEvent
               {
                  screenPos = Input.mousePosition,
                  pressTime = Time.time - _startTime,
                  tapCount = _mouseTapCount
               });
               _startTime = 0;
               _mouseReleaseLastTime = Time.time;
            }
#endif
         }
      }
   }
}
