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
        public Transform transform => _transform;
        
        private bool _inited = false;

        
        public void OnInit()
        {
            if(_inited) return;
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

        public void Deinit()
        {
            ClearAllPool();
            if(!_transform) return;
            GameObject.Destroy(_transform);
            _transform = null;
            EventManager.instance.onClearUnusedAsset.RemoveListener(ClearAllPool);
        }

        public void OnGameStart()
        {
            ModuleLog<PoolManager>.Log("Module Init!");
        }
        
        #region IPoolable

        /// <summary>
        /// 生成一个对象池，或获得已有的对象池
        /// </summary>
        /// <param name="createFun">构造方法</param>
        /// <param name="maxNum">对象池存放的最大数量</param>
        /// <param name="initNum">创建对象池时预先生成对象数量</param>
        /// <param name="groupName">组</param>
        /// <typeparam name="T">对象类型</typeparam>
        /// <returns>对象池</returns>
        public PoolableObjectPool Register<T>(Func<T> createFun, int maxNum, int initNum, PoolGroupName groupName = PoolGroupName.Default)
            where T :class, IPoolable
        {
            return GetGroup(groupName).Push<T>(createFun, maxNum, initNum);
        }
        
        /// <summary>
        /// 注销一个对象池
        /// 注销后，对象池中的对象将全部被销毁
        /// </summary>
        /// <param name="groupName">组</param>
        /// <typeparam name="T">对象类型</typeparam>
        public void UnRegister<T>(PoolGroupName groupName = PoolGroupName.Default)
            where T :class, IPoolable
        {
            GetGroup(groupName).Dispose<T>();
        }

        /// <summary>
        /// 获取一个对象池内的对象，没有注册的对象类型将会返回null
        /// </summary>
        /// <param name="groupName">组</param>
        /// <typeparam name="T">对象类型</typeparam>
        /// <returns></returns>
        public T Get<T>(PoolGroupName groupName = PoolGroupName.Default)
            where T :class, IPoolable
        {
            var obj = GetGroup(groupName).Get<T>();
            if (obj == null) ModuleLog<PoolManager>.LogError($"{typeof(T).Name} is null, {typeof(T).Name} was unregister,\n groupName: " + groupName);
            return obj;
        }
        
        /// <summary>
        /// 获取一个对象池内的对象，没有注册的对象类型将会进行注册
        /// </summary>
        /// <param name="groupName">组</param>
        /// <typeparam name="T">有new()的对象类型</typeparam>
        /// <returns></returns>
        public T GetOrNew<T>(PoolGroupName groupName = PoolGroupName.Default)
            where T :class, IPoolable, new()
        {
            return GetGroup(groupName).GetOrPush<T>();
        }

        /// <summary>
        /// 回收一个对象到对象池
        /// </summary>
        /// <param name="item">对象</param>
        /// <param name="groupName">组</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>是否成功将对象放入池中</returns>
        public bool Release<T>(T item, PoolGroupName groupName = PoolGroupName.Default)
            where T :class, IPoolable
        {
            return GetGroup(groupName).Release(item);
        }

        #endregion

        #region GameObject
        
        /// <summary>
        /// 创建一个对象池
        /// </summary>
        /// <param name="path">预制体路径</param>
        /// <param name="maxNum">对象池最大数量</param>
        /// <param name="initNum">创建对象池时预先生成对象数量</param>
        /// <param name="groupName">组</param>
        /// <returns></returns>
        public IEnumerator Register(string path, int maxNum, int initNum, PoolGroupName groupName = PoolGroupName.Default, Action callBack = null)
        {
            yield return GetGroup(groupName).PushGameObjectPool(path, maxNum, initNum,callBack);
        }
        
        /// <summary>
        /// 注销对象池
        /// </summary>
        /// <param name="path">预制体路径</param>
        /// <param name="groupName">组</param>
        public void UnRegister(string path, PoolGroupName groupName = PoolGroupName.Default)
        {
            GetGroup(groupName).DisposeGameObjectPool(path);
        }
        
        
        /// <summary>
        /// 获取指定路径的GameObjectPool对象。
        /// </summary>
        /// <param name="path">预制体路径</param>
        /// <param name="groupName">对象池组名，默认为Default</param>
        /// <returns>返回GameObjectPool对象，如果路径未注册则返回null</returns>
        public GameObjectPool GetGameObjectPool(string path, PoolGroupName groupName = PoolGroupName.Default)
        {
            return GetGroup(groupName).GetGameObjectPool(path);
        }

        /// <summary>
        /// 获取对象池中的对象，没有注册的对象将返回null
        /// </summary>
        /// <param name="path">预制体路径</param>
        /// <param name="groupName">组</param>
        /// <returns></returns>
        public GameObject GetGameObject(string path, PoolGroupName groupName = PoolGroupName.Default)
        {
            var go = GetGroup(groupName).GetGameObject(path);
            if (go == null) ModuleLog<PoolManager>.LogError("GameObject is null, path was unregister,\n path: " + path + ", groupName: " + groupName);
            return go;
        }
        
        /// <summary>
        /// 获取对象池中的对象并设置父节点和位置，没有注册的对象将返回null
        /// </summary>
        /// <param name="path">预制体路径</param>
        /// <param name="parent">父节点</param>
        /// <param name="pos">位置</param>
        /// <param name="groupName">组</param>
        /// <returns></returns>
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
        /// 获取对象池中的对象并设置父节点和位置，没有注册的对象将自动注册
        /// </summary>
        /// <param name="path">预制体路径</param>
        /// <param name="callback">回调</param>
        /// <param name="groupName">组</param>
        public void GetGameObjectAsync(string path, Action<GameObject> callback, PoolGroupName groupName = PoolGroupName.Default)
        {
            GetGroup(groupName).GetGameObjectAsync(path, callback);
        }

        /// <summary>
        /// 回收对象到对象池中
        /// </summary>
        /// <param name="go">GameObject</param>
        /// <param name="groupName">组</param>
        /// <returns></returns>
        public bool Release(GameObject go, PoolGroupName groupName = PoolGroupName.Default)
        {
            return GetGroup(groupName).ReleaseGameObject(go);
        }
        
        #endregion

        public Transform GetGroupRoot(PoolGroupName groupName)
        {
            var groupIndex = (int) groupName;
            return _groupRoot[groupIndex].root;
        }
        
        public PoolGroup GetGroup(PoolGroupName groupName)
        {
            var groupIndex = (int) groupName;
            return _groupRoot[groupIndex];
        }

        public void ClearAllPool()
        {
            foreach (var poolGroup in _groupRoot)
            {
                poolGroup.ForceClear();
            }
        }

        public void ClearByGroup(PoolGroupName groupName)
        {
            GetGroup(groupName).ForceClear();
        }

        public void DisposeAll()
        {
            foreach (var poolGroup in _groupRoot)
            {
                poolGroup.Dispose();
            }
        }
        
        public void DisposeByGroup(PoolGroupName groupName)
        {
            GetGroup(groupName).Dispose();
        }
        
    }
}