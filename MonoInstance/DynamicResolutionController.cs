using UnityEngine;
using UnityEngine.Rendering;

namespace PowerCellStudio
{
    public class DynamicResolutionController : MonoBehaviour
    {
        [Header("动态分辨率设置")]
        [Range(0.5f, 1.0f)]
        public float minScale = 0.5f;
        [Range(0.5f, 1.0f)]
        public float maxScale = 1.0f;
        public int targetFPS = 60f;
        public float adjustSpeed = 0.1f; // 调整速度
        public float checkInterval = 1f;

        private float _currentScale = 1.0f;
        private float _deltaTime = 0.0f;
        private int _frameCount;

        void Start()
        {
            _currentScale = maxScale;
            // 使用SRP的Dynamic Resolution
            if (DynamicResolutionHandler.instance != null)
            {
                DynamicResolutionHandler.SetDynamicResScaler(ScaleFunc, DynamicResScalePolicyType.ReturnsMinMaxLerpFactor);
                DynamicResolutionHandler.instance.SetDynamicResolutionEnabled(true);
            }
        }

        void Update()
        {
            _frameCount ++;
            _deltaTime += Time.unscaledDeltaTime;
            if (_deltaTime > checkInterval)
            {
                CheckPerformance();
            }
        }

        private void CheckPerformance()
        {
            float fps = _frameCount / _deltaTime;
            _frameCount = 0f;
            _deltaTime = 0f;

            // 根据帧率调整分辨率比例
            if (fps < targetFPS * 0.9f)
                _currentScale = Mathf.Max(minScale, _currentScale - adjustSpeed * checkInterval);
            else if (fps > targetFPS * 1.1f)
                _currentScale = Mathf.Min(maxScale, _currentScale + adjustSpeed * checkInterval);

            // SRP动态分辨率
            if (DynamicResolutionHandler.instance == null) // 非SRP，手动设置分辨率
            {
                var currentResolution = ApplicationManager.curResolution;
                int width = Mathf.RoundToInt(currentResolution.x * _currentScale);
                int height = Mathf.RoundToInt(currentResolution.y * _currentScale);
                Screen.SetResolution(width, height, Screen.fullScreenMode);
            }
        }

        // SRP动态分辨率回调
        private float ScaleFunc()
        {
            return _currentScale;
        }
    }
}