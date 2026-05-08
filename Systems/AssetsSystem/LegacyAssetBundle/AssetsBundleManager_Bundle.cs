using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Pool;

namespace PowerCellStudio
{
    public partial class AssetsBundleManager
    {
        public static string MainBundleName
        {
            get
            {
#if UNITY_EDITOR
                var folds = Directory.GetDirectories(Application.streamingAssetsPath);
                if (folds == null || folds.Length == 0) return "StreamingAssets";
                return Path.GetFileNameWithoutExtension(folds[0]);
#endif
                switch (Application.platform)
                {
                    case RuntimePlatform.OSXEditor:
                    case RuntimePlatform.OSXPlayer:
                        return "StandaloneOSX";
                    case RuntimePlatform.WindowsPlayer:
                    case RuntimePlatform.WindowsEditor:
                        return "StandaloneWindows";
                    case RuntimePlatform.IPhonePlayer:
                        return "iOS";
                    case RuntimePlatform.Android:
                        return "Android";
                    case RuntimePlatform.LinuxPlayer:
                    case RuntimePlatform.LinuxEditor:
                        return "StandaloneLinux";
                    case RuntimePlatform.WebGLPlayer:
                        return "WebGL";
                    case RuntimePlatform.PS4:
                        return "PS4";
                    case RuntimePlatform.tvOS:
                        return "tvOS";
                    case RuntimePlatform.Switch:
                        return "Switch";
                    case RuntimePlatform.GameCoreXboxSeries:
                        return "XboxSeries";
                    case RuntimePlatform.XboxOne:
                    case RuntimePlatform.GameCoreXboxOne:
                        return "XboxOne";
                    case RuntimePlatform.PS5:
                        return "PS5";
                    default:
                        return "AssetBundles";
                }
            }
        }

        public delegate void BundleLoadEvent(string bundleName, AssetBundle bundle);

        public event BundleLoadEvent onBundleLoaded;

        // 缓存已加载的Bundle，key为bundleName，value为Bundle和引用计数
        private LoadedCache<AssetBundle> _loadedBundles;
        // 正在加载的Bundle，key为bundleName，value为加载协程的YieldInstruction
        private BundleLoadingHolder _loadingBundles;
        private List<PrepareHandler> _prepareHandlers = new List<PrepareHandler>();
        // 移除保护，key为bundleName，value为Bundle和移除时间
        private RemovedAssetHolder _removedAssetHolder;
        
        #region BundleDependence
        // Bundle依赖关系管理
        private BundleDependenceMap _bundleDependenceMap;

        private string _bundleFoldName;
        private string GetBundlePath(string bundleName)
        {
            if (_remoteBundleIndexer.IsBundleRemote(bundleName))
                return Path.Combine(Application.persistentDataPath, _bundleFoldName, bundleName);
            return Path.Combine(Application.streamingAssetsPath, _bundleFoldName, bundleName);
        }

        private IEnumerator GetBundleManifest()
        {
            var mainBundleName = MainBundleName;
            var path = GetBundlePath(mainBundleName);
            _loadingBundles.AddLoadingHandle(mainBundleName, null);
            AssetBundle bundle = null;
            if (Application.platform == RuntimePlatform.Android)
            {
                var webRequest = UnityWebRequestAssetBundle.GetAssetBundle(path);
                yield return webRequest.SendWebRequest();
                bundle = DownloadHandlerAssetBundle.GetContent(webRequest);
                webRequest.Dispose();
            }
            else
            {
                var loadedBundleRequest = AssetBundle.LoadFromFileAsync(path);
                yield return loadedBundleRequest;
                bundle = loadedBundleRequest.assetBundle;
            }
            _loadingBundles.SetLoaded(mainBundleName, bundle);
            if (!bundle)
            {
                AssetLog.LogError($"MainBundle Name Error: {mainBundleName}");
                yield break;
            }
            var manifest = bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            _bundleDependenceMap = new BundleDependenceMap(manifest);
            bundle.UnloadAsync(false);
        }

