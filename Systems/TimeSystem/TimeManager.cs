using System;
using System.Collections.Generic;
using UnityEngine;
using Time = UnityEngine.Time;

namespace PowerCellStudio
{
    public sealed class TimeManager : SingletonBase<TimeManager>, IExecutionModule, IEventModule, IOnGameStartModule
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

            public TimeScaler(float v)
            {
                _baseValue = Mathf.Max(0f, v);
                Calculated();
            }

            public void PushBlend(float v)
            {
                if(v < 0f) ModuleLog<TimeManager>.Error("TimeScaler was set a negative number");
                _blendValue.Add(Math.Max(v, 0f));
                Calculated();
            }

            public void PopBlend()
            {
                if(_blendValue.Count == 0) return;
                _blendValue.RemoveAt(_blendValue.Count -1);
                Calculated();
            }

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

            private void Calculated()
            {
                var cal = Math.Max(_baseValue, 0f);
                foreach (var i in _blendValue)
                {
                    cal *= i;
                }
                _calculatedValue = cal;
            }

            public void UpdateValue(float val)
            {
                if(val < 0f) ModuleLog<TimeManager>.Error("TimeScaler was set a negative number");
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
        
        public bool inExecution { get; set; }

        public void OnGameStart()
        {
            ModuleLog<TimeManager>.Log("Module Init!");
        }
        
        public void OnInit()
        {
            Clear();
        }

        public void RegisterEvent()
        {
            EventManager.instance.onStartGame.AddListener(StartRecord);
            EventManager.instance.onResetGame.AddListener(StopRecord);
        }

        public void UnRegisterEvent()
        {
            EventManager.instance.onStartGame.RemoveListener(StartRecord);
            EventManager.instance.onResetGame.RemoveListener(StopRecord);
        }

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
        /// 时间戳
        /// </summary>
        public long timeWithoutPause => _timeWithoutPause;
        
        /// <summary>
        /// 时间戳
        /// </summary>
        public long unscaleTimeWithoutPause => _unscaleTimeWithoutPause;
        public bool paused => _paused;
        
        /// <summary>
        /// 当前执行的时间缩放值
        /// </summary>
        public float currentScale => _target;

        private long GetStartTime()
        {
            var timeSave = PlayerDataUtils.ReadPlayerPrefs<TimeSave>();
            if (timeSave.startTime == 0L)
            {
                timeSave.startTime = DateTime.Now.Ticks;
                PlayerDataUtils.SavePlayerPrefs<TimeSave>(timeSave);
            }
            return timeSave.startTime;
        }
        
        private void SaveStartTime()
        {
            var timeSave = new TimeSave()
            {
                startTime = _startTime + _unscaleTimeWithoutPause,
            };
            PlayerDataUtils.SavePlayerPrefs<TimeSave>(timeSave);
        }

        public void StartRecord()
        {
            if(_inTimeRecording) return;
            _inTimeRecording = true;
            _startTime = GetStartTime();
            _timeWithoutPause = _startTime;
            _unscaleTimeWithoutPause = _startTime;
        }
        
        public void StopRecord()
        {
            Clear();
            SaveStartTime();
            if(!_inTimeRecording) return;
            _inTimeRecording = false;
        }

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

        public void Clear()
        {
            _stack.Clear();
            _stack.Push(new TimeScaler(1));
            UpdateTarget(0f);
        }
        
        public void Push(float value, float duration = 0f)
        {
            if (value == 0)
            {
                ModuleLog<TimeManager>.LogError("if you want to push a zero TimeScaler, please use PauseTime().");
                return;
            }
            var newScale = new TimeScaler(value);
            if (_paused)
            {
                ModuleLog<TimeManager>.LogWarning("Now time is paused, timeScale will not be set immediately.");
                var pauseTimeScale = _stack.Pop();
                _stack.Push(newScale);
                _stack.Push(pauseTimeScale);
                return;
            }
            _stack.Push(newScale);
            UpdateTarget(duration);
        }
        
        public void Pop(float duration = 0)
        {
            if (_stack.Count == 1) return;
            if (_paused)
            {
                ModuleLog<TimeManager>.LogWarning("Now time is paused, timeScale will not be set immediately.");
                var pauseTimeScale = _stack.Pop();
                _stack.Pop();
                _stack.Push(pauseTimeScale);
                return;
            }
            _stack.Pop();
            UpdateTarget(duration);
        }
        
        public void UpdateTimeScale(float newValue, float duration = 0)
        {
            if (newValue <= 0)
            {
                ModuleLog<TimeManager>.LogError("if you want to push a zero TimeScaler, please use PauseTime().");
                return;
            }
            if (_paused)
            {
                ModuleLog<TimeManager>.LogWarning("Now time is paused, timeScale will not be set immediately.");
                var pauseTimeScale = _stack.Pop();
                GetCurTimeScaler().UpdateValue(newValue);
                _stack.Push(pauseTimeScale);
                return;
            }
            GetCurTimeScaler().UpdateValue(newValue);
            UpdateTarget(duration);
        }
        
        public void UpdateTimeScale(Func<float, float> fun, float duration = 0)
        {
            if (_paused)
            {
                ModuleLog<TimeManager>.LogWarning("Now time is paused, timeScale will not be set immediately.");
                var pauseTimeScale = _stack.Pop();
                var nv =fun.Invoke(GetCurTimeScaler().baseValue);
                if (nv <= 0)
                {
                    ModuleLog<TimeManager>.LogError("if you want to push a zero TimeScaler, please use PauseTime().");
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
                ModuleLog<TimeManager>.LogError("if you want to push a zero TimeScaler, please use PauseTime().");
                return;
            }
            curTimeScaler.UpdateValue(newValue);
            UpdateTarget(duration);
        }
        
        public void PushBlend(float value, float duration = 0f)
        {
            if (_paused)
            {
                ModuleLog<TimeManager>.LogError("Now time is paused, timeScale will not be set immediately.");
                var pauseTimeScale = _stack.Pop();
                GetCurTimeScaler().PushBlend(value);
                _stack.Push(pauseTimeScale);
                return;
            }
            var curTimeScaler = GetCurTimeScaler();
            curTimeScaler.PushBlend(value);
            // _blendScalers.Add(newScale);
            UpdateTarget(duration);
        }

        public void RemoveBlend(float val, float duration = 0f)
        {
            if (_paused)
            {
                ModuleLog<TimeManager>.LogError("Now time is paused, timeScale will not be set immediately.");
                var pauseTimeScale = _stack.Pop();
                GetCurTimeScaler().RemoveBlend(val);
                _stack.Push(pauseTimeScale);
                return;
            }
            var curTimeScaler = GetCurTimeScaler();
            curTimeScaler.RemoveBlend(val);
            UpdateTarget(duration);
        }
        
        public void PopBlend(float duration = 0f)
        {
            if (_paused)
            {
                ModuleLog<TimeManager>.LogError("Now time is paused, timeScale will not be set immediately.");
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
        
        public void Execute(float dt)
        {
            if (_inTimeRecording && !_paused)
            {
                _timeWithoutPause += (long)(Time.deltaTime * 1000);
                _unscaleTimeWithoutPause += (long)(Time.unscaledDeltaTime * 1000);
            }
            if (!_blending) return;
            // if (Mathf.Approximately(_target, Time.timeScale)) return;
            if (_time >= _duration)
            {
                _blending = false;
                Time.timeScale = _target;
                return;
            }
            _time += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(_time / _duration);
            Time.timeScale = Mathf.Lerp(Time.timeScale, _target, progress);
        }
        
        public void PauseTime()
        {
            _paused ++;
            if (_paused.refCount > 1) return;
            var newScale = new TimeScaler(0);
            _stack.Push(newScale);
            UpdateTarget(0);
            EventManager.instance.onTimeScalePause?.Invoke(true);
        }

        public void ResumeTime()
        {
            if (!_paused) return;
            _paused --;
            if (_paused) return;
            Pop();
            EventManager.instance.onTimeScalePause?.Invoke(false);
        }

        public Coroutine DoSlowMotion(float minScaleValue, float duration = 1f, float recoverTime = 0.7f,
            bool ignoreTimeScale = true)
        {
            PushBlend(minScaleValue, duration);
            return ApplicationManager.instance.DelayedCall(duration, () => { RemoveBlend(minScaleValue, recoverTime); }, ignoreTimeScale);
        }
    }
}