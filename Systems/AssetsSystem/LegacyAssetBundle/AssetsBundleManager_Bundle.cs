using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Microsoft.Xbox.Services.Client;

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
                    ApplicationManager.instance.StopCoroutine(_unloadCoroutine);
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
                    ApplicationManager.instance.StopCoroutine(_unloadCoroutine);
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
                if (ApplicationManager.isExist && AssetsBundleManager.delayUnloadDuration > 0)
                    _unloadCoroutine = ApplicationManager.instance.StartCoroutine(WaitToUnloadHandle());
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

        public delegate void BundleLoadEvent(string bundleName, AssetBundle bundle);

        public event BundleLoadEvent onBundleLoaded;

        private Dictionary<string, AssetsBundleRef> _loadedBundleDic;
        private Dictionary<string, LoaderYieldInstruction<AssetBundle>> _waitForLoadList;

        private string[] GetBundleDependencies(string bundleName)
        {
            var dependencies = _bundleManifest.GetAllDependencies(bundleName);
            return dependencies ?? Array.Empty<string>();
        }

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
                AssetLog.LogError($"Bundle: {bundleName} Load Fail");
                return;
            }
            var abf = new AssetsBundleRef(loadedBundle, this);
            _loadedBundleDic.Add(bundleName, abf);
            onBundleLoaded?.Invoke(bundleName, loadedBundle);
        }

        private void Unprepare(PrepareHandler handler)
        {
            if (handler == null) return;
            if (!handler.isDnoe)
            {
                ApplicationManager.instance.StartCoroutine(UnprepareHandler(handler));
                return;
            }
            foreach(var bundleName in handler.successLable)
            {
                ReleaseBundle((string)bundleName);
            }
            handler.Dispose();
        }

        private IEnumerator UnprepareHandler(PrepareHandler handler)
        {
            yield return handler;
            Unprepare(handler);
        }

        public PrepareHandler Prepare(string[] labels, Action onComplete, bool isConcurrent = false)
        {
            if (labels == null || labels.Length == 0)
            {
                onProcess?.Invole(1f);
                onComplete?.Invoke();
                return;
            }
            var handler = new PrepareHandler();
            handler.OnComplete(onComplete);
            ApplicationManager.instance.StartCoroutine(PrepareHandler(labels, onComplete, isConcurrent, handler));
            return handler;
        }

        private IEnumerator PrepareHandler(string[] labels, Action onComplete, bool isConcurrent, PrepareHandler handler)
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
                var doneCount = 0
                while (doneCount < labels.Length))
                {
                    doneCount = waitList.AnyAny(o=>o.isDone);
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

        private IEnumerator SaveBundleOnLocal(string bundleName, bytes[] data)
        {
            var path = Path.Combine(Application.streamingAssetsPath, bundleName);
            yield return File.WriteAllBytesAsync(path, bundleByte).AsCoroutine();
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
            ApplicationManager.instance.StartCoroutine(AsyncLoadAssetsBundleHandler(bundleName, newRequest));
            return newRequest;
        }

        private IEnumerator AsyncLoadAssetsBundleHandler(string bundleName, LoaderYieldInstruction<AssetBundle> loaderYieldInstruction)
        {
            yield return LoadBundleDependenceAsync(bundleName);
            AssetBundle bundle = null;
            var path = Path.Combine(Application.streamingAssetsPath, bundleName);
            Byte[] bundleByte = null;
            if (File.Exists(curFile))
            {
                var abcr = AssetBundle.LoadFromFileAsync(path);
                yield return abcr;
                bundle = abcr.assetBundle;
            }
            else
            {
                var url = Path.Combine(_remotePath, bundleName);
                using var webRequest = UnityWebRequestAssetBundle.GetAssetBundle(url);
                yield return webRequest;
                bundle = DownloadHandlerAssetBundle.GetContent(webRequest);
                bundleByte = webRequest.downloadHandler.data;
            }
            if (!bundle)
            {
                loaderYieldInstruction.SetAsset(null);
                OnBundleLoaded(bundleName, null);
                yield break;
            }
            loaderYieldInstruction.SetAsset(bundle);
            OnBundleLoaded(bundleName, bundle);

            if (bundleByte == null || !bundle) yield break;
            yield return SaveBundleOnLocal(bundleName, bundleByte);
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
            var path = Path.Combine(Application.streamingAssetsPath, bundleName);
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
        public bool UnloadAssetsBundle(AssetsBundleRef bundleRef)
        {
            var preload = _preloadHandles.Keys.ToList();
            foreach (var keyValue in preload)
            {
                path = keyValue.Key;
                var bundleName = File.Getw GetBundleNameByAsset(path);
                if (Path.GetFileNameWithoutExtension(bundleName) == bundleRef.Bundle.name)
                {
                    _preloadHandles[path].Dispose();
                    _preloadHandles.Remove(path);
                }
            }
            bundleRef.Bundle.Unload(false);
            _loadedBundleDic.Remove(bundleRef.Bundle.name);
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

        #endregion
    }
}