        #endregion

        public void AddBundleRef(string bundleName, int count = 1)
        {
            if (!_loadedBundles.IsLoaded(bundleName)) return;
            _loadedBundles.AddRef(bundleName, count);
            var dependencies = _bundleDependenceMap.GetBundleDependencies(bundleName);
            foreach (var dependencyBundle in dependencies)
            {
                _loadedBundles.AddRef(dependencyBundle, count);
            }
        }
        
        public void DelBundleRef(string bundleName, int count = 1)
        {
            if (_loadedBundles.TryDelRef(bundleName, count, out var bundle))
            {
                _loadedBundles.RemoveCache(bundleName);
                _removedAssetHolder.Push(bundleName, bundle, delayUnloadDuration);
            }
            else if (_bundleRefCountBuffer.TryGetValue(bundleName, out var refCount))
            {
                var newValue = refCount - count;
                if (newValue < 1)
                    _bundleRefCountBuffer.Remove(bundleName);
                else
                    _bundleRefCountBuffer[bundleName] = newValue;
            }
            var dependencies = _bundleDependenceMap.GetBundleDependencies(bundleName);
            foreach (var dependencyBundle in dependencies)
            {
                if (_loadedBundles.TryDelRef(dependencyBundle, count, out var ab))
                {
                    _loadedBundles.RemoveCache(dependencyBundle);
                    _removedAssetHolder.Push(dependencyBundle, ab, delayUnloadDuration);
                }
                else if (_bundleRefCountBuffer.TryGetValue(dependencyBundle, out var depRefCount))
                {
                    var newValue = depRefCount - count;
                    if (newValue < 1)
                        _bundleRefCountBuffer.Remove(dependencyBundle);
                    else
                        _bundleRefCountBuffer[dependencyBundle] = newValue;
                }
            }
        }

        private void OnBundleLoaded(string bundleName, AssetBundle loadedBundle)
        {
            if (!loadedBundle)
            {
                _loadingBundles.SetLoaded(bundleName, null);
                var path = GetBundlePath(bundleName);
                AssetLog.LogError($"Bundle: {bundleName} Load Fail, path: {path}");
                return;
            }
            _loadedBundles.AddCache(bundleName, loadedBundle);
            _loadingBundles.SetLoaded(bundleName, loadedBundle);
            if (_bundleRefCountBuffer.TryGetValue(bundleName, out var refCount))
            {
                _loadedBundles.AddRef(bundleName, refCount);
                _bundleRefCountBuffer.Remove(bundleName);
            }
            onBundleLoaded?.Invoke(bundleName, loadedBundle);
            TriggerLoadPlan(bundleName, loadedBundle);
        }

        private void TriggerLoadPlan(string bundleName, AssetBundle loadedBundle)
        {
            if (!_loadPlan.TryGetPlan(bundleName, out var planList)) return;
            if (planList == null || planList.Count == 0) return;
            AddBundleRef(bundleName, planList.Count);
            foreach (var (assetPath, assetType) in planList)
            {
                GetAssetFromBundleAsync(loadedBundle, bundleName, assetPath, assetType);
            }
            _loadPlan.ClearPlan(bundleName);
        }

        public void Unprepare(PrepareHandler handler)
        {
            if (handler == null || handler.successLable == null) return;
            handler.cancled = true;
            if (!handler.isDone)
            {
                _coroutineRunner.StartCoroutine(WaitForPrepareDone(handler));
                return;
            }
            foreach(var bundleName in handler.successLable)
            {
                DelBundleRef((string)bundleName);
            }
            _prepareHandlers.Remove(handler);
            handler.Dispose();
        }

        private IEnumerator WaitForPrepareDone(PrepareHandler handler)
        {
            yield return handler;
            Unprepare(handler);
        }

        public PrepareHandler Prepare(string[] labels, Action onComplete, bool isConcurrent = false)
        {
            if (labels == null || labels.Length == 0)
            {
                onComplete?.Invoke();
                return null;
            }
            var handler = new PrepareHandler();
            handler.OnComplete(onComplete);
            _coroutineRunner.StartCoroutine(DownLoadPrepareBundle(labels, isConcurrent, handler));
            _prepareHandlers.Add(handler);
            return handler;
        }

