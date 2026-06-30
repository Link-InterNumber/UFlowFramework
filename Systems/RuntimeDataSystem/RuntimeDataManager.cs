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
        public class RuntimeData<T> : IRuntimeData
            where T : struct //, ICloneT<T>
        {
            public RuntimeData(T data) { _rawData = data; }
            public Type dataType { get { return typeof(T); } }

            private T _rawData;
            public event OnRuntimeDataChange<T> onRuntimeDataChange;

            public T GetData() { return _rawData; }

            public void ReplaceData(T newData)
            {
                var temp = _rawData;
                _rawData = newData;
                onRuntimeDataChange?.Invoke(temp, newData);
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
        // internal class RuntimeDataList<T> : IRuntimeData where T : class
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
        public class RuntimeDataDic<K,T> : IRuntimeData, IEnumerable<T> 
            where T : struct //, ICloneT<T>
        {
            // ReSharper disable once UnusedMember.Local
            public RuntimeDataDic() { _rawData = new Dictionary<K, T>(); }
            // ReSharper disable once UnusedMember.Local
            public RuntimeDataDic(IEnumerable<KeyValuePair<K, T>> initDatas) { _rawData = new Dictionary<K, T>(initDatas); }
            
            private Dictionary<K,T> _rawData;
            public event OnRuntimeDataChange<T> onRuntimeDataChange;
            
            public Type dataType { get { return typeof(T); } }

            public T GetData(K key)
            {
                if (_rawData.TryGetValue(key, out var data))
                {
                    return data;
                }
                return default;
            }
            
            public void ReplaceData(K key, T newData)
            {
                T temp = default;
                if (_rawData.TryGetValue(key, out var oldData))
                {
                    temp = oldData;
                }
                _rawData[key] = newData;
                onRuntimeDataChange?.Invoke(temp, newData);
            }

            // ReSharper disable once UnusedMethodReturnValue.Local
            public bool Remove(K key)
            {
                if (_rawData.Remove(key, out var t))
                {
                    onRuntimeDataChange?.Invoke(t, default);
                    return true;
                }
                return false;
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
                return _rawData.Values.Select(o => o).GetEnumerator();
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

            public IRuntimeData GetDataByElementType<T>() where T : struct //, ICloneT<T>
            {
                var key = typeof(T);
                var datas = _datas.Values;
                foreach (var runtimeData in datas)
                {
                    var datasType = runtimeData.dataType;
                    if (datasType == key) return runtimeData;
                }
                return null;
            }
        
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
        
        private Dictionary<Type, IRuntimeDataHandler> _handlers;
        
        public void OnGameStart()
        {
            ModuleLogger.Log<RuntimeDataManager>("Module Init!");
        }
        
        public void OnInit()
        {
            if(_doNotClearStorage == null) _doNotClearStorage = new RuntimeDataStorage();
            _storage = new RuntimeDataStorage();
        }

        public void RegisterEvent()
        {
            EventManager.instance.onStartGame.AddListener(OnStartGame);
            EventManager.instance.onResetGame.AddListener(ClearRuntimeStorage);
        }

        public void UnRegisterEvent()
        {
            EventManager.instance?.onStartGame.RemoveListener(OnStartGame);
            EventManager.instance?.onResetGame.RemoveListener(ClearRuntimeStorage);
        }
        
        private void OnStartGame()
        {
            _handlers = new Dictionary<Type, IRuntimeDataHandler>();
            // 反射查找所可以实例化的IRuntimeDataHandler<T>
            var instantiableHandler = ReflectionUtils.GetInstantiableSubtype(typeof(IRuntimeDataHandler<>));
            for (var i = 0; i < instantiableHandler.Count; i++)
            {
                var handlerType = instantiableHandler[i];
                var genericType = handlerType.GetGenericArguments()[0];
                var handler = ReflectionUtils.CreateInstance(handlerType) as IRuntimeDataHandler;
                if (handler == null) continue;
                handler.InitData();
                _handlers.Add(genericType, handler);
            }
            InitRuntimeData();
        }

        partial void InitRuntimeData();

        /// <summary>
        /// 添加运行时数据
        /// </summary>
        /// <param name="data">数据实例</param>
        /// <param name="doNotClear">设定是否不会随游戏重启而清除</param>
        /// <typeparam name="T">数据类</typeparam>
        public void AddRuntimeStorage<T>(T data, bool doNotClear = false) where T : class, IRuntimeData
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
        public T GetRuntimeStorage<T>() where T : class, IRuntimeData
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
        public void RemoveRuntimeStorage<T>() where T : class, IRuntimeData
        {
            if (!_storage.Remove<T>())
            {
                _doNotClearStorage.Remove<T>();
            }
        }

        private void ClearRuntimeStorage()
        {
            _storage.Clear();
        }

        public T GetData<T>(int key) where T : struct //, ICloneT<T>
        {
            var t = typeof(T);
            if (_handlers.TryGetValue(t, out var handler) && handler is IRuntimeDataHandler<T> handlerData)
            {
                return handlerData.GetData(key);
            }
            var datas = _storage.GetDataByElementType<T>();
            if (datas == null)
            {
                datas = _doNotClearStorage.GetDataByElementType<T>();
            }
            if (datas == null) return default(T);
            if (datas is RuntimeDataDic<int, T> dic) return dic.GetData(key);
            if (datas is RuntimeData<T> rd) return rd.GetData();
            return default(T);
        }

        public void AddData<T>(T data) where T : struct //, ICloneT<T>
        {
            var t = typeof(T);
            if (_handlers.TryGetValue(t, out var handler) && handler is IRuntimeDataHandler<T> handlerData)
            {
                handlerData.AddData(data);
                return;
            }
            var datas = _storage.GetDataByElementType<T>();
            if (datas == null)
            {
                datas = _doNotClearStorage.GetDataByElementType<T>();
            }
            if (datas == null) return;
            if (datas is RuntimeDataDic<int, T> dic)
            {
                dic.ReplaceData(data.GetHashCode(), data);
                return;
            }

            if (datas is RuntimeData<T> rd)
            {
                rd.GetData();
            }
        }
        
        public void RemoveData<T>(T data) where T : struct //, ICloneT<T>
        {
            var t = typeof(T);
            if (_handlers.TryGetValue(t, out var handler) && handler is IRuntimeDataHandler<T> handlerData)
            {
                handlerData.RemoveData(data);
                return;
            }
            var datas = _storage.GetDataByElementType<T>();
            if (datas == null)
            {
                datas = _doNotClearStorage.GetDataByElementType<T>();
            }
            if (datas == null) return;
            if (datas is RuntimeDataDic<int, T> dic)
            {
                dic.Remove(data.GetHashCode());
                return;
            }

            if (datas is RuntimeData<T> rd)
            {
                rd.ReplaceData(default(T));
            }
        }

        public void AddChangeListener<T>(OnRuntimeDataChange<T> action)
            where T : struct //, ICloneT<T>
        {
            var t = typeof(T);
            if (_handlers.TryGetValue(t, out var handler) && handler is IRuntimeDataHandler<T> handlerData)
            {
                handlerData.AddListener(action);
                return;
            }
            var datas = _storage.GetDataByElementType<T>();
            if (datas == null)
            {
                datas = _doNotClearStorage.GetDataByElementType<T>();
            }
            if (datas == null) return;
            if (datas is RuntimeDataDic<int, T> dic)
            {
                dic.AddListener(action);
                return;
            }

            if (datas is RuntimeData<T> rd)
            {
                rd.AddListener(action);
            }
        }
        
        public void RemoveChangeListener<T>(OnRuntimeDataChange<T> action)
            where T : struct //, ICloneT<T>
        {
            var t = typeof(T);
            if (_handlers.TryGetValue(t, out var handler) && handler is IRuntimeDataHandler<T> handlerData)
            {
                handlerData.RemoveListener(action);
                return;
            }
            var datas = _storage.GetDataByElementType<T>();
            if (datas == null)
            {
                datas = _doNotClearStorage.GetDataByElementType<T>();
            }
            if (datas == null) return;
            if (datas is RuntimeDataDic<int, T> dic)
            {
                dic.RemoveListener(action);
                return;
            }

            if (datas is RuntimeData<T> rd)
            {
                rd.RemoveListener(action);
            }
        }
    }
}