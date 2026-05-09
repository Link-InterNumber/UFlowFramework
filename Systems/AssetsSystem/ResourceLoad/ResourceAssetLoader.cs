using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;
using Path = System.IO.Path;

namespace PowerCellStudio
{
    public class ResourceAssetLoader : IAssetLoader
    {
        private Dictionary<string, Object> _assets;
        private Dictionary<string, ResourceRequest> _waitForLoaded;
        private bool _disposed;

        public ResourceAssetLoader()
        {
            _index = IndexGetter.instance.Get<ResourceAssetLoader>();
            _assets = new Dictionary<string, Object>();
            _waitForLoaded = new Dictionary<string, ResourceRequest>();
            _disposed = false;
        }

        private int _index;
        public int index => _index;
        private bool _spawned = false;
        public bool spawned => _spawned;
        public string tag { get; set; }

        public void Init()
        {
            if (_spawned) return;
            _disposed = false;
            _waitForLoaded.Clear();
            _spawned = true;
        }

        public void Deinit()
        {
            if (!_spawned) return;
            _disposed = true;
            foreach (var asset in _assets)
            {
                Resources.UnloadAsset(asset.Value);
            }

            _assets.Clear();
            _waitForLoaded.Clear();
            _spawned = false;
        }

        public bool Release(string address)
        {
            _assets.TryGetValue(address, out var asset);
            if (asset == null) return false;
            Resources.UnloadAsset(asset);
            _assets.Remove(address);
            return true;
        }

        public bool IsLoading(string address)
        {
            return _waitForLoaded.ContainsKey(address);
        }

        public bool IsAnyLoading()
        {
            return _waitForLoaded.Count > 0;
        }

        public void Concat(IAssetLoader other)
        {
            if (other is ResourceAssetLoader resourceLoader)
            {
                foreach (var asset in resourceLoader._assets)
                {
                    if (!_assets.ContainsKey(asset.Key))
                    {
                        _assets.Add(asset.Key, asset.Value);
                    }
                }

                foreach (var pair in resourceLoader._waitForLoaded)
                {
                    _waitForLoaded[pair.Key] = pair.Value;
                }
            }
        }

        private bool TryGetCachedAsset<T>(string address, out T asset) where T : Object
        {
            if (_assets.TryGetValue(address, out var loadedAsset) && loadedAsset is T cachedAsset && cachedAsset != null)
            {
                asset = cachedAsset;
                return true;
            }

            asset = null;
            return false;
        }

        private void RemoveLoadingRequest(string address, ResourceRequest request)
        {
            if (_waitForLoaded.TryGetValue(address, out var cachedRequest) && ReferenceEquals(cachedRequest, request))
            {
                _waitForLoaded.Remove(address);
            }
        }

        private ResourceRequest GetOrStartLoadRequest<T>(string address) where T : Object
        {
            if (_waitForLoaded.TryGetValue(address, out var request))
            {
                return request;
            }

            var assetName = Path.GetFileNameWithoutExtension(address);
            request = Resources.LoadAsync<T>(assetName);
            _waitForLoaded[address] = request;
            return request;
        }


        public void LoadAsync<T>(string address, OnLoadSuccess<T> onSuccess, OnLoadFailed onFail = null)
            where T : Object
        {
            if (_disposed) return;
            if (TryGetCachedAsset(address, out T cachedAsset))
            {
                onSuccess?.Invoke(cachedAsset);
                return;
            }

            var request = GetOrStartLoadRequest<T>(address);
            request.completed += operation =>
            {
                RemoveLoadingRequest(address, request);
                if (request.asset == null)
                {
                    onFail?.Invoke();
                    return;
                }

                var obj = request.asset as T;
                if (obj == null)
                {
                    onFail?.Invoke();
                    return;
                }

                _assets[address] = request.asset;
                if (_disposed)
                {
                    Release(address);
                    return;
                }

                onSuccess?.Invoke(obj);
            };
        }

        public Task<T> LoadTask<T>(string address) where T : Object
        {
            if (_disposed) return null;
            if (TryGetCachedAsset(address, out T cachedRuntimeAsset))
            {
                return Task.FromResult(cachedRuntimeAsset);
            }

            var task = new TaskCompletionSource<T>();
            var request = GetOrStartLoadRequest<T>(address);
            request.completed += operation =>
            {
                RemoveLoadingRequest(address, request);
                if (request.asset == null) return;
                var obj = request.asset as T;
                if (obj == null) return;
                _assets[address] = request.asset;
                if (_disposed)
                {
                    Release(address);
                    task.SetResult(null);
                }
                else task.SetResult(obj);
            };
            return task.Task;
        }

