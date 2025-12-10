using System;
using System.Collections.Generic;
using UnityEngine;
using Time = UnityEngine.Time;

namespace PowerCellStudio
{
    /// <summary>
    /// TimeManager类，用于管理时间缩放和相关的时间记录。
    /// Manages time scaling and related time recording functionalities.
    /// </summary>
    public sealed class TimeManager : SingletonBase<TimeManager>, IFixedExecutionModule, IEventModule, IOnGameStartModule
    {
        #region define

        private class TimeScaler
        {
            public static readonly TimeScaler One = new TimeScaler(1f);

            private float _baseValue;
            internal float baseValue => _baseValue;
            private List<float> _blendValue = new List<float>();
            private float _calculatedValue;
            internal float calculatedValue => _calculatedValue;

            /// <summary>
            /// 构造函数：初始化TimeScaler with 基础值。
            /// Constructor: Initialize TimeScaler with a base value.
            /// </summary>
            /// <param name="v">基础值 / Base value</param>
            public TimeScaler(float v)
            {
                _baseValue = Mathf.Max(0f, v);
                Calculated();
            }

            /// <summary>
            /// 推入一个混合值。
            /// Push a blend value.
            /// </summary>
            /// <param name="v">混合值 / The blend value</param>
            public void PushBlend(float v)
            {
                if (v < 0f) ModuleLog.LogError<TimeManager>("TimeScaler was set a negative number");
                _blendValue.Add(Mathf.Max(v, 0f));
                Calculated();
            }

            /// <summary>
            /// 弹出混合值。
            /// Pop a blend value.
            /// </summary>
            public void PopBlend()
            {
                if (_blendValue.Count == 0) return;
                _blendValue.RemoveAt(_blendValue.Count - 1);
                Calculated();
            }

            /// <summary>
            /// 移除特定的混合值。
            /// Remove a specific blend value.
            /// </summary>
            /// <param name="v">要移除的混合值 / The blend value to be removed</param>
            public void RemoveBlend(float v)
            {
                for (var i = 0; i < _blendValue.Count; i++)
                {
                    if (Math.Abs(v - _blendValue[i]) > 0.002f) continue;
                    _blendValue.RemoveAt(i);
                    break;
                }
                Calculated();
            }

            /// <summary>
            /// 计算当前时间缩放值。
            /// Calculate the current time scaling value.
            /// </summary>
            private void Calculated()
            {
                var cal = Math.Max(_baseValue, 0f);
                foreach (var i in _blendValue)
                {
                    cal *= i;
                }
                _calculatedValue = cal;
            }

            /// <summary>
            /// 更新基础时间值。
            /// Update the base time value.
            /// </summary>
            /// <param name="val">新的基础值 / New base value</param>
            public void UpdateValue(float val)
            {
                if(val < 0f) ModuleLog.LogError<TimeManager>("TimeScaler was set a negative number");
                _baseValue = val;
                Calculated();
            }
        }
        
        [Serializable]
        public class TimeSave : IPersistenceData
        {
            [SerializeField] public long startTime;
        }
        
        #endregion
        
        private Stack<TimeScaler> _stack = new Stack<TimeScaler>();
        private float _target;
        private float _duration;
        private float _time;
        private bool _blending = false;
        private long _startTime;
        private long _timeWithoutPause;
        private long _unscaleTimeWithoutPause;
        private RefCountBool _paused = new RefCountBool();
        private bool _inTimeRecording = false;
        private float _globalScale = 1f;
        private float globalScale => _globalScale;
        
        /// <summary>
        /// 当前是否处于执行状态。
        /// Indicates whether the manager is in execution state.
        /// </summary>
        public bool inExecution { get; set; }

        /// <summary>
        /// 游戏启动时调用，模块初始化。
        /// Called when the game starts for module initialization.
        /// </summary>
        public void OnGameStart()
        {
            ModuleLog.Log<TimeManager>("Module Init!");
        }

        /// <summary>
        /// 初始化模块，清空所有时间缩放。
        /// Initialize the module and clear all time scaling.
        /// </summary>
        public void OnInit()
        {
            Clear();
        }