        private IEnumerator DownLoadPrepareBundle(string[] labels, bool isConcurrent, PrepareHandler handler)
        {
            var bundlesName = ListPool<string>.Get();
            for (var i = 0; i < labels.Length; i++)
            {
                if (handler.cancled) break;
                var bundleName = labels[i];
                if (isConcurrent)
                {
                    GetAssetsBundleAsync(bundleName);
                    if(_loadingBundles.IsLoading(bundleName))
                    {
                        bundlesName.Add(bundleName);
                    }
                }
                else
                {
                    handler.SetProcessValue(i * 1f / labels.Length);
                    GetAssetsBundleAsync(bundleName);
                    while (_loadingBundles.IsLoading(bundleName))
                    {
                        yield return null;
                    }
                    bundlesName.Add(bundleName);
                }
            }
            if (isConcurrent)
            {
                var doneCount = 0;
                while (doneCount < bundlesName.Count)
                {
                    doneCount = bundlesName.Count(o=>!_loadingBundles.IsLoading(o));
                    handler.SetProcessValue(doneCount * 1f / bundlesName.Count);
                    yield return null;
                }
            }
            for (var i = 0; i < bundlesName.Count; i++)
            {
                var bundleName = bundlesName[i];
                if (_loadedBundles.IsLoaded(bundleName))
                {
                    handler.Append(bundleName);
                    AddBundleRef(bundleName);
                }
            }
            ListPool<string>.Release(bundlesName);
            handler.SetProcessValue(1f);
            if (handler.cancled) yield break;
            handler.SetComplete();
        }

        // 获取已加载的Bundle，优先从已加载的Bundle中获取，如果已卸载但在移除保护中，则重新加入已加载的Bundle中并返回
        // 会检查依赖，如果依赖的Bundle未加载，则返回null
        private AssetBundle GetUseableBundle(string bundleName)
        {
            AssetBundle bundle = GetAssetBundleCache(bundleName);
            // 依赖检查
            if (bundle)
            {
                var dependencies = _bundleDependenceMap.GetBundleDependencies(bundleName);
                var result = true;
                foreach (var dependencyBundle in dependencies)
                {
                    if (!GetAssetBundleCache(dependencyBundle))
                    {
                        result = false;
                    }
                }
                if (!result)
                {
                    return null;
                }
            }
            return bundle;
        }

        private AssetBundle GetAssetBundleCache(string bundleName)
        {
            if (_loadedBundles.TryGetCache(bundleName, out var bundle))
            {
                return bundle;
            }
            else if (_removedAssetHolder.TryGetBundle(bundleName, out bundle))
            {
                _loadedBundles.AddCache(bundleName, bundle);
                return bundle;
            }
            return null;
        }

        #region Async

        // 异步加载方案
        private void GetAssetsBundleAsync(string bundleName)
        {
            var loadedBundle = GetUseableBundle(bundleName);
            if (loadedBundle)
            {
                TriggerLoadPlan(bundleName, loadedBundle);
                // onGetBundle?.Invoke(loadedBundle, bundleName);
                return;
            }
            _coroutineRunner.StartCoroutine(AsyncLoadAssetsBundleHandler(bundleName));
        }

        private IEnumerator AsyncLoadAssetsBundleHandler(string bundleName)
        {
            var dependencies = _bundleDependenceMap.GetBundleDependencies(bundleName);
            yield return AsyncLoadMultipleAssetsBundle(dependencies);
            foreach (var dependency in dependencies)
            {
                if (!_loadedBundles.IsLoaded(dependency))
                {
                    AssetLog.LogError($"Load Bundle [{bundleName}] Failed, because dependency bundle [{dependency}] load failed");
                }
            }
            if (_loadedBundles.IsLoaded(bundleName))
            {
                var bundle = GetAssetBundleCache(bundleName);
                TriggerLoadPlan(bundleName, bundle);
                // onGetBundle?.Invoke(bundle, bundleName);
                yield break;
            }
            if (_loadingBundles.IsLoading(bundleName))
            {
                // _loadingBundles.AddLoadingHandle(bundleName, onGetBundle);
                yield break;
            }
            _loadingBundles.AddLoadingHandle(bundleName, null);
            AsyncLoadSingleAssetsBundle(bundleName);
        }

