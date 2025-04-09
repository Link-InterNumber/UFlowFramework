using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public sealed class PoolGroup
    {
        private Dictionary<string, GameObjectPool> _gameObjectPools = new Dictionary<string, GameObjectPool>();
        private Dictionary<string, string> _gameObjectTagMap = new Dictionary<string, string>();
        private HashSet<string> _onLoading = new HashSet<string>();
        private Dictionary<Type, PoolableObjectPool> _pools = new Dictionary<Type, PoolableObjectPool>();
        private Transform _root;
        public bool _autoDestroy = true;
        public bool autoDestroy
        {
            get { return _autoDestroy; }
            set
            {
                if(_autoDestroy == value) return;
                _autoDestroy = value;
                foreach (var (_, gameObjectPool) in _gameObjectPools)
                {
                    gameObjectPool.autoDestroy = value;
                }
            }
        }

        public Transform root => _root;

        public PoolGroup(Transform rootParent, string rootName)
        {
            _root = new GameObject(rootName).transform;
            _root.SetParent(rootParent);
            _root.localScale = Vector3.one;
            _root.localPosition = Vector3.zero;
        }

        public PoolableObjectPool GetPool<T>()
        {
            var key = typeof(T);
            if (_pools.ContainsKey(key)) return _pools[key];
            return null;
        }

        public GameObjectPool GetPool(string path)
        {
            return _gameObjectPools.TryGetValue(path, out var pool) ? pool : null;
        }

        public PoolableObjectPool Push<T>(Func<T> create, int maxNum, int initNum) where T : class, IPoolable
        {
            var key = typeof(T);
            if (_pools.ContainsKey(key)) return _pools[key];
            var newPool = new PoolableObjectPool(create, maxNum, initNum);
            _pools[key] = newPool;
            return newPool;
        }
        
        public bool HasPool<T>() where T : class, IPoolable
        {
            return _pools.ContainsKey(typeof(T));
        }
        
        public T Get<T>() where T : class, IPoolable
        {
            if(_pools.TryGetValue(typeof(T), out var pool))
            {
                return pool.Get() as T;
            }
            return null;
        }
        
        public T GetOrPush<T>() where T : class, IPoolable, new()
        {
            if(_pools.TryGetValue(typeof(T), out var pool))
            {
                return pool.Get() as T;
            }
            return Push<T>(()=>new T(), 10, 5).Get() as T;
        }

        public bool Release<T>(T obj) where T : class, IPoolable
        {
            if (_pools.TryGetValue(typeof(T), out var pool))
            {
                return pool.Release(obj);
            }
            if (autoDestroy) obj.Dispose();
            return false;
        }

        public IEnumerator PushGameObjectPool(string path, int maxNum, int initNum, Action callBack)
        {
            if (_gameObjectPools.ContainsKey(path))
            {
                var existPool = _gameObjectPools[path];
                if (existPool.loadStatus == AssetLoadStatus.Loaded)
                {
                    callBack?.Invoke();
                    yield break;
                }
            }
            if (_onLoading.Contains(path))
            {
                while (_onLoading.Contains(path))
                {
                    yield return null;
                }
                callBack?.Invoke();
                yield break;
            }
            var pool = new GameObjectPool(path, maxNum, initNum, _root);
            pool.autoDestroy = autoDestroy;
            _onLoading.Add(path);
            yield return pool.WaitForInitAsYieldInstruction();
            _onLoading.Remove(path);
            if (pool.loadStatus != AssetLoadStatus.Loaded)
            {
                yield break;
            }
            _gameObjectPools[path] = pool;
            _gameObjectTagMap[pool.tag] = path;
            callBack?.Invoke();
        }
        
        public GameObjectPool GetGameObjectPool(string path)
        {
            if (_gameObjectPools.TryGetValue(path, out var pool))
            {
                return pool;
            }
            return null;
        }

        public GameObject GetGameObject(string path)
        {
            if(_gameObjectPools.TryGetValue(path, out var pool))
            {
                return pool.Get();
            }
            return null;
        }

        public void GetGameObjectAsync(string path, Action<GameObject> callBack)
        {
            var go = GetGameObject(path);
            if (go != null)
            {
                callBack(go);
                return;
            }
            ApplicationManager.instance.StartCoroutine(GetGameObjectAsyncHandler(path, callBack));
        }

        private IEnumerator GetGameObjectAsyncHandler(string path, Action<GameObject> callBack)
        {
            yield return PushGameObjectPool(path, 10, 1, null);
            var go = GetGameObject(path);
            if(go) callBack(go);
        }

        public bool ReleaseGameObject(GameObject go)
        {
            var tag = go.name.Split('^')[0];
            if (_gameObjectTagMap.TryGetValue(tag, out var path))
            {
                if (_gameObjectPools.TryGetValue(path, out var pool))
                {
                    pool.ReleaseWithoutCheck(go);
                    return true;
                }
            }
            if(autoDestroy) GameObject.Destroy(go);
            return false;
        }

        public void Clear<T>() where T : class, IPoolable
        {
            var key = typeof(T);
            if (!_pools.TryGetValue(key, out var pool)) return;
            pool.Clear();
        }
        
        public void ClearGameObjectPool(string path)
        {
            if (!_gameObjectPools.TryGetValue(path, out var pool)) return;
            // _gameObjectTagMap.Remove(pool.tag);
            pool.Clear();
        }
        
        public void Dispose<T>() where T : class, IPoolable
        {
            var key = typeof(T);
            if (!_pools.TryGetValue(key, out var pool)) return;
            _pools.Remove(key);
            pool.Dispose();
        }
        
        public void DisposeGameObjectPool(string path)
        {
            if (!_gameObjectPools.TryGetValue(path, out var pool)) return;
            _gameObjectTagMap.Remove(pool.tag);
            _gameObjectPools.Remove(path);
            pool.Dispose();
        }

        public void ClearAll()
        {
            foreach (var (_, pool) in _gameObjectPools)
            {
                pool.Clear();
            }
            foreach (var (_, pool) in _pools)
            {
                pool.Clear();
            }
        }

        public void ForceClear()
        {
            ClearAll();
            ReplaceNewRoot();
        }

        private void ReplaceNewRoot()
        {
            var rootParent = _root.parent;
            var rootName = _root.name;
            GameObject.Destroy(_root.gameObject);
            _root.SetParent(rootParent);
            _root.localScale = Vector3.one;
            _root = new GameObject(rootName).transform;
        }

        public void Dispose()
        {
            foreach (var (_, pool) in _gameObjectPools)
            {
                pool.Dispose();
            }
            _gameObjectPools.Clear();
            _gameObjectTagMap.Clear();
            foreach (var (_, pool) in _pools)
            {
                pool.Dispose();
            }
            _pools.Clear();
            // ReplaceNewRoot();
            GameObject.Destroy(_root.gameObject);
            _root = null;
        }
    }
}