        /// <summary>
        /// 注册相关事件。
        /// Register related events.
        /// </summary>
        public void RegisterEvent()
        {
            EventManager.instance.onStartGame.AddListener(StartRecord);
            EventManager.instance.onResetGame.AddListener(StopRecord);
        }

        /// <summary>
        /// 注销相关事件。
        /// Unregister related events.
        /// </summary>
        public void UnRegisterEvent()
        {
            EventManager.instance.onStartGame.RemoveListener(StartRecord);
            EventManager.instance.onResetGame.RemoveListener(StopRecord);
        }

        /// <summary>
        /// 当前未暂停的时间戳（毫秒）。
        /// Gets the current timestamp without pauses (in milliseconds).
        /// </summary>
        public long timeWithoutPause => _timeWithoutPause;

        /// <summary>
        /// 当前未暂停的非缩放时间戳（毫秒）。
        /// Gets the current unscaled timestamp without pauses (in milliseconds).
        /// </summary>
        public long unscaleTimeWithoutPause => _unscaleTimeWithoutPause;

        /// <summary>
        /// 当前是否处于暂停状态。
        /// Indicates whether the manager is currently paused.
        /// </summary>
        public bool paused => _paused;

        /// <summary>
        /// 当前执行的时间缩放值。
        /// Gets the current time scaling value in execution.
        /// </summary>
        public float currentScale => _target;
        
        private long GetStartTime()
        {
            var timeSave = PlayerDataUtils.Read<TimeSave>(PlayerDataType.PlayerPrefs);
            if (timeSave == null)
            {
                timeSave = new TimeSave();
            }
            if (timeSave.startTime == 0L)
            {
                timeSave.startTime = DateTime.Now.Ticks;
                PlayerDataUtils.Save<TimeSave>(timeSave, PlayerDataType.PlayerPrefs);
            }
            return timeSave.startTime;
        }
        
        private void SaveStartTime()
        {
            var timeSave = new TimeSave()
            {
                startTime = _startTime + _unscaleTimeWithoutPause,
            };
            PlayerDataUtils.Save<TimeSave>(timeSave, PlayerDataType.PlayerPrefs);
        }

        /// <summary>
        /// 开始记录时间。
        /// Starts recording time.
        /// </summary>
        public void StartRecord()
        {
            if (_inTimeRecording) return;
            _inTimeRecording = true;
            _startTime = GetStartTime();
            _timeWithoutPause = _startTime;
            _unscaleTimeWithoutPause = _startTime;
        }

        /// <summary>
        /// 停止记录时间并保存。
        /// Stops recording time and saves the state.
        /// </summary>
        public void StopRecord()
        {
            Clear();
            SaveStartTime();
            if (!_inTimeRecording) return;
            _inTimeRecording = false;
        }

        /// <summary>
        /// 设置全局时间缩放。
        /// Set global time scaling.
        /// </summary>
        /// <param name="val">缩放值 / Scale value</param>
        /// <param name="duration">过渡时间 / Transition duration</param>
        public void SetGlobalScale(float val, float duration = 0)
        {
            _globalScale = Mathf.Max(0f, val);
            UpdateTarget(duration);
        }
        
        private TimeScaler GetCurTimeScaler()
        {
            if (_stack.Count == 0) _stack.Push(TimeScaler.One);
            return _stack.Peek();
        }

        /// <summary>
        /// 清空所有时间缩放，重置为1。
        /// Clear all time scaling, resetting to 1.
        /// </summary>
        public void Clear()
        {
            _stack.Clear();
            _stack.Push(new TimeScaler(1));
            UpdateTarget(0f);
        }

        /// <summary>
        /// 推入一个新的时间缩放。
        /// Push a new time scaling factor.
        /// </summary>
        /// <param name="timeScale">缩放值 / Scaling value</param>
        /// <param name="duration">过渡时间 / Transition duration</param>
        public void Push(float timeScale, float duration = 0f)
        {
            if (timeScale == 0)
            {
                ModuleLog.LogError<TimeManager>("If you want to push a zero TimeScaler, please use PauseTime().");
                return;
            }
            var newScale = new TimeScaler(timeScale);
            if (_paused)
            {
                ModuleLog.LogWarning<TimeManager>("Now time is paused, timeScale will not be set immediately.");
                var pauseTimeScale = _stack.Pop();
                _stack.Push(newScale);
                _stack.Push(pauseTimeScale);
                return;
            }
            _stack.Push(newScale);
            UpdateTarget(duration);
        }

