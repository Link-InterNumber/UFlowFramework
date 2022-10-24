using System;
using System.Collections.Generic;
using LinkFrameWork.DesignPatterns;
using LinkFrameWork.Extentions;
using LinkFrameWork.PoolSystem;
using Unity.VisualScripting;
using UnityEngine;
using IPoolable = LinkFrameWork.PoolSystem.IPoolable;

namespace LinkFrameWork.MonoInstance
{
    public class PoolManager : MonoSingleton<PoolManager>
    {
        private Dictionary<Type, PoolCarrier> _poolCarriers = new Dictionary<Type, PoolCarrier>();
        private Dictionary<Type, Transform> _poolParent = new Dictionary<Type, Transform>();

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
        }

        public bool Register<T>(IPoolable prefab) where T : IPoolable
        {
            if (IsRegister<T>()) return false;

            var poolCarrier = new PoolCarrier();
            poolCarrier.InitPool(prefab);
            if (prefab is MonoBehaviour monoPrefab)
            {
                monoPrefab.transform.SetParent(transform);
                monoPrefab.gameObject.SetActive(false);
            }

            _poolCarriers.Add(typeof(T), poolCarrier);
            return true;
        }

        public bool Register<T>(GameObject temp) where T : IPoolable
        {
            if (IsRegister<T>()) return false;

            var poolCarrier = new PoolCarrier();
            var isp = temp.IsPrefab();
            var prefab = temp.IsPrefab() ? Instantiate(temp) : temp;
            poolCarrier.InitPool(prefab);
            prefab.transform.SetParent(transform);
            prefab.gameObject.SetActive(false);
            _poolCarriers.Add(typeof(T), poolCarrier);
            return true;
        }

        public bool UnRegister<T>() where T : IPoolable
        {
            if (!IsRegister<T>()) return false;
            var key = typeof(T);
            var pool = _poolCarriers[key];
            pool.Clear();
            pool.GetPrefab().DestroyNode();
            _poolCarriers.Remove(key);
            pool = null;
            if (!_poolParent.TryGetValue(key, out var t)) return true;
            Destroy(t.gameObject);
            _poolParent.Remove(key);
            return true;
        }

        public bool IsRegister<T>() where T : IPoolable
        {
            return _poolCarriers.ContainsKey(typeof(T));
        }

        public T Spawn<T>() where T : class, IPoolable
        {
            if (!IsRegister<T>()) throw new Exception($"{typeof(T)} Do Not Register");

            var node = _poolCarriers[typeof(T)].GetNode().Spawn<T>();
            node.OnSpawn();
            return node;
        }

        public void DeSpawn<T>(T node) where T : class, IPoolable
        {
            node.OnDeSpawn();
            var key = typeof(T);
            if (!IsRegister<T>()) return;
            _poolCarriers[key].PushNode(node);
            if (node is not MonoBehaviour monoPoolNode) return;
            if (!_poolParent.TryGetValue(key, out var t))
            {
                var nodeParent = new GameObject($"{key.Name}_Pool");
                ComponentHolderProtocol.GetOrAddComponent<PoolCarrierInspector>(nodeParent).Init(GetPool<T>());
                t = nodeParent.transform;
                _poolParent.Add(key, t);
            }

            monoPoolNode.transform.SetParent(t);
        }

        // public void DeSpawnGameObject<T>(T node) where T : PoolableGameObject
        // {
        //     DeSpawn(node);
        //     var key = typeof(T);
        //     if (!_poolParent.TryGetValue(key, out var t))
        //     {
        //         var nodeParent = new GameObject($"{key.Name}_Pool");
        //         nodeParent.GetOrAddComponent<PoolCarrierInspector>().Init(GetPool<T>());
        //         t = nodeParent.transform;
        //         _poolParent.Add(key, t);
        //     }
        //     node.transform.SetParent(t);
        // }

        public PoolCarrier GetPool<T>() where T : IPoolable
        {
            return !IsRegister<T>() ? null : _poolCarriers[typeof(T)];
        }

        public bool TryGetPool<T>(out PoolCarrier pool) where T : IPoolable
        {
            return _poolCarriers.TryGetValue(typeof(T), out pool);
        }

        public bool ClearPool<T>() where T : IPoolable
        {
            if (!IsRegister<T>()) return false;
            _poolCarriers[typeof(T)].Clear();
            return true;
        }
    }
}