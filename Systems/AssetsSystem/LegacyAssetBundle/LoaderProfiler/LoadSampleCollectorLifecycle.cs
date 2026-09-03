using UnityEngine;

namespace PowerCellStudio
{
    /// <summary>
    /// 管理 LoadSampleCollector 的创建、每帧刷新和销毁。
    /// Manages LoadSampleCollector creation, per-frame flushing, and disposal.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LoadSampleCollectorLifecycle : MonoBehaviour
    {
#if UNITY_EDITOR
        private LoadSampleCollector _collector;
        private bool _isInitialized;

        /// <summary>
        /// 当前加载数据收集器。
        /// Gets the current load sample collector.
        /// </summary>
        public LoadSampleCollector Collector => _collector;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            InitializeCollector();
        }

        private void OnDestroy()
        {
            DisposeCollector();
        }

        private void OnApplicationQuit()
        {
            DisposeCollector();
        }

        // private void Update()
        // {
        //     if (!_isInitialized || _collector == null)
        //         return;
        //     _collector.EnsureProfilerFrame();
        // }

        private void LateUpdate()
        {
            if (!_isInitialized || _collector == null)
                return;

            // 在帧末将当前帧的 Loader 数据写入 Unity Profiler。
            // Flush the current frame's loader data to the Unity Profiler.
            _collector.FlushProfilerFrame();

            // 清理已经完成的样本，并保留仍在加载中的样本。
            // Remove completed samples while retaining active samples.
            _collector.ClearEndSample();
            
            _collector.EnsureProfilerFrame();
        }

        private void InitializeCollector()
        {
            if (_isInitialized)
                return;

// #if UNITY_EDITOR
            _collector = new LoadSampleCollector(
                new EditorLoadDependencyProvider());
// #else
//             _collector = new LoadSampleCollector();
// #endif

            LoadSampleCollector.instance = _collector;
            _isInitialized = true;
        }

        private void DisposeCollector()
        {
            if (!_isInitialized)
                return;

            if (LoadSampleCollector.instance == _collector)
                LoadSampleCollector.instance = null;

            _collector?.Dispose();
            _collector = null;
            _isInitialized = false;
        }
#endif
    }
}