        private IEnumerator AsyncLoadMultipleAssetsBundle(IEnumerable<string> bundleNames)
        {
            var waitList = ListPool<string>.Get();
            foreach (var bundleName in bundleNames)
            {
                if (_loadedBundles.IsLoaded(bundleName)) continue;
                if (_loadingBundles.IsLoading(bundleName))
                {
                    waitList.Add(bundleName);
                }
                else
                {
                    var newRequest = _loadingBundles.AddLoadingHandle(bundleName, null);
                    waitList.Add(bundleName);
                    AsyncLoadSingleAssetsBundle(bundleName);
                } 
            }
            var wait = waitList.Count > 0 && waitList.Any(o=>_loadingBundles.IsLoading(o));
            while (wait)
            {
                yield return null;
                wait = waitList.Any(o=>_loadingBundles.IsLoading(o));
            }
            ListPool<string>.Release(waitList);
        }

        private void AsyncLoadSingleAssetsBundle(string bundleName)
        {
            var path = GetBundlePath(bundleName);
            if (Application.platform == RuntimePlatform.Android)
            {
                if (!_remoteBundleIndexer.IsBundleNeedLoadFromRemote(bundleName))
                {
                    var webRequest = UnityWebRequestAssetBundle.GetAssetBundle(path);
                    var operation = webRequest.SendWebRequest();
                    operation.completed += result =>
                    {
                        if (webRequest.result == UnityWebRequest.Result.Success)
                        {
                            var bundle = DownloadHandlerAssetBundle.GetContent(webRequest);
                            OnBundleLoaded(bundleName, bundle);
                        }
                        else
                        {
                            OnBundleLoaded(bundleName, null);
                        }
                        webRequest.Dispose();
                    };
                    return;
                }
            }
            else if (File.Exists(path))
            {
                var abcr = AssetBundle.LoadFromFileAsync(path);
                abcr.completed += result =>
                {
                    var bundle = abcr.assetBundle;
                    OnBundleLoaded(bundleName, bundle);
                };
                return;
            }
            _remoteBundleIndexer.LoadRemoteBundle(bundleName, result =>
            {
                if (!result) 
                {
                    OnBundleLoaded(bundleName, null);
                    return;
                }
                _remoteBundleIndexer.SaveRemoteManifest();
                AsyncLoadSingleAssetsBundle(bundleName);
            });
        }

        #endregion

        #region Sync

        private bool LoadBundleDependence(string bundleName)
        {
            var dependencies = _bundleDependenceMap.GetBundleDependencies(bundleName);
            var result = true;
            for (var i = 0; i < dependencies.Length; i++)
            {
                var dependencyBundle = dependencies[i];
                if (_loadedBundles.IsLoaded(dependencyBundle)) continue;
                var bundle = LoadAssetBundle(dependencyBundle);
                if (!bundle) result = false;
            }
            return result;
        }

        private AssetBundle LoadAssetBundle(string bundleName)
        {
            if (_loadingBundles.IsLoading(bundleName))
            {
                AssetLog.LogWarning($"Bundle [{bundleName}] is loading, please wait");
                return null;
            }
            _loadingBundles.AddLoadingHandle(bundleName, null);
            var path = GetBundlePath(bundleName);
            AssetBundle loadedBundle = null;
            try
            {
                if (Application.platform == RuntimePlatform.Android)
                {
                    if (_remoteBundleIndexer.IsBundleRemote(bundleName))
                    {
                        loadedBundle = AssetBundle.LoadFromFile(path);
                    }
                    else
                    {
                        AssetLog.LogError($"Bundle [{bundleName}] does not exit in Application.persistentDataPath!\nUse GetAssetsBundleAsync() to load it.");
                    }
                }
                else
                {
                    loadedBundle = AssetBundle.LoadFromFile(path);
                }
            }
            catch (Exception e)
            {
                loadedBundle = null;
                AssetLog.LogError($"Bundle [{bundleName}] do not exist on local");
                Debug.LogError(e);
            }
            finally
            {
                OnBundleLoaded(bundleName, loadedBundle);
            }
            return loadedBundle;
        }
        