        public LoaderYieldInstruction<T> LoadAsYieldInstruction<T>(string address) where T : Object
        {
            if (_disposed) return null;
            var instruction = AssetUtils.GetLoadHandler<T>(address);
            if (TryGetCachedAsset(address, out T cachedRuntimeAsset))
            {
                instruction.SetAsset(cachedRuntimeAsset);
                return instruction;
            }

            var request = GetOrStartLoadRequest<T>(address);
            request.completed += operation =>
            {
                RemoveLoadingRequest(address, request);
                if (request.asset == null)
                {
                    instruction.SetAsset(null);
                    return;
                }

                var obj = request.asset as T;
                if (obj == null)
                {
                    instruction.SetAsset(null);
                    return;
                }

                _assets[address] = request.asset;
                if (_disposed)
                {
                    Release(address);
                    instruction.SetAsset(null);
                }
                else instruction.SetAsset(obj);
            };
            return instruction;
        }

        public void AsyncLoadNInstantiate(string address, OnLoadSuccess<GameObject> onSuccess,
            OnLoadFailed onFail = null)
        {
            if (_disposed) return;
            if (TryGetCachedAsset(address, out GameObject cachedPrefab))
            {
                var cachedGo = GameObject.Instantiate(cachedPrefab);
                _assets[address] = cachedGo;
                onSuccess?.Invoke(cachedGo);
                return;
            }

            var request = GetOrStartLoadRequest<GameObject>(address);
            request.completed += operation =>
            {
                RemoveLoadingRequest(address, request);
                if (request.asset == null)
                {
                    onFail?.Invoke();
                    return;
                }

                var obj = request.asset as GameObject;
                if (obj == null)
                {
                    onFail?.Invoke();
                    return;
                }

                var go = GameObject.Instantiate(obj);
                _assets.Add(address, go);
                if (_disposed)
                {
                    Release(address);
                }
                else
                {
                    // go.AddComponent<ABGameObjectSelfCleanup>().Set(this, address);
                    onSuccess?.Invoke(go);
                }
            };
        }

        public void AsyncLoadNInstantiate(string address, Transform parent, OnLoadSuccess<GameObject> onSuccess,
            OnLoadFailed onFail = null)
        {
            if (_disposed) return;
            if (TryGetCachedAsset(address, out GameObject cachedPrefab))
            {
                var cachedGo = GameObject.Instantiate(cachedPrefab, parent);
                cachedGo.transform.localScale = Vector3.one;
                _assets[address] = cachedGo;
                onSuccess?.Invoke(cachedGo);
                return;
            }

            var handle = GetOrStartLoadRequest<GameObject>(address);
            handle.completed += operation =>
            {
                RemoveLoadingRequest(address, handle);
                if (handle.asset == null)
                {
                    onFail?.Invoke();
                    return;
                }

                var obj = handle.asset as GameObject;
                if (obj == null)
                {
                    onFail?.Invoke();
                    return;
                }

                var go = GameObject.Instantiate(obj);
                _assets.Add(address, go);
                if (_disposed)
                {
                    Release(address);
                }
                else
                {
                    go.transform.SetParent(parent);
                    go.transform.localScale = Vector3.one;
                    // go.AddComponent<ABGameObjectSelfCleanup>().Set(this, address);
                    onSuccess?.Invoke(go);
                }
            };
        }

        public void LoadAllAsync<T>(string label, OnLoadSuccess<IList<T>> onSuccess, OnLoadFailed onFail = null) where T : Object
        {
            if (_disposed) return;
            if (TryGetCachedAsset(label, out T cachedRuntimeAsset))
            {
                onSuccess?.Invoke(new List<T> { cachedRuntimeAsset });
                return;
            }

            var request = GetOrStartLoadRequest<T>(label);
            request.completed += operation =>
            {
                RemoveLoadingRequest(label, request);
                if (request.asset == null)
                {
                    onFail?.Invoke();
                    return;
                }

                var obj = request.asset as T;
                if (obj == null)
                {
                    onFail?.Invoke();
                    return;
                }

                _assets[label] = request.asset;
                if (_disposed)
                {
                    Release(label);
                    onSuccess?.Invoke(null);
                }
                else onSuccess?.Invoke(new List<T> { obj });
            };
        }
    }
}