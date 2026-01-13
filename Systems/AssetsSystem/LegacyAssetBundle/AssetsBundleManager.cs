using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace PowerCellStudio
{
    public partial class AssetsBundleManager : IAssetManager//<AssetAssetLoader>
    {
        public static bool simulateAssetBundleInEditor = true;
        /// <summary>
        /// 卸载bundle的引用计数下限
        /// </summary>
        public static int disposeRefLine = 0;

        public static float delayUnloadDuration = 10f;

        private bool _inited = false;
        
        public AssetInitState initState { get; private set; }
        public float initProcess { get; private set; }

        private MonoBehaviour _coroutineRunner;

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
            _coroutineRunner = coroutineRunner;
            _coroutineRunner.StartCoroutine(InitHandler(callBack));
            EventManager.instance.onClearUnusedAsset.AddListener(ClearUnusedAsset);
        }

        private IEnumerator InitHandler(Action callBack)
        {
            _bundleFoldName = MainBundleName;
            initState = AssetInitState.CheckForResourceUpdates;
            yield return GetServerRemoteManifest();
            GetClientRemoteManifest();
            yield return CheckRemoteBundle();
            yield return InitPathMap();
            if (_assetPathMap == null) yield break;
            initProcess = 0.3f;
            initState = AssetInitState.InitModule;
            yield return GetBundleManifest();
            initProcess = 0.6f;
            GetAssetsBundleAsync("default", null);
            var loadDefault = _waitForLoadList["default"];
            yield return loadDefault;
            initProcess = 0.9f;
            _loadedBundleDic.TryGetValue("default", out var bundleRef);
            var loaded = bundleRef?.Bundle;
            if (!loaded)
            {
                AssetLog.LogError("default bundle did not exit!");
            }
            AddRef("default");
            _inited = true;
            AssetLog.Log("AssetsBundleManager inited successfully");
            initProcess = 1f;
            initState = AssetInitState.Complete;
            callBack?.Invoke();
        }

        // public bool CheckWithID = false;

        public string GetBundleNameByAsset(string path)
        {
            if (!_inited) throw new Exception("AssetsBundleManager do not inited!!!");
            if (!_assetPathMap.TryGetValue(path, out var matched))
            {
                AssetLog.LogError($"Can not find Bundle Name of [{path}]");
                return string.Empty;
            }
            return matched.assetBundle;
            // if (!CheckWithID) return matched.assetBundle;
            // var id = lowerPath.GenHashCode();
            // return matched.hashCode.Equals(id) ? matched.assetBundle : string.Empty;
        }
    }
}