        /// <summary>
        /// 弹出一个时间缩放。
        /// Pop a time scaling factor from the stack.
        /// </summary>
        /// <param name="duration">过渡时间 / Transition duration</param>
        public void Pop(float duration = 0)
        {
            if (_stack.Count == 1) return;
            if (_paused)
            {
                ModuleLog.LogWarning<TimeManager>("Now time is paused, timeScale will not be set immediately.");
                var pauseTimeScale = _stack.Pop();
                _stack.Pop();
                _stack.Push(pauseTimeScale);
                return;
            }
            _stack.Pop();
            UpdateTarget(duration);
        }

        /// <summary>
        /// 更新当前时间缩放值。
        /// Update the current time scaling value.
        /// </summary>
        /// <param name="newValue">新缩放值 / New scaling value</param>
        /// <param name="duration">过渡时间 / Transition duration</param>
        public void UpdateTimeScale(float newValue, float duration = 0)
        {
            if (newValue <= 0)
            {
                ModuleLog.LogError<TimeManager>("If you want to push a zero TimeScaler, please use PauseTime().");
                return;
            }
            if (_paused)
            {
                ModuleLog.LogWarning<TimeManager>("Now time is paused, timeScale will not be set immediately.");
                var pauseTimeScale = _stack.Pop();
                GetCurTimeScaler().UpdateValue(newValue);
                _stack.Push(pauseTimeScale);
                return;
            }
            GetCurTimeScaler().UpdateValue(newValue);
            UpdateTarget(duration);
        }

        /// <summary>
        /// 使用函数更新当前时间缩放值。
        /// Update the current time scaling value using a function.
        /// </summary>
        /// <param name="fun">缩放变换函数 / Scaling transformation function</param>
        /// <param name="duration">过渡时间 / Transition duration</param>
        public void UpdateTimeScale(Func<float, float> fun, float duration = 0)
        {
            if (_paused)
            {
                ModuleLog.LogWarning<TimeManager>("Now time is paused, timeScale will not be set immediately.");
                var pauseTimeScale = _stack.Pop();
                var nv = fun.Invoke(GetCurTimeScaler().baseValue);
                if (nv <= 0)
                {
                    ModuleLog.LogError<TimeManager>("If you want to push a zero TimeScaler, please use PauseTime().");
                    return;
                }
                GetCurTimeScaler().UpdateValue(nv);
                _stack.Push(pauseTimeScale);
                return;
            }
            var curTimeScaler = GetCurTimeScaler();
            var newValue = fun.Invoke(curTimeScaler.baseValue);
            if (newValue <= 0)
            {
                ModuleLog.LogError<TimeManager>("If you want to push a zero TimeScaler, please use PauseTime().");
                return;
            }
            curTimeScaler.UpdateValue(newValue);
            UpdateTarget(duration);
        }

        /// <summary>
        /// 推入一个混合缩放值。
        /// Push a blend scaling value.
        /// </summary>
        /// <param name="value">混合缩放值 / Blend scaling value</param>
        /// <param name="duration">过渡时间 / Transition duration</param>
        public void PushBlend(float value, float duration = 0f)
        {
            if (_paused)
            {
                ModuleLog.LogError<TimeManager>("Now time is paused, timeScale will not be set immediately.");
                var pauseTimeScale = _stack.Pop();
                GetCurTimeScaler().PushBlend(value);
                _stack.Push(pauseTimeScale);
                return;
            }
            var curTimeScaler = GetCurTimeScaler();
            curTimeScaler.PushBlend(value);
            UpdateTarget(duration);
        }

