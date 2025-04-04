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
        private HashSet<string> _waitForLoaded;
        private bool _disposed;
        
        public ResourceAssetLoader()
        {
            _index = IndexGetter.instance.Get<ResourceAssetLoader>();
            _assets = new Dictionary<string, Object>();
            _waitForLoaded = new HashSet<string>();
            _disposed = false;
        }

        private long _index;
        public long index => _index;
        private bool _spawned = false;
        public bool spawned => _spawned;
        public string tag { get; set; }
        
        public void Init()
        {
            if(_spawned) return;
            _spawned = true;
        }

        public void Deinit()
        {
            if(!_spawned) return;
            _disposed = true;
            foreach (var asset in _assets)
            {
                Resources.UnloadAsset(asset.Value);
            }
            _assets.Clear();
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
            return _waitForLoaded.Contains(address);
        }

        public void LoadAsync<T>(string address, Action<T> onSuccess, Action onFail = null) where T : Object
        {
            if(_disposed) return;
            // 从resources文件夹中异步加载资源
            var assetName = Path.GetFileNameWithoutExtension(address);
            var asset = Resources.LoadAsync<T>(assetName);
            _waitForLoaded.Add(address);
            asset.completed += operation =>
            {
                _waitForLoaded.Remove(address);
                if(asset.asset == null)
                {
                    onFail?.Invoke();
                    return;
                }
                var obj = asset.asset as T;
                if(obj == null)
                {
                    onFail?.Invoke();
                    return;
                }
                _assets.Add(address, asset.asset);
                if (_disposed)
                {
                    Release(address);
                    return;
                }
                onSuccess?.Invoke(asset.asset as T);
            };
        }

        public Task<T> LoadTask<T>(string address) where T : Object
        {
            if (_disposed) return null;
            var assetName = Path.GetFileNameWithoutExtension(address);
            var task = new TaskCompletionSource<T>();
            var request = Resources.LoadAsync<T>(assetName);
            _waitForLoaded.Add(address);
            request.completed += operation =>
            {
                _waitForLoaded.Remove(address);
                if(request.asset == null) return;
                var obj = request.asset as T;
                if(obj == null) return;
                _assets.Add(address, request.asset);
                if (_disposed)
                {
                    Release(address);
                    task.SetResult(null);
                }
                else task.SetResult(request.asset as T);
            };
            return task.Task;
        }

        public LoaderYieldInstruction<T> LoadAsYieldInstruction<T>(string address) where T : Object
        {
            if (_disposed) return null;
            var assetName = Path.GetFileNameWithoutExtension(address);
            var request = Resources.LoadAsync<T>(assetName);
            var instruction = new LoaderYieldInstruction<T>(assetName);
            _waitForLoaded.Add(address);
            request.completed += operation =>
            {
                _waitForLoaded.Remove(address);
                if(request.asset == null)
                {
                    instruction.SetAsset(null);
                    return;
                }
                var obj = request.asset as T;
                if(obj == null)
                {
                    instruction.SetAsset(null);
                    return;
                }
                _assets.Add(address, request.asset);
                if (_disposed)
                {
                    Release(address);
                    instruction.SetAsset(null);
                }
                else instruction.SetAsset(request.asset as T);
            };
            return instruction;
        }

        public void AsyncLoadNInstantiate(string address, Action<GameObject> onSuccess, Action onFail = null)
        {
            if(_disposed) return;
            var assetName = Path.GetFileNameWithoutExtension(address);
            var asset = Resources.LoadAsync<GameObject>(assetName);
            _waitForLoaded.Add(address);
            asset.completed += operation =>
            {
                _waitForLoaded.Remove(address);
                if(asset.asset == null)
                {
                    onFail?.Invoke();
                    return;
                }
                var obj = asset.asset as GameObject;
                if(obj == null)
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

        public void AsyncLoadNInstantiate(string address, Transform parent, Action<GameObject> onSuccess, Action onFail = null)
        {
            if(_disposed) return;
            var assetName = Path.GetFileNameWithoutExtension(address);
            var asset = Resources.LoadAsync<GameObject>(assetName);
            _waitForLoaded.Add(address);
            asset.completed += operation =>
            {
                _waitForLoaded.Remove(address);
                if(asset.asset == null)
                {
                    onFail?.Invoke();
                    return;
                }
                var obj = asset.asset as GameObject;
                if(obj == null)
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
    }
}