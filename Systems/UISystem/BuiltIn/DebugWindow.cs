using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;
using System.Linq;

namespace PowerCellStudio
{
    [WindowInfo("Assets/Res/UI/DebugWindow.prefab")]
    public class DebugWindow : UIWindow, IUIStandAlone, IUIComponent
    {
        public Text txtDeltaTime;
        public Text txtScaleDeltaTime;
        public Text txtFPS;
        public Text txtMemoryUsage;
        public Text txtMemoryAllocated;
        public Text txtMemoryGraphicsDriver;
        public Text txtMemoryUnused;
        public Text txtMemoryMono;

        public Text txtFixedDeltaTime;
        public Text txtSetFixedDeltaTime;
        public Text txtFixedFPS;
        
        public Text txtTimeScale;
        public Slider sliderTimeScale;
        public InputField inputTimeScale;
        public InputField inputTargetFrameRate;
        public InputField inputTargetFixedFrameRate;
        public Text txtTargetFrameRate;
        public Text txtTargetFixedFrameRate;

        // log
        public Toggle tglLog;
        public Toggle tglWarning;
        public Toggle tglError;
        public Button btnClearLog;
        public RecycleScrollRect listLog;
        
        private int _frameCount = 0;
        private float _frameTime = 0;
        private int _fixedFrameCount = 0;
        private float _fixedFrameTime = 0;

        public override void RegisterEvent()
        {
            tglLog.isOn = true;
            tglWarning.isOn = true;
            tglError.isOn = true;
        }
        
        public override void OnOpen(object data)
        {
            txtTargetFrameRate.text = Application.targetFrameRate.ToString();
            sliderTimeScale.minValue = 0;
            sliderTimeScale.maxValue = 10;
            sliderTimeScale.value = Time.timeScale;
            _frameTime = Time.unscaledTime;
            _fixedFrameTime = Time.unscaledTime;
            txtTargetFixedFrameRate.text = Mathf.RoundToInt(1f / Time.fixedDeltaTime).ToString();
            inputTimeScale.inputType = InputField.InputType.Standard;
            inputTimeScale.contentType = InputField.ContentType.DecimalNumber;
            inputTargetFrameRate.inputType = InputField.InputType.Standard;
            inputTargetFrameRate.contentType = InputField.ContentType.IntegerNumber;
            inputTargetFixedFrameRate.inputType = InputField.InputType.Standard;
            inputTargetFixedFrameRate.contentType = InputField.ContentType.IntegerNumber;
            sliderTimeScale.onValueChanged.AddListener(OnTimeScaleSliderChange);
            inputTargetFrameRate.onEndEdit.AddListener(OnTargetFrameRateDropdownChange);
            inputTimeScale.onEndEdit.AddListener(OnInputTimeScale);
            inputTargetFixedFrameRate.onEndEdit.AddListener(OnTargetFixedFrameRateDropdownChange);
            
            DebugBtn.onLoged += OnLoged;
            btnClearLog.onClick.AddListener(OnClickClearLog);
            tglLog.onValueChanged.AddListener(OnClickLog);
            tglWarning.onValueChanged.AddListener(OnClickWarning);
            tglError.onValueChanged.AddListener(OnClickError);
            UpdateLogs();

        }

        private void OnTargetFixedFrameRateDropdownChange(string arg0)
        {
            if (int.TryParse(arg0, out var value))
            {
                if(value <= 0) return;
                Time.fixedDeltaTime = 1f / value;
                txtTargetFixedFrameRate.text = value.ToString();
            }
        }

        private void OnInputTimeScale(string arg0)
        {
            if (float.TryParse(arg0, out var value))
            {
                sliderTimeScale.value = value;
            }
        }

        public override void OnClose()
        {
            sliderTimeScale.onValueChanged.RemoveListener(OnTimeScaleSliderChange);
            inputTargetFrameRate.onEndEdit.RemoveListener(OnTargetFrameRateDropdownChange);
            inputTimeScale.onEndEdit.RemoveListener(OnInputTimeScale);
            inputTargetFixedFrameRate.onEndEdit.RemoveListener(OnTargetFixedFrameRateDropdownChange);
            DebugBtn.onLoged -= OnLoged;
            btnClearLog.onClick.RemoveListener(OnClickClearLog);
            tglLog.onValueChanged.RemoveListener(OnClickLog);
            tglWarning.onValueChanged.RemoveListener(OnClickWarning);
            tglError.onValueChanged.RemoveListener(OnClickError);
        }

