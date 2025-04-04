using System;
// using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
// using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace PowerCellStudio
{
    public class AddressableAssetLoader: IAssetLoader, IIndex
    {
        private bool _spawned = false;
        public bool spawned => _spawned;
        
        public string tag { get; set; }
        
        private long _index;
        public long index => _index;
        public AddressableAssetLoader()
        {
            _index = IndexGetter.instance.Get<AddressableAssetLoader>();
        }
        
        private Dictionary<string, AsyncOperationHandle> _handles;

        #region Methods used in AddressableManager

        /// <summary>
        /// Don't use this method
        /// </summary>
        public void Init()
        {
            if(_spawned) return;
            if(_handles == null) _handles = new Dictionary<string, AsyncOperationHandle>();
            _handles.Clear();
            _spawned = true;
        }
        
        private void ReleaseAllHandle()
        {
            if(_handles == null) return;
            foreach (var (_, handle) in _handles)
            {
                if(handle.IsValid())
                    AddressableManager.Release(handle);
            }
            _handles.Clear();
        }
        
        /// <summary>
        /// Don't use this method
        /// </summary>
        public void Deinit()
        {
            ReleaseAllHandle();
            _handles = null;
            _spawned = false;
        }

        public bool Release(string address)
        {
            if(_handles.TryGetValue(address, out var handle))
            {
                _handles.Remove(address);
                if(handle.IsValid())
                {
                    AddressableManager.Release(handle);
                    return true;
                }
            }
            return false;
        }

        public bool IsLoading(string address)
        {
            if(_handles.TryGetValue(address, out var handle))
            {
                if (handle.IsValid())
                    return !handle.IsDone;
            }
            return false;
        }

        #endregion

        #region GameObject

        public AsyncOperationHandle<GameObject> GetInstantiateHandle(string address, Vector3 pos, Transform parent = null, Quaternion quaternion = default)
        {
            return AddressableManager.LoadGameObjectAsync(address, 
                    pos, 
                    parent, 
                    quaternion.Equals(default)? Quaternion.identity : quaternion);
        }

        /// <summary>
        /// 非多线程平台会报错
        /// </summary>
        /// <param name="address"></param>
        /// <param name="pos"></param>
        /// <param name="parent"></param>
        /// <param name="quaternion"></param>
        /// <returns></returns>
//         public GameObject LoadNInstantiate(string address, Vector3 pos, Transform parent = null, Quaternion quaternion = default)
//         {
//             var handle = GetInstantiateHandle(address, pos, parent, quaternion);
//             // _prefabHandle.Add(handle);
// // #if UNITY_EDITOR
// //             var taskCompletionSource = new TaskCompletionSource<GameObject>();
// //             var task1 = taskCompletionSource.Task;
// //             var task = handle.Task;
// //             Task.Run(async () =>
// //             {
// //                 var go = await task;
// //                 taskCompletionSource.SetResult(go);
// //             });
// //             return task1.Result;
// // #endif
//             var gameObj = handle.WaitForCompletion();
//             if (gameObj == null)
//             {
//                 AddressableManager.Release(handle);
//                 return null;
//             }
//             gameObj.AddComponent<AddressableGameObjectSelfCleanup>();
//             return gameObj;
//         }
        
        public void AsyncLoadNInstantiate(string address, Action<GameObject> onSuccess, Action onFail = null)
        {
            var handle = GetInstantiateHandle(address, Vector3.zero, null, Quaternion.identity);
            handle.Completed += operationHandle =>
            {
                if (operationHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    operationHandle.Result.AddComponent<AddressableGameObjectSelfCleanup>();
                    onSuccess?.Invoke(operationHandle.Result);
                }
                else
                {
                    AddressableManager.Release(handle);
                    AssetLog.LogError($"Load Prefab [{address}] Fail!");
                    onFail?.Invoke();
                }
            };
        }

        public void AsyncLoadNInstantiate(string address, Transform parent, Action<GameObject> onSuccess, Action onFail = null)
        {
            var handle = GetInstantiateHandle(address, Vector3.zero, parent, Quaternion.identity);
            handle.Completed += operationHandle =>
            {
                if (operationHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    operationHandle.Result.transform.localScale = Vector3.one;
                    operationHandle.Result.transform.localPosition = Vector3.zero;
                    operationHandle.Result.transform.rotation = Quaternion.identity;
                    operationHandle.Result.AddComponent<AddressableGameObjectSelfCleanup>();
                    onSuccess?.Invoke(operationHandle.Result);
                }
                else
                {
                    AddressableManager.Release(handle);
                    AssetLog.LogError($"Load Prefab [{address}] Fail!");
                    onFail?.Invoke();
                }
            };
        }

        public async Task<GameObject> LoadNInstantiateTask(string address)
        {
            var handle = GetInstantiateHandle(address, Vector3.zero, null, Quaternion.identity);
            var obj = await handle.Task;
            if (obj == null)
            {
                AddressableManager.Release(handle);
                return null;
            }
            obj.AddComponent<AddressableGameObjectSelfCleanup>();
            return obj;
        }

        #endregion

        #region load from Addressables

        private AsyncOperationHandle<T> GetLoadHandle<T>(string address) where T : Object
        {
            if(_handles.TryGetValue(address, out var handle))
            {
                var asyncHandle = handle.Convert<T>();
                if (asyncHandle.IsValid())
                    return asyncHandle;
            }
            var newHandle = AddressableManager.LoadAsync<T>(address);
            _handles.Add(address, newHandle);
            return newHandle;
        }

#if !UNITY_WEBGL
        /// <summary>
        /// 非多线程平台会报错
        /// </summary>
        /// <param name="address"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        // public T LoadImmediately<T>(string address) where T : Object
        // {
        //     var handle = GetLoadHandle<T>(address);
        //     if (handle.IsDone)
        //     {
        //         if (handle.Status == AsyncOperationStatus.Succeeded)
        //             return handle.Result;
        //         AssetLog.LogError($"Load {typeof(T).Name} Asset Fail!");
        //         return null;
        //     }
        //     var asset = handle.WaitForCompletion();
        //     if (asset == null)
        //     {
        //         ReleaseHandle(address);
        //     }
        //     return asset == default(T) ? null : asset;
        // }
        
        /// <summary>
        /// 非多线程平台会报错
        /// </summary>
        /// <param name="assetReference">assetReference</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        // public T LoadImmediately<T>(AssetReferenceT<T> assetReference) where T : Object
        // {
        //     var handle = GetLoadHandle<T>(assetReference);
        //     if (handle.IsDone)
        //     {
        //         if (handle.Status == AsyncOperationStatus.Succeeded)
        //             return handle.Result;
        //         AssetLog.LogError($"Load {typeof(T).Name} Asset Fail!");
        //         return null;
        //     }
        //     var asset = handle.WaitForCompletion();
        //     return asset == default(T) ? asset : null;
        // }
#endif

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <typeparam name="T">要加载的资源类型</typeparam>
        /// <param name="address">资源的地址</param>
        /// <param name="onSuccess">资源成功加载时调用的回调</param>
        /// <param name="onFail">资源加载失败时调用的回调</param>
        public void LoadAsync<T>(string address, Action<T> onSuccess, Action onFail = null) where T : Object
        {
            var handle = GetLoadHandle<T>(address);
            if (handle.IsDone)
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                    onSuccess?.Invoke(handle.Result);
                else
                {
                    AssetLog.LogError($"Load {typeof(T).Name} Asset Fail!");
                    onFail?.Invoke();
                }
                return;
            }
            handle.Completed += operationHandle =>
            {
                if(operationHandle.Status == AsyncOperationStatus.Succeeded) 
                    onSuccess?.Invoke(operationHandle.Result);
                else
                {
                    AssetLog.LogError($"Load {typeof(T).Name} Asset [{address}] Fail!");
                    onFail?.Invoke();
                }
            };
        }

        /// <summary>
        /// 异步加载资源并返回任务
        /// </summary>
        /// <typeparam name="T">要加载的资源类型</typeparam>
        /// <param name="address">资源的地址</param>
        /// <returns>表示异步加载操作的任务</returns>
        public Task<T> LoadTask<T>(string address) where T : Object
        {
            var handle = GetLoadHandle<T>(address);
            return handle.Task;
        }
        
        /// <summary>
        /// 将资源加载为协程支持对象
        /// </summary>
        /// <typeparam name="T">要加载的资源类型</typeparam>
        /// <param name="address">资源的地址</param>
        /// <returns>协程支持对象</returns>
        public LoaderYieldInstruction<T> LoadAsYieldInstruction<T>(string address) where T : Object
        {
            var handle = GetLoadHandle<T>(address);
            var instruction = new LoaderYieldInstruction<T>(address);
            if (handle.IsDone)
            {
                instruction.SetAsset(handle.Result);
                return instruction;
            }
            handle.Completed += operationHandle =>
            {
                if (operationHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    instruction.SetAsset(operationHandle.Result);
                }
                else
                {
                    AssetLog.LogError($"Load {typeof(T).Name} Asset Fail!");
                    AddressableManager.Release(handle);
                    instruction.SetAsset(null);
                }
            };
            return instruction;
        }

        #endregion

        #region load form AssetReference

        private AsyncOperationHandle<T> GetLoadHandle<T>(AssetReferenceT<T> assetReference) where T : Object
        {
            if(_handles.TryGetValue(assetReference.AssetGUID, out var handle))
            {
                var asyncHandle = handle.Convert<T>();
                if(asyncHandle.IsValid())
                    return asyncHandle;
            }
            var newHandle = assetReference.LoadAssetAsync();
            _handles.Add(assetReference.AssetGUID, newHandle);
            return newHandle;
        }
        
        private  AsyncOperationHandle<T> GetLoadHandle<T>(AssetReference assetReference) where T : Object
        {
            if(_handles.TryGetValue(assetReference.AssetGUID, out var handle))
            {
                var asyncHandle = handle.Convert<T>();
                if(asyncHandle.IsValid())
                    return asyncHandle;
            }
            var newHandle = assetReference.LoadAssetAsync<T>();
            _handles.Add(assetReference.AssetGUID, newHandle);
            return newHandle;
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <typeparam name="T">要加载的资源类型</typeparam>
        /// <param name="assetReference">资源的引用</param>
        /// <param name="action">资源成功加载时调用的回调</param>
        public void LoadAsync<T>(AssetReferenceT<T> assetReference, Action<T> action) where T : Object
        {
            var handle = GetLoadHandle<T>(assetReference);
            if (handle.IsDone)
            {
                if(handle.Status == AsyncOperationStatus.Succeeded) 
                    action?.Invoke(handle.Result);
                else AssetLog.LogError($"Load {typeof(T).Name} Asset Fail!");
                return;
            }
            handle.Completed += operationHandle =>
            {
                if(operationHandle.Status == AsyncOperationStatus.Succeeded) 
                    action?.Invoke(operationHandle.Result);
                else AssetLog.LogError($"Load {typeof(T).Name} Asset Fail!");
            };
        }
        
        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <typeparam name="T">要加载的资源类型</typeparam>
        /// <param name="assetReference">资源的引用</param>
        /// <param name="onSuccess">资源成功加载时调用的回调</param>
        /// <param name="onFail">资源加载失败时调用的回调</param>
        public void LoadAsync<T>(AssetReference assetReference, Action<T> onSuccess, Action onFail) where T : Object
        {
            var handle = GetLoadHandle<T>(assetReference);
            if (handle.IsDone)
            {
                if(handle.Status == AsyncOperationStatus.Succeeded) 
                    onSuccess?.Invoke(handle.Result);
                else
                {
                    AssetLog.LogError($"Load {typeof(T).Name} Asset Fail!");
                    onFail?.Invoke();
                }
                return;
            }
            handle.Completed += operationHandle =>
            {
                if(operationHandle.Status == AsyncOperationStatus.Succeeded) 
                    onSuccess?.Invoke(operationHandle.Result);
                else
                {
                    AssetLog.LogError($"Load {typeof(T).Name} Asset Fail!");
                    onFail?.Invoke();
                }
            };
        }

        /// <summary>
        /// 异步加载资源并返回任务
        /// </summary>
        /// <typeparam name="T">要加载的资源类型</typeparam>
        /// <param name="assetReference">资源的引用</param>
        /// <returns>表示异步加载操作的任务</returns>
        public Task<T> LoadTask<T>(AssetReferenceT<T> assetReference) where T : Object
        {
            var handle = GetLoadHandle<T>(assetReference);
            return handle.Task;
        }

        /// <summary>
        /// 将资源加载为协程支持对象
        /// </summary>
        /// <typeparam name="T">要加载的资源类型</typeparam>
        /// <param name="assetReference">资源的引用</param>
        /// <returns>协程支持对象</returns>
        public LoaderYieldInstruction<T> LoaderYieldInstruction<T>(AssetReferenceT<T> assetReference)
            where T : Object
        {
            var handle = GetLoadHandle<T>(assetReference);
            var instruction = new LoaderYieldInstruction<T>(assetReference.AssetGUID);
            if (handle.IsDone)
            {
                instruction.SetAsset(handle.Result);
                return instruction;
            }
            handle.Completed += operationHandle =>
            {
                if (operationHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    instruction.SetAsset(operationHandle.Result);
                }
                else
                {
                    AssetLog.LogError($"Load {typeof(T).Name} Asset Fail!");
                    AddressableManager.Release(handle);
                    instruction.SetAsset(null);
                }
            };
            return instruction;
        }

        #endregion
    }
}