using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PowerCellStudio
{
    [AddComponentMenu("UI/ButtonEx", 30)]
    public class ButtonEx : Button
    {
        public bool enableLongPress = false;
        public bool longPressRepeat = false;
        public float longPressStartTime = 0.2f;
        public float longPressTriggerTime = 1f;
        public float longPressIntervalTime = 0.5f;
        public UnityEvent onLongPress;
        public UnityEvent onLongPressUp;

        private float _pressDownTime;
        public float longPressedTime => _pressDownTime > 0 ? Time.realtimeSinceStartup - _pressDownTime : 0f;
        private int _longPressRepeatTimes;
        public int longPressRepeatTimes => _longPressRepeatTimes;

        private bool _pressed;
        
        protected override void OnDisable()
        {
            _pressed = false;
            _pressDownTime = 0;
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            _pressed = false;
            _pressDownTime = 0;
            base.OnDestroy();
        }

        private bool _onLongPressInvoked;
        private void Update()
        {
            if (!enableLongPress || !_pressed) return;
            if (_pressDownTime <= 0 || Time.realtimeSinceStartup - _pressDownTime < longPressTriggerTime) return;
            if (!_onLongPressInvoked)
            {
                _onLongPressInvoked = true;
                onLongPress?.Invoke();
            }
            if(!longPressRepeat)
                return;
            var timeSincePressed = Time.realtimeSinceStartup - _pressDownTime - longPressTriggerTime;
            var timeCheckToInvoke = (_longPressRepeatTimes + 1) * longPressIntervalTime;
            if (timeSincePressed < timeCheckToInvoke) return;
            onLongPress?.Invoke();
            _longPressRepeatTimes++;
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            _pressed = true;
            _pressDownTime = Time.realtimeSinceStartup;
            _longPressRepeatTimes = 0;
            _onLongPressInvoked = false;
            base.OnPointerDown(eventData);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            if (enableLongPress && _pressDownTime > 0 &&
                Time.realtimeSinceStartup - _pressDownTime > longPressStartTime)
            {
                onLongPressUp?.Invoke();
            }
            _pressed = false;
            _pressDownTime = 0;
            base.OnPointerUp(eventData);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            _pressed = false;
            _pressDownTime = 0;
            base.OnPointerExit(eventData);
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if(enableLongPress && _pressDownTime > 0 && Time.realtimeSinceStartup - _pressDownTime > longPressStartTime) return;
            base.OnPointerClick(eventData);
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            if(enableLongPress && _pressDownTime > 0 && Time.realtimeSinceStartup - _pressDownTime > longPressStartTime) return;
            base.OnSubmit(eventData);
        }
    }
}