        private void OnLoged(DebugBtn.LogInfo logInfo)
        {
            switch(logInfo.logType)
            {
                case LogType.Log:
                    if (!tglLog.isOn) return;
                    break;
                case LogType.Warning:
                    if (!tglWarning.isOn) return;
                    break;
                case LogType.Error:
                    if (!tglError.isOn) return;
                    break;
                default:
                    return;
            }
            listLog.AddItem(DebugBtn.logInfos.Count - 1, logInfo);
        }

        private void UpdateLogs()
        {
            var fliterLogs = DebugBtn.logInfos.Where(o => 
            {
                switch(o.logType)
                {
                    case LogType.Log:
                        if (!tglLog.isOn) return false;
                        break;
                    case LogType.Warning:
                        if (!tglWarning.isOn) return false;
                        break;
                    case LogType.Error:
                        if (!tglError.isOn) return false;
                        break;
                    default:
                        return false;
                }
                return true;
            }).ToList();
            listLog.UpdateList(fliterLogs);
        }

        private void OnClickClearLog()
        {
            DebugBtn.logInfos.Clear();
            listLog.Clear();
        }

        private void OnClickLog(bool isOn)
        {
            UpdateLogs();
        }

        private void OnClickWarning(bool isOn)
        {
            UpdateLogs();
        }

        private void OnClickError(bool isOn)
        {
            UpdateLogs();
        }
        
        private void OnTimeScaleSliderChange(float arg0)
        {
            TimeManager.instance.SetGlobalScale(arg0);
        }
        
        private void OnTargetFrameRateDropdownChange(string arg0)
        {
            if (int.TryParse(arg0, out var value))
            {
                Application.targetFrameRate = value;
                txtTargetFrameRate.text = value.ToString();
            }
        }

        public override void OnFocus()
        {
            
        }

        private void FixedUpdate()
        {
            _fixedFrameCount++;
            if(Time.unscaledTime - _fixedFrameTime > 1)
            {
                txtFixedFPS.text = $"{_fixedFrameCount / (Time.unscaledTime - _fixedFrameTime):N1}";
                txtFixedDeltaTime.text = $"{(Time.unscaledTime - _fixedFrameTime) / _fixedFrameCount:N4}";
                txtSetFixedDeltaTime.text = $"{Time.fixedDeltaTime:N4}";
                _fixedFrameCount = 0;
                _fixedFrameTime = Time.unscaledTime;
            }
        }

        private void Update()
        {
            txtMemoryUsage.text = $"{Profiler.GetTotalReservedMemoryLong() / 1024 / 1024}MB";
            txtMemoryGraphicsDriver.text = $"{Profiler.GetAllocatedMemoryForGraphicsDriver() / 1024 / 1024}MB";
            txtMemoryAllocated.text = $"{Profiler.GetTotalAllocatedMemoryLong() / 1024 / 1024}MB";
            txtMemoryMono.text = $"{Profiler.GetMonoUsedSizeLong() / 1024 / 1024}MB";
            txtMemoryUnused.text = $"{Profiler.GetTotalUnusedReservedMemoryLong() / 1024 / 1024}MB";

            _frameCount++;
            if(Time.unscaledTime - _frameTime > 1)
            {
                txtFPS.text = $"{_frameCount / (Time.unscaledTime - _frameTime):N1}";
                txtDeltaTime.text = $"{(Time.unscaledTime - _frameTime) / _frameCount:N4}";
                txtScaleDeltaTime.text = $"{((Time.unscaledTime - _frameTime) / _frameCount * Time.timeScale):N4}";
                _frameCount = 0;
                _frameTime = Time.unscaledTime;
                txtTimeScale.text = $"{Time.timeScale:N2}";
            }
        }
    }
}