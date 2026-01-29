using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PowerCellStudio
{
    public class BundleAssetLoader : IAssetLoader
    {
        private long _index;
        public long index => _index;

        public string tag { get; set; }
        
        private bool _spawned = false;
        public bool spawned => _spawned;
        
        private AssetsBundleManager _assetsBundleManager;

        public BundleAssetLoader( AssetsBundleManager assetsBundleManager)
        {
            _assetsBundleManager = assetsBundleManager;
            _index = IndexGetter.instance.Get<BundleAssetLoader>();
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
            foreach (var request in _waitForLoaded)
            {
                request.Value.Dispose();
            }
            _waitForLoaded.Clear();
        }

        public bool Release(string address)
        {
            if (_waitForLoaded.TryGetValue(address, out var request))
            {
                request.Dispose();
                _waitForLoaded.Remove(address);
                return true;
            }
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

        private void OnLoadFinish<T>(T asset, string address) where T : Object
        {
            var bundleName = GetBundleName(address);
            if (_waitForLoaded.TryGetValue(address, out var handler))
            {
                _waitForLoaded.Remove(address);
                AssetUtils.ReleaseLoadHandler<T>(handler);
            }
            if(!asset)
            {
                // handler.Dispose();
                AssetLog.LogError($"Can not Find Asset, path:[{address}], bundle name:[{bundleName}]");
                return;
            }
            _cache[address] = asset;
            AddRef(bundleName);
        }

#if UNITY_EDITOR
        private T EditorSimulateLoad<T>(string address, float delay, OnLoadSuccess<T> callback, OnLoadFailed onLoadFailed = null) 
            where T : Object
        {
            if (_waitForLoaded.TryGetValue(address, out var handler))
            {
                handler.Dispose();
                _waitForLoaded.Remove(address);
            }
            T asset = null;
            // 检查address是否最后有[xxx]
            if (AssetUtils.TryGetSubAssetName(address, out var mainPath, out var subAsset))
            {
                var allAssets = AssetDatabase.LoadAllAssetsAtPath(mainPath);
                asset = allAssets.OfType<T>()
                    .FirstOrDefault(sprite => sprite.name == subAsset);
            }
            else
            {
                asset = AssetDatabase.LoadAssetAtPath<T>(address);
            }
            
            if(!asset)
            {
                AssetLog.LogError($"Can not Find Asset, path:<{address}>");
                onLoadFailed?.Invoke();
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

        public void LoadAsync<T>(string address, OnLoadSuccess<T> onSuccess, OnLoadFailed onFail = null) where T : Object
        {
#if UNITY_EDITOR
            address = AssetUtils.EditorCheckPath(address);
#endif
            if(TryGetFromCache<T>(address, out var cached))
            {
                onSuccess?.Invoke(cached);
                return;
            }
#if UNITY_EDITOR
            if (!AssetsBundleManager.simulateAssetBundleInEditor)
            {
                EditorSimulateLoad<T>(address, Time.unscaledDeltaTime * Random.Range(1,5), onSuccess, onFail); 
                return;
            }
#endif
            if (TryGetExitRequest(address, out var instruction) && instruction is LoaderYieldInstruction<T> request)
            {
                if (onSuccess != null) request.OnLoadSuccess(onSuccess);
                if (onFail != null) request.OnLoadFailed(onFail);
                return;
            }
            var bundleName = GetBundleName(address);
            var loadRequest = AssetUtils.GetLoadHandler<T>(address);
            loadRequest.OnLoadCompleted(OnLoadFinish<T>);
            if (onSuccess != null) loadRequest.OnLoadSuccess(onSuccess);
            if (onFail != null) loadRequest.OnLoadFailed(onFail);
            _waitForLoaded.Add(address, loadRequest);
            _assetsBundleManager.LoadAssetAsync<T>(bundleName, address, loadRequest);
        }

        public Task<T> LoadTask<T>(string address) where T : Object
        {
#if UNITY_EDITOR
            address = AssetUtils.EditorCheckPath(address);
#endif
            if(TryGetFromCache<T>(address, out var cached))
                return Task.FromResult(cached);
#if UNITY_EDITOR
            if (!AssetsBundleManager.simulateAssetBundleInEditor)
            {
                var asset = EditorSimulateLoad<T>(address, Time.unscaledDeltaTime * Random.Range(1,5), null); 
                return Task.FromResult(asset);
            }
#endif
            if (TryGetExitRequest(address, out var instruction) && instruction is LoaderYieldInstruction<T> request)
            {
                return request.Task;
            }
            var bundleName = GetBundleName(address);
            var loadRequest = AssetUtils.GetLoadHandler<T>(address);
            loadRequest.OnLoadCompleted(OnLoadFinish<T>);
            _waitForLoaded.Add(address, loadRequest);
            _assetsBundleManager.LoadAssetAsync<T>(bundleName, address,loadRequest);
            return loadRequest.Task;
        }
        
        public LoaderYieldInstruction<T> LoadAsYieldInstruction<T>(string address) where T : Object
        {
#if UNITY_EDITOR
            address = AssetUtils.EditorCheckPath(address);
#endif
            if(TryGetFromCache<T>(address, out var cached))
            {
                var instruction = AssetUtils.GetLoadHandler<T>(address);
                instruction.SetAsset(cached);
                return instruction;
            }
            if (TryGetExitRequest(address, out var exit) && exit is LoaderYieldInstruction<T> request)
            {
                _waitForLoaded.Remove(address);
                return request;
            }
#if UNITY_EDITOR
            if (!AssetsBundleManager.simulateAssetBundleInEditor)
            {
                var instruction = AssetUtils.GetLoadHandler<T>(address);
                EditorSimulateLoad<T>(address, Time.unscaledDeltaTime * Random.Range(1,5), (a) =>
                {
                    instruction.SetAsset(a);
                });
                return instruction;
            }
#endif
            var bundleName = GetBundleName(address);
            var loadRequest = AssetUtils.GetLoadHandler<T>(address);
            loadRequest.OnLoadCompleted(OnLoadFinish<T>);
            // _waitForLoaded.Add(address, loadRequest);
            _assetsBundleManager.LoadAssetAsync<T>(bundleName, address, loadRequest);
            return loadRequest;
        }

        public void AsyncLoadNInstantiate(string address, OnLoadSuccess<GameObject> onSuccess, OnLoadFailed onFail = null)
        {
#if UNITY_EDITOR
            address = AssetUtils.EditorCheckPath(address);
#endif
            LoadAsync<GameObject>(address, (loaded) =>
            {
                var go = GameObject.Instantiate(loaded);
                var autoClean = go.AddComponent<ABGameObjectSelfCleanup>();
                autoClean.Set(this, address);
                onSuccess?.Invoke(go);
            }, onFail);
        }

        public void AsyncLoadNInstantiate(string address, Transform parent, OnLoadSuccess<GameObject> onSuccess, OnLoadFailed onFail = null)
        {
#if UNITY_EDITOR
            address = AssetUtils.EditorCheckPath(address);
#endif
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
#if UNITY_EDITOR
            address = AssetUtils.EditorCheckPath(address);
#endif
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
            return _assetsBundleManager.LoadAsset<T>(bundleName, address);
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