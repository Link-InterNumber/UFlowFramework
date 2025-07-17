using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

namespace PowerCellStudio
{
    public class ScreenInputMinitor : MonoBehaviour
    {
        public float pinchThreshold = 0.01f;
        private float lastPinchDistance = 0f;
        private bool isDragging = false;
        private Vector2 lastDragPosition;

        private Vector2 lastMousePosition;
        private bool isMouseDragging = false;
        private bool isMiddleMouseDragging = false;

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
            // Drag
            if (isDragging && Touch.activeTouches.Count == 1)
            {
                Vector2 delta = finger.screenPosition - lastDragPosition;
                lastDragPosition = finger.screenPosition;
                OnDrag(delta);
            }

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
                        OnPinch(pinchDelta);
                    }
                }
                lastPinchDistance = currentDistance;

                // 判断两指移动方向是否接近一致
                Vector2 delta0 = touch0.screenPosition - touch0.lastScreenPosition;
                Vector2 delta1 = touch1.screenPosition - touch1.lastScreenPosition;
                if (Vector2.Dot(delta0.normalized, delta1.normalized) > 0.9f)
                {
                    Vector2 avgDelta = (delta0 + delta1) / 2f;
                    OnTwoFingerDrag(avgDelta);
                }
            }
            else
            {
                lastPinchDistance = 0f;
            }
        }

        private void Update()
        {
            if (Application.platform == RuntimePlatform.WindowsPlayer)
            {
                var mouse = Mouse.current;
                if (mouse == null) return;

                // 鼠标左键点击
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    OnTap(mouse.position.ReadValue());
                    lastMousePosition = mouse.position.ReadValue();
                    isMouseDragging = true;
                }
                if (mouse.leftButton.wasReleasedThisFrame)
                {
                    isMouseDragging = false;
                }
                // 鼠标左键拖动
                if (isMouseDragging && mouse.leftButton.isPressed)
                {
                    Vector2 delta = mouse.position.ReadValue() - lastMousePosition;
                    lastMousePosition = mouse.position.ReadValue();
                    OnDrag(delta);
                }

                // 鼠标滚轮缩放
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    OnPinch(scroll * 10f);
                }

                // 鼠标中键拖动
                if (mouse.middleButton.wasPressedThisFrame)
                {
                    lastMousePosition = mouse.position.ReadValue();
                    isMiddleMouseDragging = true;
                }
                if (mouse.middleButton.wasReleasedThisFrame)
                {
                    isMiddleMouseDragging = false;
                }
                if (isMiddleMouseDragging && mouse.middleButton.isPressed)
                {
                    Vector2 delta = mouse.position.ReadValue() - lastMousePosition;
                    lastMousePosition = mouse.position.ReadValue();
                    OnTwoFingerDrag(delta);
                }
                return;
            }

            // Tap detection
            foreach (var touch in Touch.activeTouches)
            {
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended && touch.deltaTime < 0.2f && touch.screenPosition == touch.startScreenPosition)
                {
                    OnTap(touch.screenPosition);
                }
            }
        }

        private void OnDrag(Vector2 delta)
        {
            onDrag?.Invoke(delta);
        }

        private void OnPinch(float pinchDelta)
        {
            onPinch?.Invoke(pinchDelta);
        }

        private void OnTap(Vector2 position)
        {
            onTap?.Invoke(position);
        }

        private void OnTwoFingerDrag(Vector2 delta)
        {
            onTwoFingerDrag?.Invoke(delta);
        }
    }
}