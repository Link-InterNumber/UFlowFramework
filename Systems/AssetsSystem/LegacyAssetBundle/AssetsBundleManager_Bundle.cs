using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Microsoft.Xbox.Services.Client;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace PowerCellStudio
{
    public partial class AssetsBundleManager
    {
        private class AssetsBundleRef
        {
            public AssetBundle Bundle => _bundle;
            public int RefCount => _refCount;
            
            private AssetBundle _bundle;
            private int _refCount = 0;
            // public bool AutoDispose = false;
            public bool Alive = true;
            private Coroutine _unloadCoroutine;
            private AssetsBundleManager _assetsBundleManager;
            
            public AssetsBundleRef(AssetBundle bundle, AssetsBundleManager assetsBundleManager)
            {
                _bundle = bundle;
                _refCount = 0;
                _assetsBundleManager = assetsBundleManager;
            }

            public void DeRef()
            {
                _refCount -= 1;
                if (RefCount <= AssetsBundleManager.disposeRefLine)
                {
                    WaitToUnloadBundle();
                }
            }

            public void Restore()
            {
                Alive = true;
                if (_unloadCoroutine != null)
                {
                    _assetsBundleManager._coroutineRunner.StopCoroutine(_unloadCoroutine);
                    _unloadCoroutine = null;
                }
                if (RefCount <= AssetsBundleManager.disposeRefLine)
                {
                    _refCount = 0;
                }
            }

            public void AddRef()
            {
                Alive = true;
                if (_unloadCoroutine != null)
                {
                    _assetsBundleManager._coroutineRunner.StopCoroutine(_unloadCoroutine);
                    _unloadCoroutine = null;
                }

                if (RefCount <= AssetsBundleManager.disposeRefLine)
                {
                    _refCount = 1;
                }
                else
                {
                    _refCount += 1;
                }
            }

            public void ForceUnload()
            {
                _refCount = AssetsBundleManager.disposeRefLine - 1;
                // AutoDispose = true;
                WaitToUnloadBundle();
            }

            public void WaitToUnloadBundle()
            {
                if (!Alive || _refCount > AssetsBundleManager.disposeRefLine || _unloadCoroutine != null)
                    return;
                //  启动计时器
                if (_assetsBundleManager._coroutineRunner && AssetsBundleManager.delayUnloadDuration > 0)
                    _unloadCoroutine = _assetsBundleManager._coroutineRunner.StartCoroutine(WaitToUnloadHandle());
                else
                {
                    Alive = false;
                    _assetsBundleManager?.UnloadAssetsBundle(this);
                }
            }

            private IEnumerator WaitToUnloadHandle()
            {
                yield return new WaitForSecondsRealtime(AssetsBundleManager.delayUnloadDuration);
                Alive = false;
                _assetsBundleManager?.UnloadAssetsBundle(this);
            }
        }
        
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

        private Dictionary<string, AssetsBundleRef> _loadedBundleDic;
        private Dictionary<string, LoaderYieldInstruction<AssetBundle>> _waitForLoadList;
        private List<PrepareHandler> _prepareHandlers = new List<PrepareHandler>();
        
        #region BundleDependence
        
        private AssetBundleManifest _bundleManifest;

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
            _loadedBundleDic.Remove(mainBundleName);
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
            var abf = new AssetsBundleRef(bundle, this);
            _loadedBundleDic.Add(mainBundleName, abf);
            abf.AddRef();
            _bundleManifest = bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        }

        private string[] GetBundleDependencies(string bundleName)
        {
            var dependencies = _bundleManifest.GetAllDependencies(bundleName);
            return dependencies ?? Array.Empty<string>();
        }
        
        #endregion

        public void AddRef(string bundleName)
        {
            if (!_loadedBundleDic.TryGetValue(bundleName, out var loaded)) return;
            loaded.AddRef();
            var dependencies = GetBundleDependencies(bundleName);
            foreach (var name in dependencies)
            {
                AddRef(name);
            }
        }

        public bool IsAssetsBundleLoaded(string bundleName)
        {
            return _loadedBundleDic.ContainsKey(bundleName);
        }

        private void OnBundleLoaded(string bundleName, AssetBundle loadedBundle)
        {
            if(_waitForLoadList.TryGetValue(bundleName, out var request))
            {
                request?.Dispose();
                _waitForLoadList.Remove(bundleName);
            }
            if (!loadedBundle)
            {
                var path = GetBundlePath(bundleName);
                AssetLog.LogError($"Bundle: {bundleName} Load Fail, path: {path}");
                return;
            }
            var abf = new AssetsBundleRef(loadedBundle, this);
            _loadedBundleDic.Add(bundleName, abf);
            onBundleLoaded?.Invoke(bundleName, loadedBundle);
        }

        public void Unprepare(PrepareHandler handler)
        {
            if (handler == null) return;
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
            var waitList = new LoaderYieldInstruction<AssetBundle>[labels.Length];
            for (var i = 0; i < labels.Length; i++)
            {
                var bundleName = labels[i];
                if (isConcurrent)
                {
                    waitList[i] = GetAssetsBundleAsync(bundleName);
                }
                else
                {
                    handler.SetProcessValue(i * 1f / labels.Length);
                    yield return GetAssetsBundleAsync(bundleName);
                }
            }
            if (isConcurrent)
            {
                var doneCount = 0;
                while (doneCount < labels.Length)
                {
                    doneCount = waitList.Count(o=>o.isDone);
                    handler.SetProcessValue(doneCount * 1f / labels.Length);
                    yield return null;
                }
            }
            for (var i = 0; i < labels.Length; i++)
            {
                var bundleName = labels[i];
                if (IsAssetsBundleLoaded(bundleName))
                {
                    handler.Append(bundleName);
                    AddRef(bundleName);
                }
            }
            handler.SetProcessValue(1f);
            handler.SetComplete();
        }

        private IEnumerator SaveBundleOnLocal(string bundleName, byte[] data)
        {
            var path = Path.Combine(Application.persistentDataPath, _bundleFoldName, bundleName);
            yield return File.WriteAllBytesAsync(path, data).AsCoroutine();
        }

        #region Async

        // 异步加载方案
        private LoaderYieldInstruction<AssetBundle> GetAssetsBundleAsync(string bundleName)
        {
            if (_loadedBundleDic.TryGetValue(bundleName, out var loaded) && loaded.Alive)
            {
                loaded.Restore();
                var yieldInstruction = new LoaderYieldInstruction<AssetBundle>(bundleName);
                yieldInstruction.SetAsset(loaded.Bundle);
                // onLoadCompleted?.Invoke(loaded.Bundle);
                return yieldInstruction;
            }
            if (_waitForLoadList.TryGetValue(bundleName, out var current))
            {
                // if(onLoadCompleted != null) current.onLoadCompleted += onLoadCompleted;
                return current;
            }
            var newRequest = new LoaderYieldInstruction<AssetBundle>(bundleName);
            // if(onLoadCompleted != null) newRequest.onLoadCompleted += onLoadCompleted;
            _waitForLoadList.Add(bundleName, newRequest);
            _loadedBundleDic.Remove(bundleName);
            _coroutineRunner.StartCoroutine(AsyncLoadAssetsBundleHandler(bundleName, newRequest));
            return newRequest;
        }

        private IEnumerator AsyncLoadAssetsBundleHandler(string bundleName, LoaderYieldInstruction<AssetBundle> loaderYieldInstruction)
        {
            yield return LoadBundleDependenceAsync(bundleName);
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
                    if (!bundle) 
                    {
                        loaderYieldInstruction.SetAsset(null);
                        OnBundleLoaded(bundleName, null);
                        yield break;
                    }
                    loaderYieldInstruction.SetAsset(bundle);
                    OnBundleLoaded(bundleName, bundle);
                    yield break;
                }
            }
            else
            {
                if (File.Exists(path))
                {
                    var abcr = AssetBundle.LoadFromFileAsync(path);
                    yield return abcr;
                    bundle = abcr.assetBundle;
                    if (!bundle)
                    {
                        loaderYieldInstruction.SetAsset(null);
                        OnBundleLoaded(bundleName, null);
                        yield break;
                    }
                    loaderYieldInstruction.SetAsset(bundle);
                    OnBundleLoaded(bundleName, bundle);
                    yield break;
                }
            }
            yield return LoadRemoteBundle(bundleName, loaderYieldInstruction);
            bundle = loaderYieldInstruction.asset;
            OnBundleLoaded(bundleName, bundle);
            if (bundle) SaveRemoteManifest(_clientManifest);
        }

        private IEnumerator LoadBundleDependenceAsync(string bundleName)
        {
            var dependencies = GetBundleDependencies(bundleName);
            var waitList = new List<LoaderYieldInstruction<AssetBundle>>();
            foreach (var name in dependencies)
            {
                if (name.Equals(bundleName)) continue;
                waitList.Add(GetAssetsBundleAsync(name));
            }
            var wait = waitList.Count > 0 && waitList.Any(o=>!o.isDone);
            while (wait)
            {
                yield return null;
                wait = waitList.Any(o=>!o.isDone);
            }
        }

        #endregion

        #region Sync

        private void LoadBundleDependence(string bundleName)
        {
            var dependencies = GetBundleDependencies(bundleName);
            foreach (var name in dependencies)
            {
                if (name.Equals(bundleName)) continue;
                GetAssetBundle(name, out _);
            }
        }

        // 同步加载方案
        private bool GetAssetBundle(string bundleName, out AssetBundle loadedBundle)
        {
            if (_loadedBundleDic.TryGetValue(bundleName, out var loaded) && loaded.Alive)
            {
                loadedBundle = loaded.Bundle;
                loaded.Restore();
            }
            else
            {
                loadedBundle = LoadAssetBundle(bundleName);
            }
            return loadedBundle;
        }

        private AssetBundle LoadAssetBundle(string bundleName)
        {
            if (_waitForLoadList.ContainsKey(bundleName))
            {
                AssetLog.LogWarning($"Bundle: {bundleName} is loading, please wait");
                return null;
            }
            _waitForLoadList.Add(bundleName, null);
            _loadedBundleDic.Remove(bundleName);
            LoadBundleDependence(bundleName);
            var path = GetBundlePath(bundleName);
            AssetBundle loadedBundle = null;
            try
            {
                loadedBundle = AssetBundle.LoadFromFile(path);
            }
            catch (Exception e)
            {
                loadedBundle = null;
                AssetLog.LogError($"bundleName={bundleName} do not exist on local");
                Debug.LogError(e);
            }
            finally
            {
                OnBundleLoaded(bundleName, loadedBundle);
            }
            return loadedBundle;
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
            if (!_loadedBundleDic.TryGetValue(bundleName, out var loaded)) return;
            loaded.DeRef();
            // 依赖bundle.DeRef()
            var dependencies = GetBundleDependencies(bundleName);
            foreach (var name in dependencies)
            {
                ReleaseBundle(name);
            }
        }

        /// <summary>
        /// will unload bundle immediately.
        /// Unless you know what you're doing, using `AssetBundleManager.instance.ReleaseBundle(string bundleName)` instead if you want to release a bundle
        /// </summary>
        /// <param name="bundleRef"></param>
        /// <returns></returns>
        private bool UnloadAssetsBundle(AssetsBundleRef bundleRef)
        {
            var preload = _preloadHandles.Keys.ToList();
            foreach (var path in preload)
            {
                var bundleName = GetBundleNameByAsset(path);
                if (Path.GetFileNameWithoutExtension(bundleName) == bundleRef.Bundle.name)
                {
                    _preloadHandles[path].Dispose();
                    _preloadHandles.Remove(path);
                }
            }
            _loadedBundleDic.Remove(bundleRef.Bundle.name);
            bundleRef.Bundle.Unload(false);
            Resources.UnloadUnusedAssets();
            GC.Collect();
            return true;
        }

        public void UnloadAllAssetsBundle()
        {
            AssetBundle.UnloadAllAssetBundles(false);
            _loadedBundleDic.Clear();
            _waitForLoadList.Clear();
            _bundleManifest = null;
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        public void ClearUnusedAsset()
        {
            var preload = _preloadHandles.Values.ToList();
            foreach (var handler in preload)
            {
                handler.Dispose();
            }
            _preloadHandles.Clear();

            var prepareHandles = new List<PrepareHandler>(_prepareHandlers);
            for (var i=0; i < prepareHandles.Count; i++)
            {
                Unprepare(prepareHandles[i]);
            }

            var removeBundle = new List<AssetsBundleRef>();
            foreach (var keyNValue in _loadedBundleDic)
            {
                var bundleRef = keyNValue.Value;
                if (bundleRef.RefCount <= AssetsBundleManager.disposeRefLine)
                {
                    bundleRef.Restore();
                    removeBundle.Add(bundleRef);
                }
            }
            for (var i=0;i < removeBundle.Count;i++)
            {
                var bundleRef = removeBundle[i];
                _loadedBundleDic.Remove(bundleRef.Bundle.name);
                bundleRef.Bundle.Unload(false);
            }
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        #endregion
    }
}