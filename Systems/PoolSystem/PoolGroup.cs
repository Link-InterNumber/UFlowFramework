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
        private bool _autoDestroy = true;
        
        /// <summary>
        /// 自动销毁属性，定义对象池是否自动销毁对象
        /// AutoDestroy property defines whether the pool should automatically destroy objects.
        /// </summary>
        public bool autoDestroy
        {
            get { return _autoDestroy; }
            set
            {
                if (_autoDestroy == value) return;
                _autoDestroy = value;
                foreach (var (_, gameObjectPool) in _gameObjectPools)
                {
                    gameObjectPool.autoDestroy = value;
                }
            }
        }

        /// <summary>
        /// 根Transform，此Transform作为所有池对象的父对象
        /// Root Transform, used as the parent for all pool objects.
        /// </summary>
        public Transform root => _root;

        /// <summary>
        /// 构造一个新的对象池组
        /// Construct a new PoolGroup.
        /// </summary>
        /// <param name="rootParent">池组的父Transform / Parent Transform for the pool group</param>
        /// <param name="rootName">池组的根名称 / Root name for the pool group</param>
        public PoolGroup(Transform rootParent, string rootName)
        {
            _root = new GameObject(rootName).transform;
            _root.SetParent(rootParent);
            _root.localScale = Vector3.one;
            _root.localPosition = Vector3.zero;
        }

        /// <summary>
        /// 获取指定类型的池对象
        /// Get the pool for a specified type.
        /// </summary>
        /// <typeparam name="T">池对象类型 / Type of object in the pool</typeparam>
        /// <returns>池对象 / Pool of the specified type</returns>
        public PoolableObjectPool GetPool<T>()
        {
            var key = typeof(T);
            if (_pools.ContainsKey(key)) return _pools[key];
            return null;
        }

        /// <summary>
        /// 获取指定路径的GameObject池对象
        /// Get the GameObject pool for a specified path.
        /// </summary>
        /// <param name="path">对象路径 / Path for the object</param>
        /// <returns>GameObject池对象 / GameObject pool</returns>
        public GameObjectPool GetPool(string path)
        {
            return _gameObjectPools.TryGetValue(path, out var pool) ? pool : null;
        }

        /// <summary>
        /// 创建并添加一个新的池对象
        /// Create and add a new pool.
        /// </summary>
        /// <typeparam name="T">池对象类型 / Type of object in the pool</typeparam>
        /// <param name="create">对象创建方法 / Method for creating the object</param>
        /// <param name="maxNum">池最大数量 / Maximum number of objects in the pool</param>
        /// <param name="initNum">池初始化数量 / Initial number of objects in the pool</param>
        /// <returns>创建的池对象 / Created pool</returns>
        public PoolableObjectPool Push<T>(Func<T> create, int maxNum, int initNum) where T : class, IPoolable
        {
            var key = typeof(T);
            if (_pools.ContainsKey(key)) return _pools[key];
            var newPool = new PoolableObjectPool(create, maxNum, initNum);
            _pools[key] = newPool;
            return newPool;
        }

        /// <summary>
        /// 检查是否存在指定类型的池对象
        /// Check if a pool exists for a specified type.
        /// </summary>
        /// <typeparam name="T">池对象类型 / Type of object in the pool</typeparam>
        /// <returns>是否存在池对象 / Whether the pool exists</returns>
        public bool HasPool<T>() where T : class, IPoolable
        {
            return _pools.ContainsKey(typeof(T));
        }

        /// <summary>
        /// 获取指定类型的池对象实例
        /// Get an instance from the pool of a specified type.
        /// </summary>
        /// <typeparam name="T">池对象类型 / Type of object in the pool</typeparam>
        /// <returns>对象实例 / Instance of the object</returns>
        public T Get<T>() where T : class, IPoolable
        {
            if (_pools.TryGetValue(typeof(T), out var pool))
            {
                return pool.Get() as T;
            }
            return null;
        }

        /// <summary>
        /// 获取或创建并添加一个指定类型的池对象
        /// Get or create and add a pool for a specified type.
        /// </summary>
        /// <typeparam name="T">池对象类型 / Type of object in the pool</typeparam>
        /// <returns>对象实例 / Instance of the object</returns>
        public T GetOrPush<T>() where T : class, IPoolable, new()
        {
            if (_pools.TryGetValue(typeof(T), out var pool))
            {
                return pool.Get() as T;
            }
            return Push<T>(() => new T(), 10, 5).Get() as T;
        }

        /// <summary>
        /// 将对象释放回池中
        /// Release an object back to the pool.
        /// </summary>
        /// <typeparam name="T">池对象类型 / Type of object in the pool</typeparam>
        /// <param name="obj">要释放的对象 / Object to release</param>
        /// <returns>是否成功释放 / Whether the release was successful</returns>
        public bool Release<T>(T obj) where T : class, IPoolable
        {
            if (_pools.TryGetValue(typeof(T), out var pool))
            {
                return pool.Release(obj);
            }
            if (autoDestroy) obj.Dispose();
            return false;
        }

        /// <summary>
        /// 异步创建并添加GameObject池
        /// Asynchronously create and add a GameObject pool.
        /// </summary>
        /// <param name="path">对象路径 / Path for the object</param>
        /// <param name="maxNum">池最大数量 / Maximum number of objects in the pool</param>
        /// <param name="initNum">池初始化数量 / Initial number of objects in the pool</param>
        /// <param name="callBack">初始化完成后的回调 / Callback after initialization</param>
        /// <returns>异步方法 / IEnumerator for the asynchronous method</returns>
        public IEnumerator PushGameObjectPool(string path, int maxNum, int initNum, Action callBack)
        {
            if (_gameObjectPools.TryGetValue(path, out var existPool))
            {
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

        /// <summary>
        /// 获取指定路径的GameObject池
        /// Get the GameObject pool for a specified path.
        /// </summary>
        /// <param name="path">对象路径 / Path for the object</param>
        /// <returns>GameObject池对象 / GameObject pool</returns>
        public GameObjectPool GetGameObjectPool(string path)
        {
            if (_gameObjectPools.TryGetValue(path, out var pool))
            {
                return pool;
            }
            return null;
        }

        /// <summary>
        /// 获取指定路径的GameObject
        /// Get a GameObject for a specified path.
        /// </summary>
        /// <param name="path">对象路径 / Path for the object</param>
        /// <returns>GameObject实例 / Instance of the GameObject</returns>
        public GameObject GetGameObject(string path)
        {
            if (_gameObjectPools.TryGetValue(path, out var pool))
            {
                return pool.Get();
            }
            return null;
        }

        /// <summary>
        /// 异步获取GameObject
        /// Asynchronously get a GameObject.
        /// </summary>
        /// <param name="path">对象路径 / Path for the object</param>
        /// <param name="callBack">获取后的回调 / Callback after getting the object</param>
        public void GetGameObjectAsync(string path, Action<GameObject> callBack)
        {
            var go = GetGameObject(path);
            if (go != null)
            {
                callBack(go);
                return;
            }
            AsyncManager.Run(GetGameObjectAsyncHandler(path, callBack));
        }

        /// <summary>
        /// 异步处理程序：获取GameObject
        /// Asynchronous handler for getting a GameObject.
        /// </summary>
        /// <param name="path">对象路径 / Path for the object</param>
        /// <param name="callBack">获取后的回调 / Callback after getting the object</param>
        private IEnumerator GetGameObjectAsyncHandler(string path, Action<GameObject> callBack)
        {
            yield return PushGameObjectPool(path, 10, 1, null);
            var go = GetGameObject(path);
            if (go) callBack(go);
        }

        /// <summary>
        /// 释放GameObject
        /// Release a GameObject.
        /// </summary>
        /// <param name="go">要释放的GameObject / GameObject to release</param>
        /// <returns>是否成功释放 / Whether the release was successful</returns>
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
            if (autoDestroy) GameObject.Destroy(go);
            return false;
        }

        /// <summary>
        /// 清空指定类型的池对象
        /// Clear a pool for a specified type.
        /// </summary>
        /// <typeparam name="T">池对象类型 / Type of object in the pool</typeparam>
        public void Clear<T>() where T : class, IPoolable
        {
            var key = typeof(T);
            if (!_pools.TryGetValue(key, out var pool)) return;
            pool.Clear();
        }

        /// <summary>
        /// 清空指定路径的GameObject池
        /// Clear the GameObject pool for a specified path.
        /// </summary>
        /// <param name="path">对象路径 / Path for the object</param>
        public void ClearGameObjectPool(string path)
        {
            if (!_gameObjectPools.TryGetValue(path, out var pool)) return;
            pool.Clear();
        }

        /// <summary>
        /// 释放指定类型的池对象
        /// Dispose a pool for a specified type.
        /// </summary>
        /// <typeparam name="T">池对象类型 / Type of object in the pool</typeparam>
        public void Dispose<T>() where T : class, IPoolable
        {
            var key = typeof(T);
            if (!_pools.TryGetValue(key, out var pool)) return;
            _pools.Remove(key);
            pool.Dispose();
        }

        /// <summary>
        /// 释放指定路径的GameObject池
        /// Dispose the GameObject pool for a specified path.
        /// </summary>
        /// <param name="path">对象路径 / Path for the object</param>
        public void DisposeGameObjectPool(string path)
        {
            if (!_gameObjectPools.TryGetValue(path, out var pool)) return;
            _gameObjectTagMap.Remove(pool.tag);
            _gameObjectPools.Remove(path);
            pool.Dispose();
        }

        /// <summary>
        /// 清除所有池对象
        /// Clear all pool objects.
        /// </summary>
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

        /// <summary>
        /// 强制清除所有池对象
        /// Forcefully clear all pool objects.
        /// </summary>
        public void ForceClear()
        {
            foreach (var (_, pool) in _gameObjectPools)
            {
                pool.ClearStack();
            }
            foreach (var (_, pool) in _pools)
            {
                pool.Clear();
            }
            ReplaceNewRoot();
        }

        /// <summary>
        /// 替换新的根对象
        /// Replace the root with a new object.
        /// </summary>
        private void ReplaceNewRoot()
        {
            if (!_root) return;
            var rootParent = _root.parent;
            var rootName = _root.name;
            GameObject.Destroy(_root.gameObject);
            _root = new GameObject(rootName).transform;
            _root.SetParent(rootParent);
            _root.localScale = Vector3.one;
            foreach (var (_, pool) in _gameObjectPools)
            {
                pool.ChangeRoot(_root);
            }
        }

        /// <summary>
        /// 释放池组
        /// Dispose of the pool group.
        /// </summary>
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
            GameObject.Destroy(_root.gameObject);
            _root = null;
        }
    }
}