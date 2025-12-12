using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PowerCellStudio
{
    public class DynamicResolutionController : MonoBehaviour
    {
        [Header("动态分辨率设置")]
        [Range(0.1f, 1.0f)]
        public float minScale = 0.5f;
        [Range(0.5f, 1.0f)]
        public float maxScale = 1.0f;
        public int targetFPS = 59;
        public float adjustSpeed = 0.1f; // 调整速度
        public float checkInterval = 1f;
        
        [Header("Debug")]
        public bool showDebugGUI = true;

        private float _targetScale = 1.0f;
        private float _currentScale = 1.0f;
        private float _deltaTime = 0.0f;
        private int _frameCount;
        private float _currentFPS;

        void Start()
        {
            _targetScale = maxScale;
            _currentScale = maxScale;
            if (MainCamera.instance != null && MainCamera.instance.CameraCom != null)
            {
                MainCamera.instance.CameraCom.allowDynamicResolution = true;
            }
            // 使用SRP的Dynamic Resolution
            if (DynamicResolutionHandler.instance != null)
            {
                DynamicResolutionHandler.SetDynamicResScaler(ScaleFunc, DynamicResScalePolicyType.ReturnsMinMaxLerpFactor);
                DynamicResolutionHandler.instance.DynamicResolutionEnabled();
            }
        }

        void OnDestroy()
        {
            if (MainCamera.instance != null && MainCamera.instance.CameraCom != null)
            {
                MainCamera.instance.CameraCom.allowDynamicResolution = false;
            }
        
            if (DynamicResolutionHandler.instance != null)
            {
                DynamicResolutionHandler.SetDynamicResScaler(null, DynamicResScalePolicyType.ReturnsMinMaxLerpFactor);
#if UNITY_EDITOR
                // 退出播放时复原 RenderScale，否则会影响编辑器显示
                var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                if (pipeline != null)
                {
                    pipeline.renderScale = 1.0f;
                }
#endif
            }
            else
            {
                ScalableBufferManager.ResizeBuffers(1.0f, 1.0f);
            }
        }

        private bool _isPaused = false;
        private void OnApplicationPause(bool hasFocus)
        {
            _isPaused = hasFocus;
        }

        void Update()
        {
            if (_isPaused) return;
            _frameCount++;
            _deltaTime += Time.unscaledDeltaTime;
            if (_deltaTime > checkInterval)
            {
                CheckPerformance();
            }
            ApplySmoothResolution();
        }

        private void CheckPerformance()
        {
            _currentFPS = _frameCount / _deltaTime;
            _frameCount = 0;
            _deltaTime = 0f;

            // 根据帧率调整分辨率比例
            if (_currentFPS < targetFPS * 0.9f && _targetScale > minScale)
                _targetScale = Mathf.Max(minScale, _targetScale - adjustSpeed * checkInterval);
            else if (_currentFPS >= targetFPS && _targetScale < maxScale)
                _targetScale = Mathf.Min(maxScale, _targetScale + adjustSpeed * checkInterval);
        }

        public void ApplySmoothResolution()
        {
            if (Mathf.Approximately(_currentScale, _targetScale)) return;
            _currentScale = Mathf.MoveTowards(_currentScale, _targetScale, 0.5f * Time.unscaledDeltaTime);

            // SRP动态分辨率
            if (DynamicResolutionHandler.instance == null) // 非SRP，手动设置分辨率
            {
                // var currentResolution = ApplicationManager.instance.curResolution;
                // int width = Mathf.RoundToInt(currentResolution.x * _currentScale);
                // int height = Mathf.RoundToInt(currentResolution.y * _currentScale);
                // Screen.SetResolution(width, height, Screen.fullScreenMode);

                // 这允许在不改变窗口大小的情况下改变渲染缓冲区大小
                // 注意：这在 Editor 中可能不生效，取决于图形API (DX11/Metal通常支持)
                ScalableBufferManager.ResizeBuffers(_currentScale, _currentScale);
                Debug.Log($"ScalableBufferManager.ResizeBuffers to {_currentScale:F2}");
                return;
            }
#if UNITY_EDITOR
            // 因为 Editor 下 DynamicResolutionHandler 经常不生效，我们直接修改 RenderScale 来预览效果
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline != null)
            {
                pipeline.renderScale = _currentScale;
            }
#endif
            Debug.Log($"Dynamic Resolution Scale adjusted to {_currentScale:F2}");
        }

        // SRP动态分辨率回调
        private float ScaleFunc()
        {
            return _currentScale;
        }

#if UNITY_EDITOR || DEBUG || ENABLE_LOG
        private void OnGUI()
        {
            if (!showDebugGUI) return;

            // 在左上角绘制调试信息
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = 20;
            style.alignment = TextAnchor.MiddleLeft;
            style.normal.textColor = Color.white;

            string pipeline = DynamicResolutionHandler.instance != null ? "SRP (URP/HDRP)" : "Built-in (ScalableBuffer)";
            string status = $"FPS: {_currentFPS:F1} / {targetFPS}\n" +
                            $"Scale: {_targetScale:F2} ({(int)(_targetScale * 100)}%)\n" +
                            $"Resolution: {Screen.width * _targetScale:F0} x {Screen.height * _targetScale:F0}\n" +
                            $"Mode: {pipeline}";

            // 根据缩放比例改变背景颜色 (红色代表低分辨率，绿色代表原生)
            GUI.backgroundColor = Color.Lerp(Color.red, Color.green, _targetScale);

            GUILayout.BeginArea(new Rect(10, 10, 300, 120));
            GUILayout.Box(status, style);
            GUILayout.EndArea();
        }
#endif
    }
}