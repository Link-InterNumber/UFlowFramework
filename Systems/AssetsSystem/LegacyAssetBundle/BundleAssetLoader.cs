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
        private int _index;
        public int index => _index;

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
        
        private Dictionary<string, int> _refCount = new Dictionary<string, int>();

        private void AddRef(string assetPath)
        {
            if (_refCount.TryGetValue(assetPath, out var current))
            {
                _refCount[assetPath] = current + 1;
            }
            else
            {
                _refCount[assetPath] = 1;
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
            foreach (var (assetPath, refCount) in _refCount)
            {
                _assetsBundleManager.DelAssetRef(assetPath, refCount);
            }
            _refCount.Clear();
            _spawned = false;
        }

        public bool Release(string address)
        {
            if (_refCount.TryGetValue(address, out var current))
            {
                var newValue = current - 1;
                if (newValue < 1)
                {
                    _refCount.Remove(address);
                    _assetsBundleManager.DelAssetRef(address);
                }
                else
                {
                    _refCount[address] = newValue;
                }
                return true;
            }
            return false;
        }

        public bool IsLoading(string address)
        {
            return _assetsBundleManager.IsAssetLoading(address);
        }

        public bool IsAnyLoading()
        {
            foreach (var kvp in _refCount)
            {
                if (_assetsBundleManager.IsAssetLoading(kvp.Key))
                {
                    return true;
                }
            }
            return false;
        }

        public void Merge(IAssetLoader other)
        {
            if (other == null || other.index == this.index || !other.spawned) return;
            if (other.IsAnyLoading())
            {
                AssetLogger.LogError($"Trying to merge loader {other.index} into {this.index} while it still has loading assets. This may cause unexpected behavior.");
                return;
            }
            if (other is BundleAssetLoader otherLoader)
            {
                foreach (var kvp in otherLoader._refCount)
                {
                    if (!_refCount.ContainsKey(kvp.Key))
                    {
                        _refCount.Add(kvp.Key, kvp.Value);
                    }
                    else
                    {
                        _refCount[kvp.Key] += kvp.Value;
                    }
                }
                otherLoader._refCount.Clear();
            }
        }

        private void OnLoadFinish<T>(T asset, string address) where T : Object
        {
            if(!asset)
            {
                AssetLogger.LogError($"Can not Find Asset, path:[{address}]");
                return;
            }
            AddRef(address);
        }

        #endregion

#if UNITY_EDITOR
        private T EditorSimulateLoad<T>(string address, float delay, OnLoadSuccess<T> callback, OnLoadFailed onLoadFailed = null) 
            where T : Object
        {
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
                AssetLogger.LogError($"Can not Find Asset, path:<{address}>");
                onLoadFailed?.Invoke();
                return null;
            }
            AddRef(address);
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
            if (!AssetsBundleManager.simulateAssetBundleInEditor)
            {
                EditorSimulateLoad<T>(address, Time.unscaledDeltaTime * Random.Range(1,5), onSuccess, onFail); 
                return;
            }
#endif
            var loadRequest = AssetUtils.GetLoadHandler<T>(address, true);
            loadRequest.OnLoadCompleted(OnLoadFinish<T>);
            if (onSuccess != null) loadRequest.OnLoadSuccess(onSuccess);
            if (onFail != null) loadRequest.OnLoadFailed(onFail);
            _assetsBundleManager.LoadAssetAsync<T>(address, loadRequest);
        }

        public Task<T> LoadTask<T>(string address) where T : Object
        {
#if UNITY_EDITOR
            address = AssetUtils.EditorCheckPath(address);
            if (!AssetsBundleManager.simulateAssetBundleInEditor)
            {
                var asset = EditorSimulateLoad<T>(address, Time.unscaledDeltaTime * Random.Range(1,5), null); 
                return Task.FromResult(asset);
            }
#endif
            var loadRequest = AssetUtils.GetLoadHandler<T>(address, true);
            loadRequest.OnLoadCompleted(OnLoadFinish<T>);
            _assetsBundleManager.LoadAssetAsync<T>(address, loadRequest);
            return loadRequest.Task;
        }
        
        public LoaderYieldInstruction<T> LoadAsYieldInstruction<T>(string address) where T : Object
        {
#if UNITY_EDITOR
            address = AssetUtils.EditorCheckPath(address);
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
            var loadRequest = AssetUtils.GetLoadHandler<T>(address);
            loadRequest.OnLoadCompleted(OnLoadFinish<T>);
            _assetsBundleManager.LoadAssetAsync<T>(address, loadRequest);
            return loadRequest;
        }

        public void AsyncLoadNInstantiate(string address, OnLoadSuccess<GameObject> onSuccess, OnLoadFailed onFail = null)
        {
#if UNITY_EDITOR
            address = AssetUtils.EditorCheckPath(address);
            if (!AssetsBundleManager.simulateAssetBundleInEditor)
            {
                EditorSimulateLoad<GameObject>(address, Time.unscaledDeltaTime * Random.Range(1,5), (loaded) =>
                {
                    var go = GameObject.Instantiate(loaded);
                    var autoClean = go.AddComponent<ABGameObjectSelfCleanup>();
                    autoClean.Set(_assetsBundleManager, address);
                    onSuccess?.Invoke(go);
                }, onFail);
                return;
            }
#endif
            var loadRequest = AssetUtils.GetLoadHandler<GameObject>(address, true);
            _assetsBundleManager.LoadAssetAsync<GameObject>(address, loadRequest);
            loadRequest.OnLoadSuccess((loaded) =>
            {
                var go = GameObject.Instantiate(loaded);
                var autoClean = go.AddComponent<ABGameObjectSelfCleanup>();
                autoClean.Set(_assetsBundleManager, address);
                onSuccess?.Invoke(go);
            });
            loadRequest.OnLoadFailed(() =>
            {
                onFail?.Invoke();
            });
        }

        public void AsyncLoadNInstantiate(string address, Transform parent, OnLoadSuccess<GameObject> onSuccess, OnLoadFailed onFail = null)
        {
#if UNITY_EDITOR
            address = AssetUtils.EditorCheckPath(address);
            if (!AssetsBundleManager.simulateAssetBundleInEditor)
            {
                EditorSimulateLoad<GameObject>(address, Time.unscaledDeltaTime * Random.Range(1,5), (loaded) =>
                {
                    var go = GameObject.Instantiate(loaded, parent);
                    var autoClean = go.AddComponent<ABGameObjectSelfCleanup>();
                    autoClean.Set(_assetsBundleManager, address);
                    onSuccess?.Invoke(go);
                }, onFail);
                return;
            }
#endif
            var loadRequest = AssetUtils.GetLoadHandler<GameObject>(address, true);
            _assetsBundleManager.LoadAssetAsync<GameObject>(address, loadRequest);
            loadRequest.OnLoadSuccess((loaded) =>
            {
                var go = GameObject.Instantiate(loaded, parent);
                var autoClean = go.AddComponent<ABGameObjectSelfCleanup>();
                autoClean.Set(_assetsBundleManager, address);
                onSuccess?.Invoke(go);
            });
            loadRequest.OnLoadFailed(() =>
            {
                onFail?.Invoke();
            });
        }