        /// <summary>
        /// 移除一个混合缩放值。
        /// Remove a blend scaling value.
        /// </summary>
        /// <param name="val">混合缩放值 / Blend scaling value</param>
        /// <param name="duration">过渡时间 / Transition duration</param>
        public void RemoveBlend(float val, float duration = 0f)
        {
            if (_paused)
            {
                ModuleLog.LogError<TimeManager>("Now time is paused, timeScale will not be set immediately.");
                var pauseTimeScale = _stack.Pop();
                GetCurTimeScaler().RemoveBlend(val);
                _stack.Push(pauseTimeScale);
                return;
            }
            var curTimeScaler = GetCurTimeScaler();
            curTimeScaler.RemoveBlend(val);
            UpdateTarget(duration);
        }

        /// <summary>
        /// 弹出一个混合缩放值。
        /// Pop a blend scaling value.
        /// </summary>
        /// <param name="duration">过渡时间 / Transition duration</param>
        public void PopBlend(float duration = 0f)
        {
            if (_paused)
            {
                ModuleLog.LogError<TimeManager>("Now time is paused, timeScale will not be set immediately.");
                var pauseTimeScale = _stack.Pop();
                GetCurTimeScaler().PopBlend();
                _stack.Push(pauseTimeScale);
                return;
            }
            var curTimeScaler = GetCurTimeScaler();
            curTimeScaler.PopBlend();
            UpdateTarget(duration);
        }
        
        private void UpdateTarget(float duration)
        {
            var tsReplace = _stack.Peek();
            _target = tsReplace.calculatedValue * _globalScale;
            _blending = true;
            _time = 0f;
            _duration = duration;
            EventManager.instance.onTimeScaleReplaced?.Invoke(tsReplace.calculatedValue);
        }

        /// <summary>
        /// 执行时间缩放的过渡和时间记录。
        /// Execute the transition and record scaling time.
        /// </summary>
        /// <param name="dt">deltaTime</param>
        public void FixedExecute(float dt)
        {
            if (_inTimeRecording && !_paused)
            {
                _timeWithoutPause += (long)(dt * 1000);
                _unscaleTimeWithoutPause += (long)(Time.fixedUnscaledDeltaTime * 1000);
            }
            if (!_blending) return;
            if (_time >= _duration)
            {
                _blending = false;
                Time.timeScale = _target;
                return;
            }
            _time += Time.fixedUnscaledDeltaTime;
            var progress = Mathf.Clamp01(_time / _duration);
            Time.timeScale = Mathf.Lerp(Time.timeScale, _target, progress);
        }

        /// <summary>
        /// 暂停时间。
        /// Pause the time.
        /// </summary>
        public void PauseTime()
        {
            _paused++;
            if (_paused.refCount > 1) return;
            var newScale = new TimeScaler(0);
            _stack.Push(newScale);
            UpdateTarget(0);
            EventManager.instance.onTimeScalePause?.Invoke(true);
        }

        /// <summary>
        /// 恢复时间。
        /// Resume the passage of time.
        /// </summary>
        public void ResumeTime()
        {
            if (!_paused) return;
            _paused--;
            if (_paused) return;
            Pop();
            UpdateTarget(0);
            Time.timeScale = _target;
            EventManager.instance.onTimeScalePause?.Invoke(false);
        }

        /// <summary>
        /// 执行慢动作（时间缩放），并在指定时间后自动恢复。
        /// Perform slow motion (time dilation), automatically recover after a specified time.
        /// </summary>
        /// <param name="minScaleValue">最小缩放值 / Minimum scale value</param>
        /// <param name="duration">慢动作持续时间 / Slow motion duration</param>
        /// <param name="recoverTime">恢复到正常速度所需时间 / Recovery time to normal speed</param>
        /// <param name="ignoreTimeScale">是否忽略时间缩放 / Ignore time scale</param>
        /// <returns>Coroutine</returns>
        public Coroutine DoSlowMotion(float minScaleValue, float duration = 1f, float recoverTime = 0.7f, bool ignoreTimeScale = true)
        {
            PushBlend(minScaleValue, duration);
            return ApplicationManager.instance.DelayedCall(duration, () => { RemoveBlend(minScaleValue, recoverTime); }, ignoreTimeScale);
        }
    }
}