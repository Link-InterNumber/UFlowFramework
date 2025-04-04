using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.Pool;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PowerCellStudio
{
    public class AddressableManager: IAssetManager<AddressableAssetLoader>
    {
        private ObjectPool<AddressableAssetLoader> _pool;
        private Dictionary<long, AddressableAssetLoader> _activeLoader;

        private bool _inited = false;
        public bool inited => _inited;

        public void Init(MonoBehaviour coroutineRunner, Action callBack)
        {
            if(inited) return;
            _pool = new ObjectPool<AddressableAssetLoader>(() => new AddressableAssetLoader(),
                loader => loader.Init(),
                loader => loader.Deinit(),
                loader => loader.Deinit(), true, 10, 30);
            _activeLoader = new Dictionary<long, AddressableAssetLoader>();
            var handle = Addressables.InitializeAsync(false);
            coroutineRunner.StartCoroutine(InitHandle(handle, callBack));
        }

        public AddressableAssetLoader SpawnLoader(string tag = "")
        {
            var loader = _pool.Get();
            loader.tag = tag;
            _activeLoader.Add(loader.index, loader);
            return loader;
        }

        public void DeSpawnLoader(AddressableAssetLoader assetLoader)
        {
            if(assetLoader == null) return;
            _activeLoader.Remove(assetLoader.index);
            if(!assetLoader.spawned)
            {
                assetLoader.Deinit();
                return;
            }
            _pool.Release(assetLoader);
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

        public AssetInitState initState { get; private set; }
        public float initProcess { get; private set; }

        private IEnumerator InitHandle(AsyncOperationHandle<IResourceLocator> handle, Action callback)
        {
            initState = AssetInitState.InitModule;
            while (handle.Status == AsyncOperationStatus.None)
            {
                initProcess = handle.PercentComplete;
                yield return null;
            }
            initState = AssetInitState.CheckForResourceUpdates;
            var checkForCatalogUpdates = Addressables.CheckForCatalogUpdates(false);
            while (checkForCatalogUpdates.Status == AsyncOperationStatus.None)
            {
                initProcess = checkForCatalogUpdates.PercentComplete;
                yield return null;
            }
            // yield return checkForCatalogUpdates;
            if (checkForCatalogUpdates.Status != AsyncOperationStatus.Succeeded)
            {
                AssetLog.LogWarning("Check Addressables Asset Fail!");
            }
            else
            {
                var catLogs = checkForCatalogUpdates.Result;
                if (catLogs == null || catLogs.Count <= 0)
                {
                    AssetLog.Log("Check Addressables Asset Succeeded! Assets Is In Last Version");
                }
                else
                {
                    initState = AssetInitState.DownloadTheUpdateFile;
                    AssetLog.Log("Check Addressables Asset Succeeded! Wait For Update");
                    var updateHandle = Addressables.UpdateCatalogs(catLogs, false);
                    yield return updateHandle;
                    var resourceList = updateHandle.Result;
                    foreach (var resourceLocator in resourceList)
                    {
                        var keys = ListPool<object>.Get();
                        keys.AddRange(resourceLocator.Keys);
                        var getDownloadSizeAsync = Addressables.GetDownloadSizeAsync(keys);
                        yield return getDownloadSizeAsync;
                        var percent = 0f;
                        if (getDownloadSizeAsync.Result > 0)
                        {
                            var downloadDependencies =
                                Addressables.DownloadDependenciesAsync(keys, Addressables.MergeMode.Union, false);
                            while (downloadDependencies.Status == AsyncOperationStatus.None)
                            {
                                percent += downloadDependencies.PercentComplete;
                                initProcess = percent / getDownloadSizeAsync.Result;
                                yield return null;
                            }
                            // yield return downloadDependencies;
                            if (downloadDependencies.Status == AsyncOperationStatus.Succeeded)
                            {
                                AssetLog.Log($"Addressables Download {resourceLocator.LocatorId} Completed!");
                            }
                            else
                            {
                                AssetLog.LogWarning($"Addressables Download {resourceLocator.LocatorId} Fail!");
                            }

                            Addressables.Release(downloadDependencies);
                        }
                        Addressables.Release(getDownloadSizeAsync);
                        ListPool<object>.Release(keys);
                    }

                    Addressables.Release(updateHandle);
                    AssetLog.Log("Addressables Asset Update Completed!");
                }
            }
            Addressables.Release(handle);
            Addressables.Release(checkForCatalogUpdates);
            _inited = true;
            initState = AssetInitState.Complete;
            callback?.Invoke();
        }

        public static AsyncOperationHandle<GameObject> LoadGameObjectAsync(string address, Vector3 position, Transform parent, Quaternion quaternion)
        {
            return Addressables.InstantiateAsync(address, position, quaternion, parent);
        }

        public static bool ReleaseGameObject(GameObject obj)
        {
            return Addressables.ReleaseInstance(obj);
        }

        public static AsyncOperationHandle<T> LoadAsync<T>(string address) where T : Object
        {
            return Addressables.LoadAssetAsync<T>(address);
        }
        
        public static AsyncOperationHandle<IList<T>> LoadAllAsync<T>(string key) where T : Object
        {
            return Addressables.LoadAssetsAsync<T>(key, null, true);
        }

        public static AsyncOperationHandle<IList<T>> LoadAllAsync<T>(string address, string label) where T : Object
        {
            var key = new List<object>  { address, label };
            return Addressables.LoadAssetsAsync<T>(key, null, Addressables.MergeMode.Intersection, true);
        }
        
        public static AsyncOperationHandle<IList<T>> LoadAllAsync<T>(string address, params string[] keys) where T : Object
        {
            var key = new List<object>  { address };
            key.AddRange(keys);
            return Addressables.LoadAssetsAsync<T>(key, null, Addressables.MergeMode.Intersection, true);
        }

        public static void Release(AsyncOperationHandle handle)
        {
            if (!handle.IsDone)
            {
                handle.Completed += Addressables.Release;
                return;
            }
            Addressables.Release(handle);
        }

        private List<AsyncOperationHandle<SceneInstance> > _sceneInstances = new List<AsyncOperationHandle<SceneInstance>>();

        /// <summary>
        /// 加载场景
        /// </summary>
        /// <param name="sceneName">场景名</param>
        /// <param name="onComplete">完成回调</param>
        /// <param name="unLoadOtherScene">卸载其他场景</param>
        public void LoadScene(string sceneName, Action onComplete, bool unLoadOtherScene = false)
        {
            if (_sceneInstances.Any(o => o.Result.Scene.name.Equals(sceneName)))
                return;
            
            var handle = Addressables.LoadSceneAsync(sceneName, unLoadOtherScene ? LoadSceneMode.Single : LoadSceneMode.Additive);
            if (unLoadOtherScene) handle.Completed += UnLoadOtherScene;
            else handle.Completed += OnSceneLoaded;
            if (onComplete != null) handle.Completed += (a) => { onComplete.Invoke(); };
        }

        private void UnLoadOtherScene(AsyncOperationHandle<SceneInstance> handle)
        {
            if(handle.Status != AsyncOperationStatus.Succeeded) return;
            foreach (var asyncOperationHandle in _sceneInstances)
            {
                UnloadScene(asyncOperationHandle);
            }
            _sceneInstances.Clear();
            _sceneInstances.Add(handle);
        }

        private void OnSceneLoaded(AsyncOperationHandle<SceneInstance> handle)
        {
            if(handle.Status != AsyncOperationStatus.Succeeded) return;
            _sceneInstances.Add(handle);
        }

        /// <summary>
        /// 卸载场景
        /// </summary>
        /// <param name="name">场景名</param>
        public void UnloadScene(string name)
        {
            for (var i = 0; i < _sceneInstances.Count; i++)
            {
                var asyncOperationHandle = _sceneInstances[i];
                if (!asyncOperationHandle.Result.Scene.name.Equals(name)) continue;
                UnloadScene(asyncOperationHandle);
                _sceneInstances.RemoveAt(i);
                break;
            }
        }

        /// <summary>
        /// 卸载场景
        /// </summary>
        /// <param name="sceneInstance">场景实例</param>
        public void UnloadScene(SceneInstance sceneInstance)
        {
            var sceneName = sceneInstance.Scene.name;
            for (var i = 0; i < _sceneInstances.Count; i++)
            {
                var asyncOperationHandle = _sceneInstances[i];
                if (!asyncOperationHandle.Result.Scene.name.Equals(sceneName)) continue;
                UnloadScene(asyncOperationHandle);
                _sceneInstances.RemoveAt(i);
                break;
            }
            // Addressables.UnloadSceneAsync(sceneInstance);
        }
        
        private void UnloadScene(AsyncOperationHandle<SceneInstance> handle)
        {
            Addressables.UnloadSceneAsync(handle);
        }
        
        // #region GameObject Pool
        //
        // public void SpawnAsync(string address, Action<GameObject> action)
        // {
        //     if (!PoolManager.Instance.IsRegister(address))
        //     {
        //         var prefabHandle = LoadAsync<GameObject>(address);
        //         PoolManager.Instance.Register(address)
        //     }
        // }
        //
        // #endregion
    }
}