        // 同步加载方案
        private bool GetAssetBundle(string bundleName, out AssetBundle loadedBundle)
        {
            var ready = LoadBundleDependence(bundleName);
            if (!ready)
            {
                loadedBundle = null;
                return false;
            }
            var loaded = GetAssetBundleCache(bundleName);
            loadedBundle = (bool)loaded
                ? loaded
                : LoadAssetBundle(bundleName);
            bool result = loadedBundle;
            if (result) AddBundleRef(bundleName);
            return result;
        }

        #endregion


        #region Unload

        private Dictionary<string, int> _bundleRefCountBuffer = new Dictionary<string, int>();

        /// <summary>
        /// will unload bundle immediately.
        /// Unless you know what you're doing, using `AssetBundleManager.instance.DelAssetRef(string assetPath)` instead if you want to release a bundle
        /// </summary>
        /// <param name="bundleRef"></param>
        /// <returns></returns>
        public void UnloadAssetsBundle(string bundleName)
        {
            var refCount = _loadedBundles.GetRefCount(bundleName);
            _bundleRefCountBuffer[bundleName] = refCount;
            _loadedBundles.TryGetCache(bundleName, out var bundle);
            bundle?.Unload(false);
            var dependencies = _bundleDependenceMap.GetBundleDependencies(bundleName);
            foreach (var dependencyBundle in dependencies)
            {
                DelBundleRef(dependencyBundle, refCount);
            }
            _loadedBundles.RemoveCache(bundleName);
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        public void UnloadAllAssetsBundle()
        {
            // var allBundle = _loadedBundles.GetAll();
            // foreach (var bundle in allBundle)
            // {
            //     bundle.Unload(false);
            // }

            _removedAssetHolder.Clear();
            AssetBundle.UnloadAllAssetBundles(false);
            var cached = _loadedBundles.GetAll();
            foreach (var (bundleName, bundleRef) in cached)
            {
                _bundleRefCountBuffer[bundleName] = bundleRef.refCount;
            }
            _loadedBundles.Clear();
            _bundleIndex.ClearUnused();
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        public void ClearUnusedAsset()
        {
            var preload = _preloadHandles.Values.ToList();
            foreach (var handler in preload)
            {
                // handler.Dispose();
                AssetUtils.ReleaseLoadHandler<UnityEngine.Object>(handler);
            }
            _preloadHandles.Clear();

            var prepareHandles = new List<PrepareHandler>(_prepareHandlers);
            for (var i=0; i < prepareHandles.Count; i++)
            {
                Unprepare(prepareHandles[i]);
            }

            _bundleIndex.ClearUnused();
            var cached = _loadedBundles.GetAll();
            var removeBundle = ListPool<string>.Get();
            foreach (var cacheRef in cached)
            {
                if (cacheRef.Value.refCount <= AssetsBundleManager.disposeRefLine)
                {
                    removeBundle.Add(cacheRef.Key);
                }
            }
            for (var i = 0; i < removeBundle.Count; i++)
            {
                var bundleRef = removeBundle[i];
                _bundleRefCountBuffer[bundleRef] = _loadedBundles.GetRefCount(bundleRef);
                _loadedBundles.TryGetCache(bundleRef, out var ab);
                ab.Unload(false);
                _loadedBundles.RemoveCache(bundleRef);
            }
            _removedAssetHolder.Clear();
            ListPool<string>.Release(removeBundle);
        }

        #endregion
    }
}