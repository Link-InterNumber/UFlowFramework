using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Microsoft.Xbox.Services.Client;
using Newtonsoft.Json;
using UnityEngine.Networking;
using UnityEngine.Pool;

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

        private class BundleDependenceStack : IDisposable
        {
            private HashSet<string> _bundleSet;

            private List<List<string>> _stack;

            public int layerCount => _stack.Count;

            public BundleDependenceStack()
            {
                _bundleSet = HashSetPool<string>.Get();
                _stack = new List<List<string>>();
            }

            public bool Contains(string bundleName)
            {
                return _bundleSet.Contains(bundleName);
            }

            public void Push(int layerIndex, string bundleName)
            {
                // if (_bundleSet.Contains(bundleName)) return;
                while (_stack.Count < layerIndex + 1)
                {
                    _stack.Add(ListPool<string>.Get());
                }
                _stack[layerIndex].Add(bundleName);
                _bundleSet.Add(bundleName);
            }

            // public void Pop()
            // {
            //     var list = _stack[layerCount -  1];
            //     _stack.RemoveAt(layerCount -  1);
            //     return list;
            // }

            public List<string> GetBundleNamesByLayer(int layerIndex)
            {
                if (layerIndex< 0 || layerIndex >= _stack.Count) return new List<string>();
                return _stack[layerIndex];
            }

            public void Dispose()
            {
                HashSetPool<string>.Release(_bundleSet);
                _bundleSet = null;
                foreach (var list in _stack)
                {
                    ListPool<string>.Release(list);
                }
                _stack.Clear();
                _stack = null;
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

        private void GetBundleDependencies(string bundleName, ref HashSet<string> dependencies)
        {
            var bundles = _bundleManifest.GetAllDependencies(bundleName);
            if (bundles == null || bundles.Length == 0)
            {
                return;
            }
            for (var i = 0; i < bundles.Length; i++)
            {
                var dependencyName = bundles[i];
                if (dependencies.Contains(dependencyName)) continue;
                if (dependencyName == bundleName) continue;
                dependencies.Add(dependencyName);
                GetBundleDependencies(dependencyName, ref dependencies);
            }
        }

        private void GetBundleDependencies(string bundleName, int layerIndex, ref BundleDependenceStack dependencies)
        {
            var bundles = _bundleManifest.GetAllDependencies(bundleName);
            if (bundles == null || bundles.Length == 0)
            {
                return;
            }
            for (var i = 0; i < bundles.Length; i++)
            {
                var dependencyName = bundles[i];
                if (dependencies.Contains(dependencyName)) continue;
                if (dependencyName == bundleName) continue;
                dependencies.Push(layerIndex, dependencyName);
                GetBundleDependencies(dependencyName, layerIndex + 1, ref dependencies);
            }
        }
        
        #endregion

        public void AddRef(string bundleName)
        {
            if (!_loadedBundleDic.TryGetValue(bundleName, out var loaded)) return;
            loaded.AddRef();
            var dependencies = HashSetPool<string>.Get();
            GetBundleDependencies(bundleName, ref dependencies);

            foreach (var dependencyBundle in dependencies)
            {
                if (!_loadedBundleDic.TryGetValue(dependencyBundle, out var bundleRef)) continue;
                bundleRef.AddRef();
            }
            HashSetPool<string>.Release(dependencies);
        }

        public bool IsAssetsBundleLoaded(string bundleName)
        {
            return _loadedBundleDic.ContainsKey(bundleName);
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
            var abf = new AssetsBundleRef(loadedBundle, this);
            _loadedBundleDic.Add(bundleName, abf);
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
                if (IsAssetsBundleLoaded(bundleName))
                {
                    handler.Append(bundleName);
                    AddRef(bundleName);
                }
            }
            handler.SetProcessValue(1f);
            if (handler.cancled) yield break;
            handler.SetComplete();
        }

        private IEnumerator SaveBundleOnLocal(string bundleName, byte[] data)
        {
            var path = Path.Combine(Application.persistentDataPath, _bundleFoldName, bundleName);
            yield return File.WriteAllBytesAsync(path, data).AsCoroutine();
        }

        #region Async

        // 异步加载方案
        private void GetAssetsBundleAsync(string bundleName, OnLoadCompleted<AssetBundle> onGetBundle)
        {
            if (_loadedBundleDic.TryGetValue(bundleName, out var loaded) && loaded.Alive)
            {
                loaded.Restore();
                onGetBundle?.Invoke(loaded.Bundle, bundleName);
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
            _loadedBundleDic.Remove(bundleName);
            _coroutineRunner.StartCoroutine(AsyncLoadAssetsBundleHandler(bundleName, newRequest));
        }

        private IEnumerator AsyncLoadAssetsBundleHandler(string bundleName, LoaderYieldInstruction<AssetBundle> loaderYieldInstruction)
        {
            var dependencies = new BundleDependenceStack();
            GetBundleDependencies(bundleName, 0, ref dependencies);
            for (var i = dependencies.layerCount - 1; i > -1; i--)
            {
                var bundleNames = dependencies.GetBundleNamesByLayer(i);
                yield return AsyncLoadMultipleAssetsBundle(bundleNames);
            }
            dependencies.Dispose();
            yield return AsyncLoadSingleAssetsBundle(bundleName, loaderYieldInstruction);
        }

        private IEnumerator AsyncLoadMultipleAssetsBundle(IEnumerable<string> bundleNames)
        {
            var waitList = new List<LoaderYieldInstruction<AssetBundle>>();
            foreach (var bundleName in bundleNames)
            {
                if (IsAssetsBundleLoaded(bundleName)) continue;
                if (_waitForLoadList.TryGetValue(bundleName, out var current))
                {
                    waitList.Add(current);
                }
                else
                {
                    var newRequest = AssetUtils.GetLoadHandler<AssetBundle>(bundleName);
                    _waitForLoadList.Add(bundleName, newRequest);
                    _loadedBundleDic.Remove(bundleName);
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
            yield return LoadRemoteBundle(bundleName, loaderYieldInstruction);
            bundle = loaderYieldInstruction.asset;
            OnBundleLoaded(bundleName, bundle);
            if (bundle) SaveRemoteManifest(_clientManifest);
        }

        #endregion

        #region Sync

        private void LoadBundleDependence(string bundleName)
        {
            var dependencies = new BundleDependenceStack();
            GetBundleDependencies(bundleName, 0, ref dependencies);
            for (var i = dependencies.layerCount - 1; i > -1; i--)
            {
                var bundleNames = dependencies.GetBundleNamesByLayer(i);
                foreach (var dependencyBundle in bundleNames)
                {
                    if (IsAssetsBundleLoaded(dependencyBundle)) continue;
                    LoadAssetBundle(dependencyBundle);
                }
            }
            dependencies.Dispose();
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
                LoadBundleDependence(bundleName);
                loadedBundle = LoadAssetBundle(bundleName);
            }
            return loadedBundle;
        }

        private AssetBundle LoadAssetBundle(string bundleName)
        {
            if (_waitForLoadList.ContainsKey(bundleName))
            {
                AssetLog.LogWarning($"Bundle [{bundleName}] is loading, please wait");
                return null;
            }
            _waitForLoadList.Add(bundleName, null);
            _loadedBundleDic.Remove(bundleName);
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
            var dependencies = HashSetPool<string>.Get();
            GetBundleDependencies(bundleName, ref dependencies);
            foreach (var dependencyBundle in dependencies)
            {
                if (!_loadedBundleDic.TryGetValue(dependencyBundle, out var bundleRef)) continue;
                bundleRef.DeRef();
            }
            HashSetPool<string>.Release(dependencies);
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
                    // _preloadHandles[path].Dispose();
                    AssetUtils.ReleaseLoadHandler<UnityEngine.Object>(_preloadHandles[path]);
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
                // handler.Dispose();
                AssetUtils.ReleaseLoadHandler<UnityEngine.Object>(handler);
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
        }

        #endregion
    }
}