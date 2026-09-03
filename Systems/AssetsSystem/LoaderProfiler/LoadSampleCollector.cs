using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace PowerCellStudio
{
    public class LoadSampleCollector : IDisposable
    {
        public const string ProfilerCategoryName = "Loading";
        // 当前帧收集的LoadSample总数
        public const string ActiveLoadsCounterName = "Loader Active Loads";
        // 当前帧收集的开始加载的LoadSample总数
        public const string BeginLoadsCounterName = "Loader Begin Loads";
        // 当前帧收集的完成加载的LoadSample总数
        public const string CompletedLoadsCounterName = "Loader Completed Loads";
        // 当前帧主动请求加载的bundle数量
        public const string BundleCountCounterName = "Loader AssetBundles";
        // 当前帧主动请求加载的最大依赖深度
        public const string DependencyDepthCounterName = "Loader Max Dependency Depth";
        
        public static readonly Guid ProfilerGuid = new Guid("a4d4e1a5-49db-4d4b-9c03-6ca1d0edc9a3");
        public const int ProfilerSampleTag = 0;

        public static LoadSampleCollector instance;

        private List<LoadSample> _loadSamples;
        private Dictionary<int, LoadSample> _loadSampleDict;
        private LoadSamplePool _loadSamplePool;
        private List<LoadSample> _addBuffer;
        private readonly Dictionary<string, int> _bundleNames = new Dictionary<string, int>(StringComparer.Ordinal);
        private ILoadDependencyProvider _dependencyProvider;
        public ILoadDependencyProvider dependencyProvider => _dependencyProvider;
        
        private static readonly ProfilerCounterValue<int> ActiveLoadsCounter =
            new ProfilerCounterValue<int>(ProfilerCategory.Loading, ActiveLoadsCounterName, ProfilerMarkerDataUnit.Count);
        
        private static readonly ProfilerCounterValue<int> BeginLoadsCounter =
            new ProfilerCounterValue<int>(ProfilerCategory.Loading, BeginLoadsCounterName, ProfilerMarkerDataUnit.Count);
        
        private static readonly ProfilerCounterValue<int> CompletedLoadsCounter =
            new ProfilerCounterValue<int>(ProfilerCategory.Loading, CompletedLoadsCounterName, ProfilerMarkerDataUnit.Count);
        
        private static readonly ProfilerCounterValue<int> BundleCountCounter =
            new ProfilerCounterValue<int>(ProfilerCategory.Loading, BundleCountCounterName, ProfilerMarkerDataUnit.Count);
        
        private static readonly ProfilerCounterValue<int> DependencyDepthCounter =
            new ProfilerCounterValue<int>(ProfilerCategory.Loading, DependencyDepthCounterName, ProfilerMarkerDataUnit.Count);
        
        private static readonly ProfilerMarker BeginLoadMarker =
            new ProfilerMarker(ProfilerCategory.Loading, "LoaderProfiler.BeginLoad");
        
        private static readonly ProfilerMarker StateChangeMarker =
            new ProfilerMarker(ProfilerCategory.Loading, "LoaderProfiler.SetLoadState");
        
        private readonly List<LoadProfilerFrameData> _metadataBuffer = new List<LoadProfilerFrameData>(64);
        private int _counterFrame = -1;
        private int _lastMetadataFrame = -1;
        private bool _isClearing;

        public LoadSampleCollector(ILoadDependencyProvider dependencyProvider = null)
        {
            _dependencyProvider = dependencyProvider;
            _loadSamples = new List<LoadSample>();
            _addBuffer = new List<LoadSample>();
            _loadSampleDict = new Dictionary<int, LoadSample>();
            _loadSamplePool = new LoadSamplePool();
        }

        public void Dispose()
        {
            for (var i = 0; i < _loadSamples.Count; i++)
            {
                var sample = _loadSamples[i];
                sample.Reset();
            }
            _loadSamples.Clear();
            _loadSamples = null;
            
            for (var i = 0; i < _addBuffer.Count; i++)
            {
                var sample = _addBuffer[i];
                sample.Reset();
            }
            _addBuffer.Clear();
            _addBuffer = null;
            
            _loadSamplePool.Dispose();
            _loadSamplePool = null;
            _bundleNames.Clear();
            _loadSampleDict.Clear();
            _loadSampleDict = null;
            
            _dependencyProvider?.Dispose();
            _dependencyProvider = null;
        }

        public IReadOnlyList<LoadSample> GetSamples()
        {
            return _loadSamples;
        }
        
        public bool HasLoadSample(int hashCode) => _loadSampleDict?.ContainsKey(hashCode) ?? false;

        public void BeginLoad(string assetPath, string assetBundleName, int hashCode)
        {
            EnsureProfilerFrame();
            using (BeginLoadMarker.Auto())
            {
                BeginLoadInternal(assetPath, assetBundleName, hashCode);
            }
        }

        private void BeginLoadInternal(string assetPath, string assetBundleName, int hashCode)
        {
            if (_loadSampleDict.ContainsKey(hashCode) || !Profiler.enabled)
                return;
            var sample = _loadSamplePool.Get();
            sample.runtimeFrameIndex = Time.frameCount;
            sample.beginThisFrame = true;
            sample.assetPath = assetPath;
            sample.assetBundleName = assetBundleName;
            sample.objectHashCode = hashCode;
            sample.loadState = LoadState.Begin;
            sample.assetBundleDependencies = _dependencyProvider?.GetAssetBundleDependencies(assetBundleName);
            sample.assetDependencies = _dependencyProvider?.GetAssetDependencies(assetPath);
            List<LoadSample> addList = _isClearing ? _addBuffer : _loadSamples;
            addList.Add(sample);
            _loadSampleDict[sample.objectHashCode] = sample;
            RecordProfilerLoad(sample);
        }

        public void SetLoadState(int hashCode, LoadState state)
        {
            using (StateChangeMarker.Auto())
            {
                EnsureProfilerFrame();
                if (!_loadSampleDict.TryGetValue(hashCode, out var sample)) return;
                if ((sample.loadState & state) > 0) return;
                sample.loadState = sample.loadState | state;
            }
        }

        public void ClearEndSample()
        {
            if (_isClearing)
                return;
            FlushProfilerFrame();
            _isClearing = true;
            for (int i = _loadSamples.Count - 1; i >= 0; i--)
            {
                var sample = _loadSamples[i];
                if ((sample.loadState & LoadState.End) == 0)
                    continue;

                var assetBundleName = sample.assetBundleName;
                _loadSampleDict.Remove(sample.objectHashCode);
                _loadSamplePool.Release(sample);
                _loadSamples.RemoveAt(i);
                // 移除相关的bundle计数
                if (string.IsNullOrEmpty(assetBundleName) || !_bundleNames.ContainsKey(assetBundleName)) 
                    continue;
                _bundleNames[assetBundleName]--;
                if (_bundleNames[assetBundleName] > 0) 
                    continue;
                _bundleNames.Remove(assetBundleName);
            }
            _isClearing = false;
            
            for (var i = 0; i < _addBuffer.Count; i++)
            {
                var sample = _addBuffer[i];
                _loadSamples.Add(sample);
            }
            _addBuffer.Clear();
        }

        /// <summary>
        /// Emits the current loader records to the selected Profiler frame.
        /// Call this once from the runtime owner's end-of-frame callback.
        /// </summary>
        public void FlushProfilerFrame()
        {
            var frame = Time.frameCount;
            if (_lastMetadataFrame == frame)
                return;

            _lastMetadataFrame = frame;
            EmitProfilerMetadata();
        }

        public void EnsureProfilerFrame()
        {
            var frame = Time.frameCount;
            if (_counterFrame == frame)
                return;

            _counterFrame = frame;
            _bundleNames.Clear();
        }

        private void RecordProfilerLoad(LoadSample sample)
        {
            if (!string.IsNullOrEmpty(sample.assetBundleName))
            {
                if (!_bundleNames.TryAdd(sample.assetBundleName, 0))
                {
                    _bundleNames[sample.assetBundleName]++;
                }
            }
        }

        private void EmitProfilerMetadata()
        {
#if ENABLE_PROFILER
            if (!Profiler.enabled)
                return;

            var tempBeginLoadsCounter = 0;
            var tempCompletedLoadsCounter = 0;
            var tempDependencyDepthCounter = 0;
            
            _metadataBuffer.Clear();
            for (var i = 0; i < _loadSamples.Count; i++)
            {
                var sample = _loadSamples[i];
                _metadataBuffer.Add(new LoadProfilerFrameData
                {
                    assetPath = sample.assetPath ?? string.Empty,
                    assetBundleName = sample.assetBundleName ?? string.Empty,
                    frameIndex = sample.runtimeFrameIndex,
                    objectHashCode = sample.objectHashCode,
                    state = (int)sample.loadState,
                    beginThisFrame = sample.beginThisFrame ? 1 : 0,
                });
                if (sample.beginThisFrame)
                {
                    sample.beginThisFrame = false;
                    tempBeginLoadsCounter++;
                }
                if ((sample.loadState & LoadState.End) > 0)
                    tempCompletedLoadsCounter++;
                
                tempDependencyDepthCounter = Math.Max(tempDependencyDepthCounter, GetDepth(sample));
            }
            BeginLoadsCounter.Value = tempBeginLoadsCounter;
            CompletedLoadsCounter.Value = tempCompletedLoadsCounter;
            DependencyDepthCounter.Value = tempDependencyDepthCounter;
            ActiveLoadsCounter.Value = _loadSamples.Count;
            BundleCountCounter.Value = _bundleNames.Count;
            
            Profiler.EmitFrameMetaData(ProfilerGuid, ProfilerSampleTag, _metadataBuffer.ToArray());
#endif
        }

        private static int GetDepth(LoadSample sample)
        {
            return sample.assetBundleDependencies?.Length ?? 0;
            // return Math.Max(sample.assetDependencies?.Length ?? 0,
            //     sample.assetBundleDependencies?.Length ?? 0);
        }

        private static int GetStableHashCode(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            unchecked
            {
                var hash = (int)2166136261;
                for (var i = 0; i < value.Length; i++)
                    hash = (hash ^ value[i]) * 16777619;
                return hash;
            }
        }
    }

    public interface ILoadDependencyProvider : IDisposable
    {
        string[] GetAssetDependencies(string assetPath);
        string[] GetAssetBundleDependencies(string assetBundleName);
    }

    [Serializable]
    [CLSCompliant(false)]
    public struct LoadProfilerFrameData
    {
        public FixedString4096Bytes assetPath;
        public FixedString4096Bytes assetBundleName;
        public int frameIndex;
        public int objectHashCode;
        public int state;
        public int beginThisFrame;
    }
}