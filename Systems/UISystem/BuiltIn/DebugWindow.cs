using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

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
        
        private int _frameCount = 0;
        private float _frameTime = 0;
        private int _fixedFrameCount = 0;
        private float _fixedFrameTime = 0;
        
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