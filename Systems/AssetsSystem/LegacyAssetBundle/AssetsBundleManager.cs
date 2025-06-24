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
    public partial class AssetsBundleManager : IAssetManager//<AssetAssetLoader>
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

        private AssetBundleManifest _bundleManifest;
        private Dictionary<string, ScriptableAssetBundleData> _assetBundleDatas;
        private bool _inited = false;
        private string _remotePath = "http://localhost:8000/StreamingAssets/";

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

        public void Init(MonoBehaviour coroutineRunner, Action callBack)
        {
            if (_inited)
            {
                AssetLog.LogWarning("AssetsBundleManager has been initialized");
                callBack?.Invoke();
                return;
            }
            _preloadHandles = new Dictionary<string, LoaderYieldInstruction<Object>>();
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

        private IEnumerator InitHandler(Action callBack)
        {
            initState = AssetInitState.CheckForResourceUpdates;
            yield return GetServerRemoteManifest();
            GetClentRemoteManifest();
            yield return CheckRemoteBundle();
            yield return InitPathMap();
            if (_assetBundleDatas == null) yield break;
            initProcess = 0.3f;
            yield return GetBundleManifest();
            initProcess = 0.6f;
            var loadDefault = GetAssetsBundleAsync("default");
            yield return loadDefault;
            initProcess = 0.9f;
            var loaded = loadDefault.asset;
            if (!loaded)
            {
                AssetLog.LogError("AssetsBundleManager initialization failed");
                initProcess = 1f;
                yield break;
            }
            _inited = true;
            AssetLog.Log("AssetsBundleManager inited successfully");
            initProcess = 1f;
            initState = AssetInitState.Complete;
            callBack?.Invoke();
        }

        private IEnumerator InitPathMap()
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
                yield break;
            }
            _assetBundleDatas = new Dictionary<string, ScriptableAssetBundleData>();
            foreach (var scriptableAssetBundleData in bundleDatas.source)
            {
                if(scriptableAssetBundleData == null || string.IsNullOrEmpty(scriptableAssetBundleData.assetName)) continue;
                _assetBundleDatas.Add(scriptableAssetBundleData.assetName, scriptableAssetBundleData);
            }
            initProcess = 1f;
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

        #endregion
    }
}