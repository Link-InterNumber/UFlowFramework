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
            _loadPlan = new LoadPlan();
            _loadedAssets = new LoadedCache<Object>();
            _loadingAssets = new AssetLoadingHolder<Object>();
            _loadedBundles = new LoadedCache<AssetBundle>();
            _loadingBundles = new BundleLoadingHolder();
            _removedAssetHolder = new GameObject("RemovedAssetHolder").AddComponent<RemovedAssetHolder>();

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
            yield return InitializeRemoteBundleManifest();
            yield return InitPathMap();
            if (_bundleIndex == null) yield break;
            initProcess = 0.3f;
            initState = AssetInitState.InitModule;
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
                AssetLog.LogError("default bundle did not exit!");
            }
            AddBundleRef("default");
            _inited = true;
            AssetLog.Log("AssetsBundleManager inited successfully");
            initProcess = 1f;
            initState = AssetInitState.Complete;
            callBack?.Invoke();
        }
    }
}