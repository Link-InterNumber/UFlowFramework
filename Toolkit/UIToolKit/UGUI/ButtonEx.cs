using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace PowerCellStudio
{
    [AddComponentMenu("UI/ButtonEx", 30)]
    public class ButtonEx : Button
    {
        public bool enableLongPress = false;
        public bool longPressRepeat = false;
        [Min(0f)]
        public float longPressStartTime = 0.2f;
        [Min(0f)]
        public float longPressTriggerTime = 1f;
        [Min(0f)]
        public float longPressIntervalTime = 0.5f;
        public UnityEvent onLongPress;
        public UnityEvent onLongPressUp;

        private float _pressDownTime;
        public float longPressedTime => _pressDownTime > 0 ? Time.realtimeSinceStartup - _pressDownTime : 0f;
        private int _longPressRepeatTimes;
        public int longPressRepeatTimes => _longPressRepeatTimes;

        private bool _pressed;

        public enum PressState
        {
            None,
            Pressed,
            LongPressStarted,
            LongPressCancelled,
            LongPressedPerformed,
            LongPressedEnd
        }

        private PressState _pressState;
        public PressState pressState => _pressState;

        protected override void OnEnable()
        {
            base.OnEnable();
            _pressState = PressState.None;
            // 配置校验
            longPressStartTime = Mathf.Max(0f, longPressStartTime);
            longPressTriggerTime = Mathf.Max(0f, longPressTriggerTime);
            longPressIntervalTime = Mathf.Max(0f, longPressIntervalTime);
            if (enableLongPress)
            {
                if (longPressStartTime > longPressTriggerTime)
                {
                    UILogger.LogError("ButtonEx : longPressStartTime should not be greater then longPressTriggerTime!");
                }
            }
        }

        protected override void OnDisable()
        {
            _pressed = false;
            _submitPressed = false;
            _pressDownTime = 0;
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            _pressed = false;
            _submitPressed = false;
            _pressDownTime = 0;
            onLongPress?.RemoveAllListeners();
            onLongPress = null;
            onLongPressUp?.RemoveAllListeners();
            onLongPressUp = null;
            base.OnDestroy();
        }

        private void ResetLongPressState()
        {
            _pressed = false;
            _pressDownTime = 0;
            _pressState = PressState.None;
        }

        private void CheckLongPressUp()
        {
            if (enableLongPress && _pressDownTime > 0 &&
                Time.realtimeSinceStartup - _pressDownTime >= longPressStartTime)
            {
                if (_onLongPressInvoked)
                {
                    _pressState = PressState.LongPressedEnd;
                    onLongPressUp?.Invoke();
                }
                else
                {
                    _pressState = PressState.LongPressCancelled;
                }
            }
        }

        private bool _onLongPressInvoked;
        private bool _submitPressed;

        private void Update()
        {
            UpdateSubmitLongPress();

            if (!enableLongPress || !_pressed) return;
            if (_pressDownTime <= 0 || Time.realtimeSinceStartup - _pressDownTime < longPressStartTime) return;
            _pressState = PressState.LongPressStarted;
            if (_pressDownTime <= 0 || Time.realtimeSinceStartup - _pressDownTime < longPressTriggerTime) return;
            _pressState = PressState.LongPressedPerformed;
            if (!_onLongPressInvoked)
            {
                _onLongPressInvoked = true;
                onLongPress?.Invoke();
            }

            if (!longPressRepeat)
                return;
            var timeSincePressed = Time.realtimeSinceStartup - _pressDownTime - longPressTriggerTime;
            var timeCheckToInvoke = (_longPressRepeatTimes + 1) * longPressIntervalTime;
            if (timeSincePressed < timeCheckToInvoke) return;
            onLongPress?.Invoke();
            _longPressRepeatTimes++;
        }

        private void UpdateSubmitLongPress()
        {
            if (!enableLongPress) return;

            if (!_submitPressed)
            {
                if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject &&
                    IsSubmitPressedDown())
                {
                    BeginLongPress();
                    _submitPressed = true;
                }

                return;
            }

            if (!IsSubmitPressedUp()) return;

            var longPressInvoked = _onLongPressInvoked;
            CheckLongPressUp();
            ResetLongPressState();
            _submitPressed = false;

            if (!longPressInvoked)
            {
                base.OnSubmit(new BaseEventData(EventSystem.current));
            }
        }

        private static bool IsSubmitPressedDown()
        {
#if ENABLE_INPUT_SYSTEM
            return TryGetSubmitAction(out var submitAction) && submitAction.WasPressedThisFrame();
#else
            return Input.GetButtonDown("Submit");
#endif
        }

        private static bool IsSubmitPressedUp()
        {
#if ENABLE_INPUT_SYSTEM
            return TryGetSubmitAction(out var submitAction) && submitAction.WasReleasedThisFrame();
#else
            return Input.GetButtonUp("Submit");
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private static bool TryGetSubmitAction(out InputAction submitAction)
        {
            submitAction = null;
            if (EventSystem.current == null ||
                EventSystem.current.currentInputModule is not InputSystemUIInputModule inputModule)
            {
                return false;
            }

            submitAction = inputModule.submit?.action;
            return submitAction != null && submitAction.enabled;
        }
#endif

        private void BeginLongPress()
        {
            _pressState = PressState.Pressed;
            _pressed = true;
            _pressDownTime = Time.realtimeSinceStartup;
            _longPressRepeatTimes = 0;
            _onLongPressInvoked = false;
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            BeginLongPress();
            base.OnPointerDown(eventData);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {   
            CheckLongPressUp();
            base.OnPointerUp(eventData);
            ResetLongPressState();
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            CheckLongPressUp();
            base.OnPointerExit(eventData);
            ResetLongPressState();
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (enableLongPress && _pressDownTime > 0 &&
                Time.realtimeSinceStartup - _pressDownTime > longPressStartTime) return;
            base.OnPointerClick(eventData);
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            if (enableLongPress && _submitPressed) return;
            if (enableLongPress && _pressDownTime > 0 &&
                Time.realtimeSinceStartup - _pressDownTime > longPressStartTime) return;
            base.OnSubmit(eventData);
        }
    }
}