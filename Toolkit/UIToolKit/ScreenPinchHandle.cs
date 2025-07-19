using UnityEngine;
namespace PowerCellStudio
{
   public class ScreenPinchHandle : IScreenInputHandle
   {
      private event ScreenInputEventHandler _onPinch;
      private float _lastDistance = 0f;
      private Vector2 _lastPinchPos;
      public float _pinchThreshold = 0.01f;
      private float _startTime;
      public void RegisterInput(ScreenInputEventHandler action) => _onPinch += action;
      public void UnregisterInput(ScreenInputEventHandler action) => _onPinch -= action;

      public void OnEnable()
      {
         _lastDistance = 0f;
#if ENABLE_INPUT_SYSTEM
         UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerDown += OnFingerDown;
         UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerUp += OnFingerUp;
         UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerMove += OnFingerMove;
#endif
      }
      public void OnDisable()
      {
         _lastDistance = 0f;
#if ENABLE_INPUT_SYSTEM
         UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerDown -= OnFingerDown;
         UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerUp -= OnFingerUp;
         UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerMove -= OnFingerMove;
#endif
      }

      private void EndPinch()
      {
         _onPinch?.Invoke(new ScreenInputEvent
         {
            position = _lastPinchPos,
            pinchDelta = 0f, // Reset pinch delta on finger up
            state = ScreenInputEventState.End,
            pressTime = Time.time - _startTime
         });
         _lastDistance = 0f;
      }

#if ENABLE_INPUT_SYSTEM
      private void OnFingerDown(UnityEngine.InputSystem.EnhancedTouch.Finger finger)
      {
         if (_lastDistance == 0f) return;
         EndPinch();
      }

      private void OnFingerUp(UnityEngine.InputSystem.EnhancedTouch.Finger finger)
      {
         if (_lastDistance == 0f) return;
         EndPinch();
      }

      private void OnFingerMove(UnityEngine.InputSystem.EnhancedTouch.Finger finger)
      {
         if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count != 2) return;

         var touch0 = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0];
         var touch1 = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[1];
         float currentDistance = Vector2.Distance(touch0.screenPosition, touch1.screenPosition);
         float pinchDelta = currentDistance - _lastDistance;
         if (Mathf.Abs(pinchDelta) > _pinchThreshold)
         {
            _lastPinchPos = (touch0.screenPosition + touch1.screenPosition) / 2f;
            if (_lastDistance == 0f)
            {
               _startTime = Time.time;
            }
            _onPinch?.Invoke(new ScreenInputEvent
            {
               position = _lastPinchPos,
               pinchDelta = _lastDistance == 0f ? 0 : pinchDelta,
               state = _lastDistance == 0f ? ScreenInputEventState.Start : ScreenInputEventState.Execute,
               pressTime = Time.time - _startTime
            });
         }
         _lastDistance = currentDistance;
      }
#endif

      public void Dispose()
      {
         _onPinch = null;
         _lastDistance = 0f;
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
               float curDist = Vector2.Distance(t0.position, t1.position);
               float pinchDelta = curDist - _lastDistance;
               if (Mathf.Abs(pinchDelta) > 0.01f)
               {
                  _lastPinchPos = (t0.position + t1.position) / 2f;
                  if (_lastDistance == 0f)
                  {
                     _startTime = Time.time;
                  }
                  _onPinch?.Invoke(new ScreenInputEvent
                  {
                     position = _lastPinchPos,
                     pinchDelta = _lastDistance == 0f ? 0 : pinchDelta,
                     state = _lastDistance == 0f ? ScreenInputEventState.Start : ScreenInputEventState.Execute,
                     pressTime = Time.time - _startTime
                  });
               }
               _lastDistance = curDist;
            }
            else if (_lastDistance != 0f)
            {
               EndPinch();
            }
#endif
         }
         else
         {
#if ENABLE_INPUT_SYSTEM
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null) return;
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
               var mousePos = mouse.position.ReadValue();
               _onPinch?.Invoke(new ScreenInputEvent
               {
                  position = mousePos,
                  pinchDelta = 0,
                  pressTime = 0,
                  state = ScreenInputEventState.Start
               });
               _onPinch?.Invoke(new ScreenInputEvent
               {
                  position = mousePos,
                  pinchDelta = scroll * 10f,
                  pressTime = 0,
                  state = ScreenInputEventState.Execute
               });
               _onPinch?.Invoke(new ScreenInputEvent
               {
                  position = mousePos,
                  pinchDelta = 0,
                  pressTime = 0,
                  state = ScreenInputEventState.End
               });
            }
#else
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
               var mousePos = Input.mousePosition;
               _onPinch?.Invoke(new ScreenInputEvent
               {
                  position = mousePos,
                  pinchDelta = 0,
                  pressTime = 0,
                  state = ScreenInputEventState.Start
               });
               _onPinch?.Invoke(new ScreenInputEvent
               {
                  position = mousePos,
                  pinchDelta = scroll * 100f, // 旧Input缩放量通常较小，适当放大
                  pressTime = 0,
                  state = ScreenInputEventState.Execute
               });
               _onPinch?.Invoke(new ScreenInputEvent
               {
                  position = mousePos,
                  pinchDelta = 0,
                  pressTime = 0,
                  state = ScreenInputEventState.End
               });
            }
#endif
         }
      }
   }
}
