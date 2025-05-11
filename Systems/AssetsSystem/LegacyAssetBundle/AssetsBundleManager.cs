using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PowerCellStudio
{
    public class AssetsBundleManager : IAssetManager//<AssetAssetLoader>
    {
        public static bool simulateAssetBundleInEditor = false;
        /// <summary>
        /// 卸载bundle的引用计数下限
        /// </summary>
        public static int disposeRefLine = 0;

        /// <summary>
        /// 是否开启自动卸载
        /// </summary>
        // public static bool EnableAutoUnload = true;

        public static float delayUnloadDuration = 10f;
        private Dictionary<string, AssetsBundleRef> _loadedBundleDic;
        private Dictionary<string, LoaderYieldInstruction<AssetBundle>> _waitForLoadList;
        private AssetBundleManifest _bundleManifest;
        private Dictionary<string ,ScriptableAssetBundleData> _assetBundleDatas;
        private bool _inited = false;
        private string _webglCdn = "http://localhost:8000/StreamingAssets/";

        private string _mainBundleName
        {
            get
            {
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
                    case RuntimePlatform.XboxOne:
                        return "XboxOne";
                    case RuntimePlatform.tvOS:
                        return "tvOS";
                    case RuntimePlatform.Switch:
                        return "Switch";
                    case RuntimePlatform.GameCoreXboxSeries:
                        return "XboxSeries";
                    case RuntimePlatform.GameCoreXboxOne:
                        return "XboxOne";
                    case RuntimePlatform.PS5:
                        return "PS5";
                    default:
                        return "StandaloneWindows";
                }
            }
        }

        public AssetInitState initState { get; private set; }
        public float initProcess { get; private set; }

        public bool IsAssetsBundleLoaded(string bundleName)
        {
            return _loadedBundleDic.ContainsKey(bundleName);
        }

        public void Init(MonoBehaviour coroutineRunner, Action callBack)
        {
            if (_inited)
            {
                AssetLog.LogWarning("AssetsBundleManager has been initialized");
                callBack?.Invoke();
                return;
            }
            _loadedBundleDic = new Dictionary<string, AssetsBundleRef>();
            _waitForLoadList = new Dictionary<string, LoaderYieldInstruction<AssetBundle>>();
            
            _pool = new ObjectPool<BundleAssetLoader>(() => new BundleAssetLoader(this),
                loader => loader.Init(),
                loader => loader.Deinit(),
                loader => loader.Deinit(), true, 10, 30);
            _activeLoader = new Dictionary<long, BundleAssetLoader>();
            
#if UNITY_EDITOR
            if (!simulateAssetBundleInEditor)
            {
                _inited = true;
                initState = AssetInitState.Complete;
                initProcess = 1f;
                AssetLog.Log("AssetsBundleManager inited successfully");
                callBack?.Invoke();
                return;
            }
#endif
            coroutineRunner.StartCoroutine(InitHandler(callBack));
        }

        private ObjectPool<BundleAssetLoader> _pool;
        private Dictionary<long, BundleAssetLoader> _activeLoader;
        
        public IAssetLoader SpawnLoader(string tag= "")
        {
            var loader = _pool.Get();
            loader.tag = tag;
            _activeLoader.Add(loader.index, loader);
            return loader;
        }

        public void DeSpawnLoader(IAssetLoader assetLoader)
        {
            if (assetLoader == null) return;
            var assetBundleLoader = assetLoader as BundleAssetLoader;
            if (assetBundleLoader == null) return;
            _activeLoader.Remove(assetBundleLoader.index);
            if (!assetBundleLoader.spawned)
            {
                assetBundleLoader.Deinit();
                return;
            }
            _pool.Release(assetBundleLoader);
        }

        public void DeSpawnAllLoader()
        {
            while (_activeLoader.Count > 0)
            {
                var loader = _activeLoader.First().Value;
                _activeLoader.Remove(loader.index);
                if(!loader.spawned)
                {
                    loader.Deinit();
                    continue;
                }
                _pool.Release(loader);
            }
        }

        public void DeSpawnLoaderByTag(string tag)
        {
            var loaders = _activeLoader.Where(o => o.Value.tag.Equals(tag)).ToArray();
            if(loaders.Length == 0) return;
            foreach (var addressableLoader in loaders)
            {
                DeSpawnLoader(addressableLoader.Value);
            }
        }

        public void LoadScene(string sceneName, Action onComplete, bool unLoadOtherScene = false)
        {
            var handler = SceneManager.LoadSceneAsync(sceneName, unLoadOtherScene ? LoadSceneMode.Single : LoadSceneMode.Additive);
            handler.completed += (operation) =>
            {
                onComplete?.Invoke();
            };
        }

        public void UnloadScene(string name)
        {
            SceneManager.UnloadSceneAsync(name);
        }

        private IEnumerator InitHandler(Action callBack)
        {
            initProcess = 0f;
            initState = AssetInitState.InitModule;
            var path = Path.Combine(ConstSetting.BundleAssetConfigFolder, Path.GetFileNameWithoutExtension(ConstSetting.BundleAssetConfigName));
            var resourceRequest = Resources.LoadAsync<ScriptableAssetBundle>(path);
            yield return resourceRequest;
            var bundleDatas = resourceRequest.asset as ScriptableAssetBundle;
            if (bundleDatas == null)
            {
                AssetLog.LogError("AssetsBundleManager initialization failed");
                initProcess = 1f;
                yield break;
            }
            _assetBundleDatas = new Dictionary<string, ScriptableAssetBundleData>();
            foreach (var scriptableAssetBundleData in bundleDatas.source)
            {
                if(scriptableAssetBundleData == null || string.IsNullOrEmpty(scriptableAssetBundleData.assetName)) continue;
                _assetBundleDatas.Add(scriptableAssetBundleData.assetName, scriptableAssetBundleData);
            }
            yield return GetBundleManifest();
            initProcess = 0.3f;
            initState = AssetInitState.CheckForResourceUpdates;
            var loadDefault = GetAssetsBundleAsync("default");
            yield return loadDefault;
            var loaded = loadDefault.asset;
            if (!loaded)
            {
                AssetLog.LogError("AssetsBundleManager initialization failed");
                initProcess = 1f;
                yield break;
            }
            _inited = true;
            AssetLog.Log("AssetsBundleManager inited successfully");
            initState = AssetInitState.Complete;
            callBack?.Invoke();
        }

        // public bool CheckWithID = false;

        public string GetBundleNameByAsset(string path)
        {
            if (!_inited) throw new Exception("AssetsBundleManager do not inited!!!");
            var lowerPath = path.ToLower();
            if (!_assetBundleDatas.TryGetValue(lowerPath, out var matched)) return string.Empty;
            return matched.assetBundle;
            // if (!CheckWithID) return matched.assetBundle;
            // var id = lowerPath.GenHashCode();
            // return matched.hashCode.Equals(id) ? matched.assetBundle : string.Empty;
        }

        #region BundleDependence

        private IEnumerator GetBundleManifest()
        {
            var mainBundleName = _mainBundleName;
            var path = Path.Combine(Application.streamingAssetsPath, mainBundleName);
            _waitForLoadList.Add(mainBundleName, null);
            _loadedBundleDic.Remove(mainBundleName);
            var loadedBundleRequest = AssetBundle.LoadFromFileAsync(path);
            yield return loadedBundleRequest;
            _waitForLoadList.Remove(mainBundleName);
            var loadedBundle = loadedBundleRequest.assetBundle;
            var abf = new AssetsBundleRef(loadedBundle, this);
            _loadedBundleDic.Add(mainBundleName, abf);
            abf.AddRef();
            if (!loadedBundle)
            {
                AssetLog.LogError($"MainBundle Name Error: {mainBundleName}");
                yield break;
            }
            _bundleManifest = loadedBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        }

        private string[] GetBundleDependencies(string bundleName)
        {
            var dependencies = _bundleManifest.GetAllDependencies(bundleName);
            return dependencies ?? Array.Empty<string>();
        }

        private void LoadBundleDependence(string bundleName)
        {
            var dependencies = GetBundleDependencies(bundleName);
            foreach (var name in dependencies)
            {
                if (name.Equals(bundleName)) continue;
                GetAssetBundle(name, out _);
            }
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

        #region Load

        public delegate void BundleLoadEvent(string bundleName, AssetBundle bundle);

        public event BundleLoadEvent onBundleLoaded;

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
#if UNITY_WEBGL
            GetAssetsBundleAsync(bundleName, null);
            return null;
#endif
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
                AssetLog.LogError($"bundleName={bundleName} do not exist");
                Debug.LogError(e);
            }
            finally
            {
                OnBundleLoaded(bundleName, loadedBundle);
            }
            return loadedBundle;
        }

        private void OnBundleLoaded(string bundleName, AssetBundle loadedBundle)
        {
            if(_waitForLoadList.TryGetValue(bundleName, out var request))
            {
                request?.Dispose();
            }
            _waitForLoadList.Remove(bundleName);
            if (!loadedBundle)
            {
                AssetLog.LogError($"Bundle: {bundleName} Load Fail");
                return;
            }
            var abf = new AssetsBundleRef(loadedBundle, this);
            _loadedBundleDic.Add(bundleName, abf);
            onBundleLoaded?.Invoke(bundleName, loadedBundle);
        }

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
#if UNITY_WEBGL
            var url = Path.Combine(_webglCdn, bundleName);
            var webRequest = UnityWebRequestAssetBundle.GetAssetBundle(url);
            yield return webRequest;
            bundle = DownloadHandlerAssetBundle.GetContent(webRequest);
