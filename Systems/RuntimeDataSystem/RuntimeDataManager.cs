using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PowerCellStudio
{
    public delegate void OnRuntimeDataChange<T>(T oldData, T newData);
    public sealed partial class RuntimeDataManager : SingletonBase<RuntimeDataManager>, IEventModule, IOnGameStartModule
    {
        #region define
        
        /// <summary>
        /// 运行时数据容器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private class RuntimeData<T> : IRuntimeData where T : ICloneT<T>
        {
            public RuntimeData(T data) { _rawData = data; }

            private T _rawData;
            private IRuntimeData _runtimeDataImplementation;
            public event OnRuntimeDataChange<T> onRuntimeDataChange;

            public T GetData() { return _rawData.Clone(); }

            public void ReplaceData(T newData)
            {
                var temp = _rawData.Clone();
                _rawData = newData;
                onRuntimeDataChange?.Invoke(temp, newData.Clone());
            }

            public void AddListener(OnRuntimeDataChange<T> action)
            {
                onRuntimeDataChange += action;
            }

            public void RemoveListener(OnRuntimeDataChange<T> action)
            {
                onRuntimeDataChange -= action;
            }
        }
        
        // /// <summary>
        // /// 列表格式存储的运行时数据容器
        // /// </summary>
        // /// <typeparam name="T"></typeparam>
        // private class RuntimeDataList<T> : IRuntimeData where T : class
        // {
        //     public RuntimeDataList() { _rawData = new List<T>(); }
        //     public RuntimeDataList(IEnumerable<T> initDatas) { _rawData = new List<T>(initDatas); }
        //     
        //     private List<T> _rawData;
        //     public event OnRuntimeDataChange<T> onRuntimeDataChange;
        //
        //     public T GetData(Func<T, bool> match)
        //     {
        //         if (match == null) return null;
        //         var data = _rawData.FirstOrDefault(match);
        //         return data;
        //     }
        //     
        //     public void ReplaceData(Func<T, bool> match, T newData)
        //     {
        //         for (var i = 0; i < _rawData.Count; i++)
        //         {
        //             if(!match(_rawData[i])) continue;
        //             _rawData[i] = newData;
        //             onRuntimeDataChange?.Invoke(_rawData[i]);
        //             break;
        //         }
        //     }
        //
        //     public bool Remove(Func<T, bool> match)
        //     {
        //         if (match == null)
        //         {
        //             return true;
        //         }
        //         return _rawData.RemoveAll(o => match(o)) > 0;
        //     }
        //     
        //     public void AddListener(OnRuntimeDataChange<T> action)
        //     {
        //         onRuntimeDataChange += action;
        //     }
        //
        //     public void RemoveListener(OnRuntimeDataChange<T> action)
        //     {
        //         onRuntimeDataChange -= action;
        //     }
        // }
        
        /// <summary>
        /// 字典格式存储的运行时数据容器
        /// </summary>
        /// <typeparam name="K">key</typeparam>
        /// <typeparam name="T">value</typeparam>
        private class RuntimeDataDic<K,T> : IRuntimeData, IEnumerable<T> where T : ICloneT<T>
        {
            // ReSharper disable once UnusedMember.Local
            public RuntimeDataDic() { _rawData = new Dictionary<K, T>(); }
            // ReSharper disable once UnusedMember.Local
            public RuntimeDataDic(IEnumerable<KeyValuePair<K, T>> initDatas) { _rawData = new Dictionary<K, T>(initDatas); }
            
            private Dictionary<K,T> _rawData;
            public event OnRuntimeDataChange<T> onRuntimeDataChange;

            public T GetData(K key)
            {
                _rawData.TryGetValue(key, out var data);
                return data;
            }
            
            public void ReplaceData(K key, T newData)
            {
                T temp = default;
                if (_rawData.TryGetValue(key, out var oldData))
                {
                    temp = oldData.Clone();
                }
                _rawData[key] = newData;
                onRuntimeDataChange?.Invoke(temp, newData.Clone());
            }

            // ReSharper disable once UnusedMethodReturnValue.Local
            public bool Remove(K key)
            {
                return _rawData.Remove(key);
            }
            
            public void AddListener(OnRuntimeDataChange<T> action)
            {
                onRuntimeDataChange += action;
            }

            public void RemoveListener(OnRuntimeDataChange<T> action)
            {
                onRuntimeDataChange -= action;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return _rawData.Values.Select(o => o.Clone()).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
            
            public void Clear()
            {
                _rawData.Clear();
            }
        }
        
        /// <summary>
        /// 保存运行时数据
        /// </summary>
        private class RuntimeDataStorage
        {
            private Dictionary<Type, IRuntimeData> _datas;
        
            public RuntimeDataStorage()
            {
                _datas = new Dictionary<Type, IRuntimeData>();
            }
        
            public void AddRuntimeData<T>(T data) where T :class, IRuntimeData
            {
                _datas[typeof(T)] = data;
            }

            // public T GetData<T>() where T : class, IRuntimeData
            // {
            //     var key = typeof(T);
            //     return _datas[key] as T;
            // }
        
            public bool TryGetData<T>(out T data) where T : class, IRuntimeData
            {
                var key = typeof(T);
                if (_datas.TryGetValue(key, out var d))
                {
                    data = d as T;
                    return true;                    
                }
                data = null;
                return false;
            }

            public bool Remove<T>() where T : class, IRuntimeData
            {
                var key = typeof(T);
                return _datas.Remove(key);
            }
            
            public void Clear()
            {
                if(_datas == null) return;
                _datas.Clear();
            }
        }
        
        #endregion

        /// <summary>
        /// 当前玩家的运行时数据
        /// </summary>
        private RuntimeDataStorage _storage;
        /// <summary>
        /// 游戏重启不会清除的运行时数据保存在这里
        /// </summary>
        private RuntimeDataStorage _doNotClearStorage;
        
        public void OnGameStart()
        {
            ModuleLog<RuntimeDataManager>.Log("Module Init!");
        }
        
        public void OnInit()
        {
            if(_doNotClearStorage == null) _doNotClearStorage = new RuntimeDataStorage();
            _storage = new RuntimeDataStorage();
        }

        public void RegisterEvent()
        {
            EventManager.instance.onStartGame.AddListener(InitRuntimeData);
            EventManager.instance.onResetGame.AddListener(ClearRuntimeData);
        }

        public void UnRegisterEvent()
        {
            EventManager.instance.onStartGame.RemoveListener(InitRuntimeData);
            EventManager.instance.onResetGame.RemoveListener(ClearRuntimeData);
        }

        private  void InitRuntimeData()
        {
            InitBag();
        }

        /// <summary>
        /// 添加运行时数据
        /// </summary>
        /// <param name="data">数据实例</param>
        /// <param name="doNotClear">设定是否不会随游戏重启而清除</param>
        /// <typeparam name="T">数据类</typeparam>
        private void AddRuntimeData<T>(T data, bool doNotClear = false) where T : class, IRuntimeData
        {
            if (doNotClear)
            {
                // if (_doNotClearStorage == null) _doNotClearStorage = new RuntimeDataStorage();
                _doNotClearStorage.AddRuntimeData(data);
                return;
            }
            // if (_storage == null) _storage = new RuntimeDataStorage();
            // ReSharper disable once PossibleNullReferenceException
            _storage.AddRuntimeData(data);
        }

        /// <summary>
        /// 获取运行时数据
        /// </summary>
        /// <typeparam name="T">数据类</typeparam>
        /// <returns></returns>
        private T GetRuntimeData<T>() where T : class, IRuntimeData
        {
            if (_storage.TryGetData<T>(out var data))
            {
                return data;
            }
            _doNotClearStorage.TryGetData<T>(out var dat);
            return dat;
        }
        
        /// <summary>
        /// 删除运行时数据
        /// </summary>
        /// <typeparam name="T">数据类</typeparam>
        private void RemoveRuntimeData<T>() where T : class, IRuntimeData
        {
            if (_storage.Remove<T>())
            {
                _doNotClearStorage.Remove<T>();
            }
        }

        private void ClearRuntimeData()
        {
            _storage.Clear();
        }
    }
}