using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    [ModuleInitOrder(1)]
    public class PoolManager : SingletonBase<PoolManager>, IOnGameStartModule
    {
        public enum PoolGroupName
        {
            Default = 0,
            UI,
            Battle,
            Main,
            Effect
        }

        private List<PoolGroup> _groupRoot;

        private Transform _transform;

        /// <summary>
        /// 获取对象池管理器的Transform。
        /// Retrieve the Transform of the PoolManager.
        /// </summary>
        public Transform transform => _transform;
        
        private bool _inited = false;

        /// <summary>
        /// 初始化对象池管理器，防止重复初始化。
        /// Initialize the PoolManager, preventing multiple initializations.
        /// </summary>
        public void OnInit()
        {
            if (_inited) return;
            _inited = true;
            var gameObject = new GameObject("PoolManager");
            _transform = gameObject.transform;
            GameObject.DontDestroyOnLoad(gameObject);
            _groupRoot = new List<PoolGroup>();
            var PoolGroupNames = Enum.GetNames(typeof(PoolGroupName));
            for (int i = 0; i < PoolGroupNames.Length; i++)
            {
                _groupRoot.Add(new PoolGroup(_transform, PoolGroupNames[i]));
            }
            EventManager.instance.onClearUnusedAsset.AddListener(ClearAllPool);
        }

        /// <summary>
        /// 反初始化并清理对象池管理器。
        /// Deinitialize and clear the PoolManager.
        /// </summary>
        public void Deinit()
        {
            ClearAllPool();
            if (!_transform) return;
            GameObject.Destroy(_transform);
            _transform = null;
            EventManager.instance.onClearUnusedAsset.RemoveListener(ClearAllPool);
        }

        /// <summary>
        /// 游戏开始时模块初始化。
        /// Log module initialization upon game start.
        /// </summary>
        public void OnGameStart()
        {
            ModuleLog.Log<PoolManager>("Module Init!");
        }
        
        #region IPoolable

        /// <summary>
        /// 获取指定组内的对象池。
        /// Get the object pool within the specified group.
        /// </summary>
        /// <typeparam name="T">对象类型 / Type of object</typeparam>
        /// <param name="groupName">组 / Pool group name</param>
        /// <returns>对象池 / The object pool</returns>
        public PoolableObjectPool GetPool<T>(PoolGroupName groupName)
        {
            return GetGroup(groupName).GetPool<T>();
        }

        /// <summary>
        /// 获取指定路径的GameObjectPool对象。
        /// Get the GameObject pool for the specified path.
        /// </summary>
        /// <param name="groupName">对象池组名 / Pool group name</param>
        /// <param name="path">预制体路径 / Prefab path</param>
        /// <returns>GameObject池对象 / GameObject pool</returns>
        public GameObjectPool GetPool(PoolGroupName groupName, string path)
        {
            return GetGroup(groupName).GetPool(path);
        }

        /// <summary>
        /// 注册一个对象池，或获得已有的对象池。
        /// Register or retrieve an existing object pool.
        /// </summary>
        /// <param name="createFun">构造方法 / Constructor method</param>
        /// <param name="maxNum">对象池存放的最大数量 / Maximum number of objects in the pool</param>
        /// <param name="initNum">创建对象池时预先生成对象数量 / Initial number of objects created</param>
        /// <param name="groupName">组 / Group name</param>
        /// <typeparam name="T">对象类型 / Type of objects</typeparam>
        /// <returns>对象池 / The object pool</returns>
        public PoolableObjectPool Register<T>(Func<T> createFun, int maxNum, int initNum, PoolGroupName groupName = PoolGroupName.Default)
            where T : class, IPoolable
        {
            return GetGroup(groupName).Push<T>(createFun, maxNum, initNum);
        }
        
        /// <summary>
        /// 注销一个对象池。
        /// Unregister an object pool. All objects will be destroyed.
        /// </summary>
        /// <param name="groupName">组 / Group name</param>
        /// <typeparam name="T">对象类型 / Type of objects</typeparam>
        public void UnRegister<T>(PoolGroupName groupName = PoolGroupName.Default)
            where T : class, IPoolable
        {
            GetGroup(groupName).Dispose<T>();
        }

        /// <summary>
        /// 从对象池获取对象，没有注册的对象类型将会返回null。
        /// Get an object from the pool; returns null if the type is unregistered.
        /// </summary>
        /// <param name="groupName">组 / Group name</param>
        /// <typeparam name="T">对象类型 / Type of object</typeparam>
        /// <returns>对象实例 / Instance of the object</returns>
        public T Get<T>(PoolGroupName groupName = PoolGroupName.Default)
            where T : class, IPoolable
        {
            var obj = GetGroup(groupName).Get<T>();
            if (obj == null) ModuleLog.LogError<PoolManager>($"{typeof(T).Name} is null, {typeof(T).Name} was unregistered, groupName: " + groupName);
            return obj;
        }
        
        /// <summary>
        /// 从对象池获取对象，没有注册的对象类型将会进行注册。
        /// Get an object from the pool; register it if the type is unregistered.
        /// </summary>
        /// <param name="groupName">组 / Group name</param>
        /// <typeparam name="T">有new()的对象类型 / Type of object with new()</typeparam>
        /// <returns>对象实例 / Instance of the object</returns>
        public T GetOrNew<T>(PoolGroupName groupName = PoolGroupName.Default)
            where T : class, IPoolable, new()
        {
            return GetGroup(groupName).GetOrPush<T>();
        }

        /// <summary>
        /// 回收一个对象到对象池。
        /// Release an object back to the pool.
        /// </summary>
        /// <param name="item">对象 / Object</param>
        /// <param name="groupName">组 / Group name</param>
        /// <typeparam name="T">对象类型 / Type of object</typeparam>
        /// <returns>是否成功回收 / Whether the release was successful</returns>
        public bool Release<T>(T item, PoolGroupName groupName = PoolGroupName.Default)
            where T : class, IPoolable
        {
            return GetGroup(groupName).Release(item);
        }

        #endregion

        #region GameObject
        
        /// <summary>
        /// 注册一个GameObject池。
        /// Register a GameObject pool.
        /// </summary>
        /// <param name="path">预制体路径 / Prefab path</param>
        /// <param name="maxNum">对象池最大数量 / Maximum number in the pool</param>
        /// <param name="initNum">创建对象池时预先生成对象数量 / Initial number of objects</param>
        /// <param name="groupName">组 / Group name</param>
        /// <param name="callBack">回调函数 / Callback function</param>
        /// <returns>IEnumerator for asynchronous operations</returns>
        public IEnumerator Register(string path, int maxNum, int initNum, PoolGroupName groupName = PoolGroupName.Default, Action callBack = null)
        {
            yield return GetGroup(groupName).PushGameObjectPool(path, maxNum, initNum, callBack);
        }
        
        /// <summary>
        /// 注销GameObject池。
        /// Unregister a GameObject pool.
        /// </summary>
        /// <param name="path">预制体路径 / Prefab path</param>
        /// <param name="groupName">组 / Group name</param>
        public void UnRegister(string path, PoolGroupName groupName = PoolGroupName.Default)
        {
            GetGroup(groupName).DisposeGameObjectPool(path);
        }
        
        /// <summary>
        /// 获取指定路径的GameObjectPool。
        /// Retrieve the GameObject pool for the specified path.
        /// </summary>
        /// <param name="path">预制体路径 / Prefab path</param>
        /// <param name="groupName">对象池组名，默认为Default / Pool group name, default is Default</param>
        /// <returns>GameObject池对象 / GameObject pool object</returns>
        public GameObjectPool GetGameObjectPool(string path, PoolGroupName groupName = PoolGroupName.Default)
        {
            return GetGroup(groupName).GetGameObjectPool(path);
        }

        /// <summary>
        /// 从池中获取GameObject对象，没有注册将返回null。
        /// Retrieve a GameObject from the pool; returns null if unregistered.
        /// </summary>
        /// <param name="path">预制体路径 / Prefab path</param>
        /// <param name="groupName">组 / Group name</param>
        /// <returns>GameObject实例 / Instance of the GameObject</returns>
        public GameObject GetGameObject(string path, PoolGroupName groupName = PoolGroupName.Default)
        {
            var go = GetGroup(groupName).GetGameObject(path);
            if (go == null) ModuleLog.LogError<PoolManager>("GameObject is null, path was unregistered, path: " + path + ", groupName: " + groupName);
            return go;
        }
        
        /// <summary>
        /// 从池中获取GameObject对象并设置父节点和位置，没有注册将返回null。
        /// Retrieve a GameObject from the pool and set its parent and position; returns null if unregistered.
        /// </summary>
        /// <param name="path">预制体路径 / Prefab path</param>
        /// <param name="parent">父节点 / Parent transform</param>
        /// <param name="pos">位置 / Position</param>
        /// <param name="groupName">组 / Group name</param>
        /// <returns>GameObject实例 / Instance of the GameObject</returns>
        public GameObject GetGameObject(string path, Transform parent, Vector3 pos, PoolGroupName groupName = PoolGroupName.Default)
        {
            var go = GetGameObject(path, groupName);
            if (!go) return go;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one;
            return go;
        }
        
        /// <summary>
        /// 异步获取GameObject，如果未注册将自动注册。
        /// Asynchronously retrieve a GameObject and automatically register if unregistered.
        /// </summary>
        /// <param name="path">预制体路径 / Prefab path</param>
        /// <param name="callback">回调函数 / Callback function</param>
        /// <param name="groupName">对象池组 / Group name</param>
        public void GetGameObjectAsync(string path, Action<GameObject> callback, PoolGroupName groupName = PoolGroupName.Default)
        {
            GetGroup(groupName).GetGameObjectAsync(path, callback);
        }

        /// <summary>
        /// 将GameObject回收至对象池。
        /// Release a GameObject back to the pool.
        /// </summary>
        /// <param name="go">GameObject实例 / Instance of the GameObject</param>
        /// <param name="groupName">组 / Group name</param>
        /// <returns>是否成功回收 / Whether the operation was successful</returns>
        public bool Release(GameObject go, PoolGroupName groupName = PoolGroupName.Default)
        {
            return GetGroup(groupName).ReleaseGameObject(go);
        }
        
        #endregion

        /// <summary>
        /// 获取指定组的Transform。
        /// Retrieve the Transform for the specified pool group.
        /// </summary>
        /// <param name="groupName">组 / Group name</param>
        /// <returns>组的Transform / The group's Transform</returns>
        public Transform GetGroupRoot(PoolGroupName groupName)
        {
            var groupIndex = (int)groupName;
            return _groupRoot[groupIndex].root;
        }
        
        /// <summary>
        /// 获取指定组的PoolGroup对象。
        /// Retrieve the PoolGroup object for the specified group.
        /// </summary>
        /// <param name="groupName">组 / Group name</param>
        /// <returns>PoolGroup对象 / The PoolGroup object</returns>
        public PoolGroup GetGroup(PoolGroupName groupName)
        {
            var groupIndex = (int)groupName;
            return _groupRoot[groupIndex];
        }

        /// <summary>
        /// 清理所有对象池。
        /// Clear all object pools.
        /// </summary>
        public void ClearAllPool()
        {
            foreach (var poolGroup in _groupRoot)
            {
                poolGroup.ForceClear();
            }
        }

        /// <summary>
        /// 清理指定组的对象池。
        /// Clear object pools in the specified group.
        /// </summary>
        /// <param name="groupName">组 / Group name</param>
        public void ClearByGroup(PoolGroupName groupName)
        {
            GetGroup(groupName).ForceClear();
        }

        /// <summary>
        /// 释放所有对象池。
        /// Dispose all object pools.
        /// </summary>
        public void DisposeAll()
        {
            foreach (var poolGroup in _groupRoot)
            {
                poolGroup.Dispose();
            }
        }
        
        /// <summary>
        /// 释放指定组的对象池。
        /// Dispose object pools in the specified group.
        /// </summary>
        /// <param name="groupName">组 / Group name</param>
        public void DisposeByGroup(PoolGroupName groupName)
        {
            GetGroup(groupName).Dispose();
        }
        
    }
}