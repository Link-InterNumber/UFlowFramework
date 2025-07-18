using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace PowerCellStudio
{
   public class ScreenInputMinitor : MonoBehaviour
   {
      public float pinchThreshold = 0.01f;
      private float lastPinchDistance = 0f;
      private bool isDragging = false;
      private Vector2 lastDragPosition;

      private bool _isMouseLeftDown = false;
      private bool _isMouseDragging;
      private bool _isMouseMiddleDown = false;
      private float _mouseDownTime;

      public delegate void OnDrag(Vector2 delta);
      public delegate void OnPinch(float pinchDelta);
      public delegate void OnTap(Vector2 position);
      public delegate void OnTwoFingerDrag(Vector2 delta);

      public event OnDrag onDrag;
      public event OnPinch onPinch;
      public event OnTap onTap;
      public event OnTwoFingerDrag onTwoFingerDrag;

      void OnEnable()
      {
         EnhancedTouchSupport.Enable();
         Touch.onFingerDown += OnFingerDown;
         Touch.onFingerUp += OnFingerUp;
         Touch.onFingerMove += OnFingerMove;
      }

      void OnDisable()
      {
         EnhancedTouchSupport.Disable();
         Touch.onFingerDown -= OnFingerDown;
         Touch.onFingerUp -= OnFingerUp;
         Touch.onFingerMove -= OnFingerMove;
      }

      private void OnFingerDown(Finger finger)
      {
         if (Touch.activeTouches.Count == 1)
         {
            lastDragPosition = finger.screenPosition;
            isDragging = true;
         }
      }

      private void OnFingerUp(Finger finger)
      {
         if (Touch.activeTouches.Count == 0)
         {
            isDragging = false;
         }
      }

      private void OnFingerMove(Finger finger)
      {
         // Pinch
         if (Touch.activeTouches.Count == 2)
         {
            var touch0 = Touch.activeTouches[0];
            var touch1 = Touch.activeTouches[1];
            float currentDistance = Vector2.Distance(touch0.screenPosition, touch1.screenPosition);

            if (lastPinchDistance != 0f)
            {
               float pinchDelta = currentDistance - lastPinchDistance;
               if (Mathf.Abs(pinchDelta) > pinchThreshold)
               {
                  OnPinchHandle(pinchDelta);
               }
            }
            lastPinchDistance = currentDistance;

            // 判断两指移动方向是否接近一致
            Vector2 delta0 = touch0.delta;
            Vector2 delta1 = touch1.delta;
            if (Vector2.Dot(delta0.normalized, delta1.normalized) > 0.9f)
            {
               Vector2 avgDelta = (delta0 + delta1) / 2f;
               OnTwoFingerDragHandle(avgDelta);
            }
         }
         // Drag
         else if (Touch.activeTouches.Count == 1)
         {
            OnDragHandle(Touch.activeTouches[0].delta);
            lastPinchDistance = 0f;
         }
         else
         {
            lastPinchDistance = 0f;
         }
      }

      private void Update()
      {
         if (Application.platform == RuntimePlatform.WindowsPlayer
#if UNITY_EDITOR
            || Application.platform == RuntimePlatform.WindowsEditor
            || Application.platform == RuntimePlatform.OSXEditor
#endif
            || Application.platform == RuntimePlatform.OSXPlayer)
         {
            var mouse = Mouse.current;
            if (mouse == null) return;

            // 鼠标左键点击
            if (mouse.leftButton.wasPressedThisFrame)
            {
               _isMouseLeftDown = true;
               _mouseDownTime = Time.time;
            }
            if (mouse.leftButton.wasReleasedThisFrame)
            {
               if (!_isMouseDragging && Time.time - _mouseDownTime < 0.2f)
                  OnTapHandle(mouse.position.ReadValue());
               _isMouseLeftDown = false;
               _isMouseDragging = false;
            }
            // 鼠标左键拖动
            if (_isMouseLeftDown && mouse.delta.ReadValue().magnitude > 0.01f)
            {
               _isMouseDragging = true;
               OnDragHandle(mouse.delta.ReadValue());
            }

            // 鼠标滚轮缩放
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
               OnPinchHandle(scroll * 10f);
            }

            // 鼠标中键拖动
            if (mouse.middleButton.wasPressedThisFrame)
            {
               _isMouseMiddleDown = true;
            }
            if (mouse.middleButton.wasReleasedThisFrame)
            {
               _isMouseMiddleDown = false;
            }
            if (_isMouseMiddleDown && mouse.delta.ReadValue().magnitude > 0.01f)
            {
               OnTwoFingerDragHandle(mouse.delta.ReadValue());
            }
            return;
         }

         // Tap detection
         foreach (var touch in Touch.activeTouches)
         {
            // 只有未处于拖动状态时才触发Tap
            if (!isDragging
                && touch.phase == UnityEngine.InputSystem.TouchPhase.Ended
                && (touch.time - touch.startTime) < 0.2f
                && Vector2.Distance(touch.screenPosition, touch.startScreenPosition) < 5f) // 用距离阈值判断
            {
               OnTapHandle(touch.screenPosition);
            }
         }
      }

      private void OnDragHandle(Vector2 delta)
      {
         onDrag?.Invoke(delta);
         Debug.LogError($"Drag Detected: {delta}");
      }

      private void OnPinchHandle(float pinchDelta)
      {
         onPinch?.Invoke(pinchDelta);
         Debug.LogError($"Pinch Detected: {pinchDelta}");
      }

      private void OnTapHandle(Vector2 position)
      {
         onTap?.Invoke(position);
         Debug.LogError($"Tap Detected: {position}");
      }

      private void OnTwoFingerDragHandle(Vector2 delta)
      {
         onTwoFingerDrag?.Invoke(delta);
         Debug.LogError($"Two-Finger Drag Detected: {delta}");
      }
   }
}