#else
            var path = Path.Combine(Application.streamingAssetsPath, bundleName);
            var abcr = AssetBundle.LoadFromFileAsync(path);
            yield return abcr;
            bundle = abcr.assetBundle;
#endif
            if (!bundle)
            {
                OnBundleLoaded(bundleName, null);
                loaderYieldInstruction.SetAsset(null);
                yield break;
            }
            OnBundleLoaded(bundleName, bundle);
            loaderYieldInstruction.SetAsset(bundle);
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
        
        public LoaderYieldInstruction<T> LoadAsset<T>(string bundleName, string assetPath)
            where T : Object
        {
            var loadAssetRequest = new LoaderYieldInstruction<T>(assetPath);
            if (GetAssetBundle(bundleName, out var bundle))
            {
                if (bundle == null)
                {
                    loadAssetRequest.SetAsset(null);
                    return loadAssetRequest;
                }
                var asset = bundle.LoadAsset<T>(assetPath);
                loadAssetRequest.SetAsset(asset);
            }
            else
            {
                loadAssetRequest.SetAsset(null);
            }
            return loadAssetRequest;
        }
        
        public void LoadAssetAsync<T>(string bundleName, string assetPath, LoaderYieldInstruction<T> loadAssetRequest)
            where T : Object
        {
            if (loadAssetRequest == null) return;
            var loadBundleRequest = GetAssetsBundleAsync(bundleName);
            if (loadBundleRequest.isDone)
            {
                var bundle = loadBundleRequest.asset;
                if (!bundle)
                {
                    loadAssetRequest.SetAsset(null);
                    return;
                }
                var assetRequest = bundle.LoadAssetAsync<T>(assetPath);
                assetRequest.completed += (operation) =>
                {
                    var operationHandle = operation as AssetBundleRequest;
                    if(operationHandle == null)
                    {
                        loadAssetRequest.SetAsset(null);
                        return;
                    }
                    var asset = operationHandle.asset as T;
                    loadAssetRequest.SetAsset(asset);
                };
                return;
            }
            loadBundleRequest.onLoadSuccess += (bundle, bundleName) =>
            {
                if (!bundle)
                {
                    loadAssetRequest.SetAsset(null);
                    return;
                }
                var assetRequest = bundle.LoadAssetAsync<T>(assetPath);
                assetRequest.completed += (operation) =>
                {
                    var operationHandle = operation as AssetBundleRequest;
                    if(operationHandle == null)
                    {
                        loadAssetRequest.SetAsset(null);
                        return;
                    }
                    var asset = operationHandle.asset as T;
                    loadAssetRequest.SetAsset(asset);
                };
            };
        }

        #endregion
        
        #region Unload

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
        /// Do not use outside the AssetsBundleManager, use AssetBundleManager.instance.ReleaseBundle(string bundleName) instead if you want to unload a bundle
        /// </summary>
        /// <param name="bundleRef"></param>
        /// <returns></returns>
        public bool UnloadAssetsBundle(AssetsBundleRef bundleRef)
        {
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