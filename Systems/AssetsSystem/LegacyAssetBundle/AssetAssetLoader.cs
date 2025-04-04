using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PowerCellStudio
{
    public class AssetAssetLoader : IAssetLoader
    {
        private long _index;
        public long index => _index;

        public string tag { get; set; }
        
        private bool _spawned = false;
        public bool spawned => _spawned;
        
        private AssetsBundleManager _assetsBundleManager;

        public AssetAssetLoader( AssetsBundleManager assetsBundleManager)
        {
            _assetsBundleManager = assetsBundleManager;
            _index = IndexGetter.instance.Get<AssetAssetLoader>();
        }

        #region Common
        
        private Dictionary<string, int> _refBundle = new Dictionary<string, int>();
        private Dictionary<string, Object> _cache = new Dictionary<string, Object>();
        private Dictionary<string, ILoaderYieldInstruction> _waitForLoaded = new Dictionary<string, ILoaderYieldInstruction>();

        private bool TryGetFromCache<T>(string address, out T cached) where T : Object
        {
            if(!_cache.TryGetValue(address, out var temp))
            {
                cached = null;
                return false;
            }
            cached = temp as T;
            return cached;
        }
        
        private bool TryGetExitRequest(string address, out ILoaderYieldInstruction instruction)
        {
            return _waitForLoaded.TryGetValue(address, out instruction);
        }

        private void AddRef(string bundleName)
        {
            if (_refBundle.TryGetValue(bundleName, out var current))
            {
                _refBundle[bundleName] = current + 1;
            }
            else
            {
                _refBundle[bundleName] = 1;
                _assetsBundleManager.AddRef(bundleName);
            }
        }

        public void Init()
        {
            if(_spawned) return;
            _spawned = true;
        }

        public void Deinit()
        {
            if(!_spawned) return;
            _cache.Clear();
            foreach (var s in _refBundle)
            {
                _assetsBundleManager.ReleaseBundle(s.Key);
            }
            _refBundle.Clear();
            _spawned = false;
        }

        public bool Release(string address)
        {
            _cache.Remove(address);
            var bundleName = GetBundleName(address);
            if (_refBundle.TryGetValue(bundleName, out var current))
            {
                var newValue = current - 1;
                if (newValue < 1)
                {
                    _refBundle.Remove(bundleName);
                    _assetsBundleManager.ReleaseBundle(bundleName);
                }
                else
                {
                    _refBundle[bundleName] = newValue;
                }
                return true;
            }
            return false;
        }

        public bool IsLoading(string address)
        {
            return _waitForLoaded.ContainsKey(address);
        }

#if UNITY_EDITOR
        private T EditorSimulateLoad<T>(string address, float delay, Action<T> callback) where T : Object
        {
            _waitForLoaded.Remove(address);
            var asset = AssetDatabase.LoadAssetAtPath<T>(address);
            if(!asset)
            {
                AssetLog.LogError($"Can not Find Asset, path:<{address}>");
                callback?.Invoke(null);
                return null;
            }
            var bundleName = GetBundleName(address);
            AddRef(bundleName);
            _cache[address] = asset;
            if(delay > 0)
            {
                ApplicationManager.instance.DelayedCall(delay, () =>
                {
                    callback?.Invoke(asset);
                });
            }
            else
            {
                callback?.Invoke(asset);
            }
            return asset;
        }
#endif


        public void LoadAsync<T>(string address, Action<T> onSuccess, Action onFail = null) where T : Object
        {
            if(TryGetFromCache<T>(address, out var cached))
            {
                onSuccess?.Invoke(cached);
                return;
            }
#if UNITY_EDITOR
            if (!AssetsBundleManager.simulateAssetBundleInEditor)
            {
                EditorSimulateLoad<T>(address, Time.deltaTime * Random.Range(1,5), onSuccess); 
                return;
            }
#endif
            if (TryGetExitRequest(address, out var instruction) && instruction is LoaderYieldInstruction<T> request)
            {
                request.onLoadSuccess += (a, path) =>
                {
                    if(!a)
                    {
                        onFail?.Invoke();
                        return;
                    }
                    onSuccess?.Invoke(a);
                };
                return;
            }
            var bundleName = GetBundleName(address);
            var loadRequest = new LoaderYieldInstruction<T>(address);
            loadRequest.onLoadSuccess += (a, path) =>
            {
                if(!a)
                {
                    OnLoadFail(path, onFail);
                    return;
                }
                OnLoadSuccess(path, onSuccess, a);
            };
            _waitForLoaded.Add(address, loadRequest);
            _assetsBundleManager.LoadAssetAsync<T>(bundleName, address, loadRequest);
        }

        private void OnLoadFail(string address, Action onFail)
        {
            _waitForLoaded.Remove(address);
            AssetLog.LogError($"Can not Find Asset, path:<{address}>");
            onFail?.Invoke();
        }
        
        private void OnLoadSuccess<T>(string address, Action<T> onSuccess, T asset) 
            where T : Object
        {
            _waitForLoaded.Remove(address);
            _cache[address] = asset;
            onSuccess?.Invoke(asset);
            var bundleName = GetBundleName(address);
            AddRef(bundleName);
        }

        public Task<T> LoadTask<T>(string address) where T : Object
        {
            if(TryGetFromCache<T>(address, out var cached))
                return Task.FromResult(cached);
#if UNITY_EDITOR
            if (!AssetsBundleManager.simulateAssetBundleInEditor)
            {
                var asset = EditorSimulateLoad<T>(address, Time.deltaTime * Random.Range(1,5), null); 
                return Task.FromResult(asset);
            }
#endif
            if (TryGetExitRequest(address, out var instruction) && instruction is LoaderYieldInstruction<T> request)
            {
                return request.Task;
            }
            var bundleName = GetBundleName(address);
            var loadRequest = new LoaderYieldInstruction<T>(address);
            loadRequest.onLoadSuccess += OnLoadSuccess<T>;
            _waitForLoaded.Add(address, loadRequest);
            _assetsBundleManager.LoadAssetAsync<T>(bundleName, address,loadRequest);
            return loadRequest.Task;
        }
        
        public LoaderYieldInstruction<T> LoadAsYieldInstruction<T>(string address) where T : Object
        {
            if(TryGetFromCache<T>(address, out var cached))
            {
                var instruction = new LoaderYieldInstruction<T>(address);
                instruction.SetAsset(cached);
                return instruction;
            }
            if (TryGetExitRequest(address, out var exit) && exit is LoaderYieldInstruction<T> request)
            {
                return request;
            }
#if UNITY_EDITOR
            if (!AssetsBundleManager.simulateAssetBundleInEditor)
            {
                var instruction = new LoaderYieldInstruction<T>(address);
                EditorSimulateLoad<T>(address, Time.deltaTime * Random.Range(1,5), (a) =>
                {
                    instruction.SetAsset(a);
                });
                return instruction;
            }
#endif
            var bundleName = GetBundleName(address);
            var loadRequest = new LoaderYieldInstruction<T>(address);
            loadRequest.onLoadSuccess += OnLoadSuccess<T>;
            _waitForLoaded.Add(address, loadRequest);
            _assetsBundleManager.LoadAssetAsync<T>(bundleName, address, loadRequest);
            return loadRequest;
        }

        private void OnLoadSuccess<T>(T asset, string address) where T : Object
        {
            if(!asset)
            {
                OnLoadFail(address, null);
                return;
            }
            OnLoadSuccess(address, null, asset);
        }

        public void AsyncLoadNInstantiate(string address, Action<GameObject> onSuccess, Action onFail = null)
        {
            LoadAsync<GameObject>(address, (loaded) =>
            {
                var go = GameObject.Instantiate(loaded);
                var autoClean = go.AddComponent<ABGameObjectSelfCleanup>();
                autoClean.Set(this, address);
                onSuccess?.Invoke(go);
            }, onFail);
        }

        public void AsyncLoadNInstantiate(string address, Transform parent, Action<GameObject> onSuccess, Action onFail = null)
        {
            LoadAsync<GameObject>(address, (loaded) =>
            {
                var go = GameObject.Instantiate(loaded);
                go.transform.SetParent(parent);
                go.transform.localScale = Vector3.one;
                var autoClean = go.AddComponent<ABGameObjectSelfCleanup>();
                autoClean.Set(this, address);
                onSuccess?.Invoke(go);
            }, onFail);
        }

#if !UNITY_WEBGL
        public T LoadImmediately<T>(string address) where T : Object
        {
            if(TryGetFromCache<T>(address, out var cached))
                return cached;
#if UNITY_EDITOR
            if (!AssetsBundleManager.simulateAssetBundleInEditor)
            {
                var asset = EditorSimulateLoad<T>(address, 0f, null);
                return asset;
            }
#endif
            var bundleName = GetBundleName(address);
            return _assetsBundleManager.LoadAsset<T>(bundleName, address).asset;
        }
#endif

        public string GetBundleName(string address)
        {
#if UNITY_EDITOR
            if (!AssetsBundleManager.simulateAssetBundleInEditor)
                return string.IsNullOrEmpty(address) ? address : "Unity_Editor";
#endif
            return string.IsNullOrEmpty(address) ? address : _assetsBundleManager.GetBundleNameByAsset(address);
        }

        #endregion
    }
}