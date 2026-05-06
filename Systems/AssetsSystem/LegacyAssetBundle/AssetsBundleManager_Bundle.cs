using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Microsoft.Xbox.Services.Client;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
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

        private LoadedCache<AssetBundle> _loadedBundles;
        private Dictionary<string, LoaderYieldInstruction<AssetBundle>> _waitForLoadList;
        private List<PrepareHandler> _prepareHandlers = new List<PrepareHandler>();
        
        #region BundleDependence
        
        private BundleDependenceMap _bundleDependenceMap;

        private string _bundleFoldName;
        private string GetBundlePath(string bundleName)
        {
            if (_clientManifest.ContainsKey(bundleName))
                return Path.Combine(Application.persistentDataPath, _bundleFoldName, bundleName);
            return Path.Combine(Application.streamingAssetsPath, _bundleFoldName, bundleName);
        }

        private IEnumerator GetBundleManifest()
        {
            var mainBundleName = MainBundleName;
            var path = GetBundlePath(mainBundleName);
            _waitForLoadList.Add(mainBundleName, null);
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
            _waitForLoadList.Remove(mainBundleName);
            if (!bundle)
            {
                AssetLog.LogError($"MainBundle Name Error: {mainBundleName}");
                yield break;
            }
            _loadedBundles.AddCache(mainBundleName, bundle);
            _loadedBundles.AddRef(mainBundleName, 1);
            var manifest = bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            _bundleDependenceMap = new BundleDependenceMap(manifest);
        }

        #endregion

        public void AddRef(string bundleName)
        {
            if (!_loadedBundles.IsLoaded(bundleName)) return;
            _loadedBundles.AddRef(bundleName, 1);
            var dependencies = _bundleDependenceMap.GetBundleDependencies(bundleName);
            foreach (var dependencyBundle in dependencies)
            {
                _loadedBundles.AddRef(dependencyBundle, 1);
            }
        }
        
        public void DelRef(string bundleName)
        {
            if (!_loadedBundles.IsLoaded(bundleName)) return;
            if (_loadedBundles.TryDelRef(bundleName, 1, out var bundle))
            {
                _loadedBundles.RemoveCache(bundleName);
                bundle.UnloadAsync(false);
            }
            var dependencies = _bundleDependenceMap.GetBundleDependencies(bundleName);
            foreach (var dependencyBundle in dependencies)
            {
                if (_loadedBundles.TryDelRef(dependencyBundle, 1, out var ab))
                {
                    _loadedBundles.RemoveCache(dependencyBundle);
                    ab.UnloadAsync(false);
                }
            }
        }

        private void OnBundleLoaded(string bundleName, AssetBundle loadedBundle)
        {
            if(_waitForLoadList.TryGetValue(bundleName, out var request))
            {
                _waitForLoadList.Remove(bundleName);
                // request?.Dispose();
                AssetUtils.ReleaseLoadHandler<AssetBundle>(request);
            }
            if (!loadedBundle)
            {
                var path = GetBundlePath(bundleName);
                AssetLog.LogError($"Bundle: {bundleName} Load Fail, path: {path}");
                return;
            }
            _loadedBundles.AddCache(bundleName, loadedBundle);
            onBundleLoaded?.Invoke(bundleName, loadedBundle);
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
                ReleaseBundle((string)bundleName);
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
            var waitList = new List<LoaderYieldInstruction<AssetBundle>>();
            var bundlesName = new List<string>();
            for (var i = 0; i < labels.Length; i++)
            {
                if (handler.cancled) break;
                var bundleName = labels[i];
                if (isConcurrent)
                {
                    GetAssetsBundleAsync(bundleName, null);
                    if(_waitForLoadList.TryGetValue(bundleName, out var bundleLoadHandler))
                    {
                        waitList.Add(bundleLoadHandler);
                        bundlesName.Add(bundleName);
                    }
                }
                else
                {
                    handler.SetProcessValue(i * 1f / labels.Length);
                    GetAssetsBundleAsync(bundleName, null);
                    if(_waitForLoadList.TryGetValue(bundleName, out var bundleLoadHandler))
                    {
                        yield return bundleLoadHandler;
                        bundlesName.Add(bundleName);
                    }
                }
            }
            if (isConcurrent)
            {
                var doneCount = 0;
                while (doneCount < waitList.Count)
                {
                    doneCount = waitList.Count(o=>o.isDone);
                    handler.SetProcessValue(doneCount * 1f / waitList.Count);
                    yield return null;
                }
            }
            for (var i = 0; i < bundlesName.Count; i++)
            {
                var bundleName = bundlesName[i];
                if (_loadedBundles.IsLoaded(bundleName))
                {
                    handler.Append(bundleName);
                    AddRef(bundleName);
                }
            }
            handler.SetProcessValue(1f);
            if (handler.cancled) yield break;
            handler.SetComplete();
        }

        #region Async

        // 异步加载方案
        private void GetAssetsBundleAsync(string bundleName, OnLoadCompleted<AssetBundle> onGetBundle)
        {
            if (_loadedBundles.TryGetCache(bundleName, out var loaded))
            {
                AddRef(bundleName);
                onGetBundle?.Invoke(loaded, bundleName);
                return;
            }
            if (_waitForLoadList.TryGetValue(bundleName, out var current))
            {
                if(onGetBundle != null) current.OnLoadCompleted(onGetBundle);
                return;
            }
            var newRequest = AssetUtils.GetLoadHandler<AssetBundle>(bundleName);
            if(onGetBundle != null) newRequest.OnLoadCompleted(onGetBundle);
            _waitForLoadList.Add(bundleName, newRequest);
            _coroutineRunner.StartCoroutine(AsyncLoadAssetsBundleHandler(bundleName, newRequest));
        }

        private IEnumerator AsyncLoadAssetsBundleHandler(string bundleName, LoaderYieldInstruction<AssetBundle> loaderYieldInstruction)
        {
            var dependencies = _bundleDependenceMap.GetBundleDependencies(bundleName);
            yield return AsyncLoadMultipleAssetsBundle(dependencies);
            yield return AsyncLoadSingleAssetsBundle(bundleName, loaderYieldInstruction);
        }

        private IEnumerator AsyncLoadMultipleAssetsBundle(IEnumerable<string> bundleNames)
        {
            var waitList = new List<LoaderYieldInstruction<AssetBundle>>();
            foreach (var bundleName in bundleNames)
            {
                if (_loadedBundles.IsLoaded(bundleName)) continue;
                if (_waitForLoadList.TryGetValue(bundleName, out var current))
                {
                    waitList.Add(current);
                }
                else
                {
                    var newRequest = AssetUtils.GetLoadHandler<AssetBundle>(bundleName);
                    _waitForLoadList.Add(bundleName, newRequest);
                    _coroutineRunner.StartCoroutine(AsyncLoadSingleAssetsBundle(bundleName, newRequest));
                    waitList.Add(newRequest);
                } 
            }
            var wait = waitList.Count > 0 && waitList.Any(o=>!o.isDone);
            while (wait)
            {
                yield return null;
                wait = waitList.Any(o=>!o.isDone);
            }
            waitList.Clear();
            waitList = null;
        }

        private IEnumerator AsyncLoadSingleAssetsBundle(string bundleName, LoaderYieldInstruction<AssetBundle> loaderYieldInstruction)
        {
            AssetBundle bundle = null;
            var path = GetBundlePath(bundleName);
            if (Application.platform == RuntimePlatform.Android)
            {
                if (!IsBundleNeedLoadFromRemote(bundleName))
                {
                    var webRequest = UnityWebRequestAssetBundle.GetAssetBundle(path);
                    yield return webRequest.SendWebRequest();
                    bundle = DownloadHandlerAssetBundle.GetContent(webRequest);
                    webRequest.Dispose();
                    loaderYieldInstruction.SetAsset(bundle);
                    OnBundleLoaded(bundleName, bundle);
                    yield break;
                }
            }
            else if (File.Exists(path))
            {
                var abcr = AssetBundle.LoadFromFileAsync(path);
                yield return abcr;
                bundle = abcr.assetBundle;
                loaderYieldInstruction.SetAsset(bundle);
                OnBundleLoaded(bundleName, bundle);
                yield break;
            }
            var waitForLoadFromRemote = new  YieldInstructionCompletionSource<bool>();
            yield return LoadRemoteBundle(bundleName, waitForLoadFromRemote);
            if (waitForLoadFromRemote.Result)
            {
                SaveRemoteManifest(_clientManifest);
                yield return AsyncLoadSingleAssetsBundle(bundleName, loaderYieldInstruction);
            }
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
            if (_waitForLoadList.ContainsKey(bundleName))
            {
                AssetLog.LogWarning($"Bundle [{bundleName}] is loading, please wait");
                return null;
            }
            _waitForLoadList.Add(bundleName, null);
            var path = GetBundlePath(bundleName);
            AssetBundle loadedBundle = null;
            try
            {
                if (Application.platform == RuntimePlatform.Android)
                {
                    if (_clientManifest.ContainsKey(bundleName))
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
            loadedBundle = _loadedBundles.TryGetCache(bundleName, out var loaded)
                ? loaded
                : LoadAssetBundle(bundleName);
            bool result = loadedBundle;
            if (result) AddRef(bundleName);
            return result;
        }

        #endregion


        #region Unload

        /// <summary>
        /// deRef a bundle
        /// </summary>
        public void ReleaseBundle(string bundleName)
        {
#if UNITY_EDITOR
            if(!simulateAssetBundleInEditor) return;
#endif
            DelRef(bundleName);
        }

        /// <summary>
        /// will unload bundle immediately.
        /// Unless you know what you're doing, using `AssetBundleManager.instance.ReleaseBundle(string bundleName)` instead if you want to release a bundle
        /// </summary>
        /// <param name="bundleRef"></param>
        /// <returns></returns>
        private bool UnloadAssetsBundle(string bundleName)
        {
            _loadedBundles.TryGetCache(bundleName, out var bundle);
            bundle.Unload(false);
            _loadedBundles.RemoveCache(bundleName);
            Resources.UnloadUnusedAssets();
            GC.Collect();
            return true;
        }

        public void UnloadAllAssetsBundle()
        {
            // var allBundle = _loadedBundles.GetAll();
            // foreach (var bundle in allBundle)
            // {
            //     bundle.Unload(false);
            // }
            AssetBundle.UnloadAllAssetBundles(false);
            _loadedBundles.Clear();
            _waitForLoadList.Clear();
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
                _loadedBundles.TryGetCache(bundleRef, out var ab);
                ab.Unload(false);
                _loadedBundles.RemoveCache(bundleRef);
            }
            ListPool<string>.Release(removeBundle);
        }

        #endregion
    }
}