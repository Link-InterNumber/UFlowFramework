using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PowerCellStudio
{
    public class AddressableManager: IAssetManager //<AddressableAssetLoader>
    {
        private bool _inited = false;
        public bool inited => _inited;

        public void Init(MonoBehaviour coroutineRunner, Action callBack)
        {
            if(inited)
            {
                callBack?.Invoke();
                return;
            }
            var handle = Addressables.InitializeAsync(false);
            coroutineRunner.StartCoroutine(InitHandle(handle, callBack));
        }

        public IAssetLoader CreateLoader()
        {
            return new AddressableAssetLoader(this);
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
                AssetLogger.LogWarning("Check Addressables Asset Fail!");
            }
            else
            {
                var catLogs = checkForCatalogUpdates.Result;
                if (catLogs == null || catLogs.Count <= 0)
                {
                    AssetLogger.Log("Check Addressables Asset Succeeded! Assets Is In Last Version");
                }
                else
                {
                    initState = AssetInitState.DownloadTheUpdateFile;
                    AssetLogger.Log("Check Addressables Asset Succeeded! Wait For Update");
                    var updateHandle = Addressables.UpdateCatalogs(catLogs, false);
                    yield return updateHandle;
                    var resourceList = updateHandle.Result;
                    foreach (var resourceLocator in resourceList)
                    {
                        var getDownloadSizeAsync = Addressables.GetDownloadSizeAsync(resourceLocator.Keys);
                        yield return getDownloadSizeAsync;
                        var percent = 0f;
                        if (getDownloadSizeAsync.Result > 0)
                        {
                            var downloadDependencies =
                                Addressables.DownloadDependenciesAsync(resourceLocator.Keys, Addressables.MergeMode.Union, false);
                            while (downloadDependencies.Status == AsyncOperationStatus.None)
                            {
                                percent += downloadDependencies.PercentComplete;
                                initProcess = percent / getDownloadSizeAsync.Result;
                                yield return null;
                            }
                            // yield return downloadDependencies;
                            if (downloadDependencies.Status == AsyncOperationStatus.Succeeded)
                            {
                                AssetLogger.Log($"Addressables Download {resourceLocator.LocatorId} Completed!");
                            }
                            else
                            {
                                AssetLogger.LogWarning($"Addressables Download {resourceLocator.LocatorId} Fail!");
                            }

                            Addressables.Release(downloadDependencies);
                        }
                        Addressables.Release(getDownloadSizeAsync);
                    }

                    Addressables.Release(updateHandle);
                    AssetLogger.Log("Addressables Asset Update Completed!");
                }
            }
            Addressables.Release(handle);
            Addressables.Release(checkForCatalogUpdates);
#if !UNITY_EDITOR
            yield return Addressables.CleanBundleCache();
#endif
            // Load Custom Remote Asset
            var remoteAssetIndexer = new RemoteAssetIndexer(AssetUtils.remotePath);
            yield return remoteAssetIndexer.Initialize(null, null); 
            
            _inited = true;
            initState = AssetInitState.Complete;
            callback?.Invoke();
        }

        public AsyncOperationHandle<GameObject> LoadGameObjectAsync(string address, Vector3 position, Transform parent, Quaternion quaternion)
        {
            return Addressables.InstantiateAsync(address, position, quaternion, parent);
        }

        public bool ReleaseGameObject(GameObject obj)
        {
            return Addressables.ReleaseInstance(obj);
        }

        private Dictionary<string, AsyncOperationHandle> _preloadHandlers = new Dictionary<string, AsyncOperationHandle>();

        public void PreloadAsset(string address)
        {
            if (_preloadHandlers.ContainsKey(address)) return;
            var handle = Addressables.LoadAssetAsync<Object>(address);
            _preloadHandlers[address] = handle;
        }

        public AsyncOperationHandle<T> LoadAsync<T>(string address) where T : Object
        {
            if (_preloadHandlers.TryGetValue(address, out var handle))
            {
                return handle.Convert<T>();
            }
            return Addressables.LoadAssetAsync<T>(address);
        }
        
        public AsyncOperationHandle<IList<T>> LoadAllAsync<T>(string key) where T : Object
        {
            return Addressables.LoadAssetsAsync<T>(key, null, true);
        }

        public AsyncOperationHandle<IList<T>> LoadAllAsync<T>(string address, string label) where T : Object
        {
            var key = new List<object>  { address, label };
            return Addressables.LoadAssetsAsync<T>(key, null, Addressables.MergeMode.Intersection, true);
        }
        
        public AsyncOperationHandle<IList<T>> LoadAllAsync<T>(string address, params string[] keys) where T : Object
        {
            var key = new List<object>  { address };
            key.AddRange(keys);
            return Addressables.LoadAssetsAsync<T>(key, null, Addressables.MergeMode.Intersection, true);
        }

        public void Release(AsyncOperationHandle handle)
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
        /// <param name="onFailed">加载失败回调</param>
        /// <param name="unLoadOtherScene">卸载其他场景</param>
        public void LoadScene(string sceneName, Action onComplete, Action onFailed, bool unLoadOtherScene = false)
        {
            if (_sceneInstances.Any(o => o.Result.Scene.name.Equals(sceneName)))
                return;
            
            var handle = Addressables.LoadSceneAsync(sceneName, unLoadOtherScene ? LoadSceneMode.Single : LoadSceneMode.Additive);
            if (unLoadOtherScene) handle.Completed += UnLoadOtherScene;
            else handle.Completed += OnSceneLoaded;
            if (onComplete != null) 
                handle.Completed += (a) => 
                    {
                        if (a.Status == AsyncOperationStatus.Succeeded) onComplete?.Invoke();
                        else onFailed?.Invoke();
                    };
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

        public void ClearUnusedAsset()
        {
            foreach (var handler in _preloadHandlers.Values)
            {
                Addressables.Release(handler);
            }
            _preloadHandlers.Clear();
            Resources.UnloadUnusedAssets();
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