#if !UNITY_WEBGL
        public T LoadImmediately<T>(string address) where T : Object
        {
#if UNITY_EDITOR
            address = AssetUtils.EditorCheckPath(address);
            if (!AssetsBundleManager.simulateAssetBundleInEditor)
            {
                var asset = EditorSimulateLoad<T>(address, 0f, null);
                return asset;
            }
#endif
            return _assetsBundleManager.LoadAsset<T>(address);
        }
#endif
        
        public void LoadAllAsync<T>(string label, OnLoadSuccess<IList<T>> onSuccess, OnLoadFailed onFail = null) where T : Object
        {
            // 获取bundle名为label的中间类型为T的所有资源
#if UNITY_EDITOR
            if (!AssetsBundleManager.simulateAssetBundleInEditor)
            {
                List<T> assets = new List<T>();
                string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(label);
                if (assetPaths == null || assetPaths.Length == 0)
                {
                    AssetLogger.LogError($"No assets found in bundle with label {label}");
                    onFail?.Invoke();
                    return;
                }
                // 遍历所有路径
                foreach (string path in assetPaths)
                {
                    // 加载资源
                    T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                    if (asset != null)
                    {
                        AddRef(path);
                        assets.Add(asset);
                    }
                }
                var delay = Time.unscaledDeltaTime * Random.Range(1,5);
                ApplicationManager.instance.DelayedCall(delay, () =>
                {
                    onSuccess?.Invoke(assets);
                });
                return;
            }
#endif
            _assetsBundleManager.LoadAssetsFromBundleAsync(label, onSuccess, onFail);
        }

    }
}