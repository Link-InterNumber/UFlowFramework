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
                AssetLogger.LogWarning("AssetsBundleManager has been initialized");
                callBack?.Invoke();
                return;
            }
            _loadPlan = new LoadPlan();
            _loadedAssets = new LoadedCache<Object>();
            _loadingAssets = new AssetLoadingHolder<Object>();
            _loadedBundles = new LoadedCache<AssetBundle>();
            _loadingBundles = new BundleLoadingHolder();
            _removedAssetHolder = new GameObject("RemovedAssetHolder").AddComponent<RemovedAssetHolder>();
            GameObject.DontDestroyOnLoad(_removedAssetHolder.gameObject);
            
#if UNITY_EDITOR
            if (!simulateAssetBundleInEditor)
            {
                _inited = true;
                initState = AssetInitState.Complete;
                initProcess = 1f;
                AssetLogger.Log("AssetsBundleManager inited successfully");
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
            initProcess = 0f;
            yield return InitializeRemoteBundleManifest();
            initState = AssetInitState.InitModule;
            initProcess = 0f;
            yield return InitPathMap();
            if (_bundleIndex == null) yield break;
            initProcess = 0.3f;
            yield return GetBundleManifest();
            initProcess = 0.6f;
            GetAssetsBundleAsync("default");
            while (_loadingBundles.IsLoading("default"))
            {
                yield return null;
            }
            initProcess = 0.9f;
            _loadedBundles.TryGetCache("default", out var loaded);
            if (!loaded)
            {
                AssetLogger.LogError("default bundle did not exit!");
            }
            AddBundleRef("default");
            _inited = true;
            AssetLogger.Log("AssetsBundleManager inited successfully");
            initProcess = 1f;
            initState = AssetInitState.Complete;
            callBack?.Invoke();
        }
    }
}