using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace PowerCellStudio
{
    public class SelectableLongPress : SelectableInteractor, ILongPressInteractor
    {
        public float longPressDelay = 0.2f;
        public float longPressDuration = 0.5f;
        public float interval = 0.3f;
        public bool ignoreTimeScale = false;
        
        private float _pressTimeStamp;
        private bool _pressing;

        private void OnEnable()
        {
            longPressDelay = Mathf.Max(longPressDelay, 0f);
            interval = Mathf.Max(interval, 0.1f);
            longPressDuration = Mathf.Max(longPressDuration, 0.1f);
        }

        public float processValue
        {
            get
            {
                if (!_pressing) return 0f;
                var currentTimeStamp = ignoreTimeScale ? Time.unscaledTime : Time.time;
                var tempPressDuration = currentTimeStamp - _pressTimeStamp;
                if (tempPressDuration < longPressDelay) return 0f;
                if (tempPressDuration < longPressDelay + longPressDuration)
                    return (tempPressDuration - longPressDelay) / longPressDuration;
                return 1f + (_pressDuration / interval);
            }
        }
        private bool _startLongPress;
        public bool isLongPressing => _pressing && _startLongPress;
        
        private float _pressDuration;
        public float pressDuration => _pressing ? _pressDuration: 0f;
        
        private bool _isConfirmed;
        public bool isConfirmed => _pressing && _isConfirmed;

        /// <summary>
        /// 长按开始触发一次
        /// </summary>
        public UnityEvent onStart = new UnityEvent();
        /// <summary>
        /// 长按达到时长后触发一次
        /// </summary>
        public UnityEvent onConfirm = new UnityEvent();
        /// <summary>
        /// 长按未达到时长，或者移出交互范围后触发一次
        /// </summary>
        public UnityEvent onCancel = new UnityEvent();
        /// <summary>
        /// 长按达到时长后松开或者移出交互范围后触发一次
        /// </summary>
        public UnityEvent onRelease = new UnityEvent();
        /// <summary>
        /// 长按达到时长后，固定时间间隔触发
        /// </summary>
        public UnityEvent onPressing = new UnityEvent();
        
        public UnityEvent<bool> _onActive = new UnityEvent<bool>();
        public UnityEvent<bool> onActive => _onActive;
        public void StopProcess()
        {
            if (!_pressing) return;
            _pressing = false;
            _startLongPress = false;
            _pressDuration = 0;
            if (!_isConfirmed)
            {
                onCancel.Invoke();
                _onActive.Invoke(false);
                return;
            }
            _isConfirmed = false;
            onRelease.Invoke();
            _onActive.Invoke(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressTimeStamp = ignoreTimeScale ? Time.unscaledTime : Time.time;
            _pressDuration = 0;
            _pressing = true;
            _isConfirmed = false;
            _startLongPress = false;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            StopProcess();
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            // do nothing...
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            StopProcess();
        }
        
        private void Update()
        {
            if (!_pressing) return;
            var currentTimeStamp = ignoreTimeScale ? Time.unscaledTime : Time.time;
            var tempPressDuration = currentTimeStamp - _pressTimeStamp;
            if (tempPressDuration < longPressDelay) return;

            if (!_startLongPress)
            {
                _startLongPress = true;
                onStart.Invoke();
                _onActive.Invoke(true);
            }
            
            if (!_isConfirmed && tempPressDuration >= longPressDelay + longPressDuration)
            {
                _isConfirmed = true;
                onConfirm.Invoke();
            }

            if (_isConfirmed)
            {
                var deltaTime = ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
                var previousInt = Mathf.FloorToInt(_pressDuration / interval);
                _pressDuration += deltaTime;
                var currentInt = Mathf.FloorToInt(_pressDuration / interval);
                if (currentInt == previousInt) return;
                onPressing.Invoke();
            }
        }
    }
}