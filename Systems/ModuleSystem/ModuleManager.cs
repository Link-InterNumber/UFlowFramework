using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using UnityEngine;

namespace PowerCellStudio
{
    [DonotInitModuleAutoly]
    public class ModuleManager : MonoSingleton<ModuleManager>
    {
#if UNITY_EDITOR
        [Serializable]
        public class ModuleInfo
        {
            [ReadOnly(true)]
            public string name;
            [ReadOnly(true)]
            public GameObject mono;
            [ReadOnly(true)]
            public bool inExecution;
            [ReadOnly(true)]
            public bool inLaterExecution;
        }
        public List<ModuleInfo> moduleInfos = new List<ModuleInfo>();
#endif

        private Dictionary<Type, IFixedExecutionModule> _fixedExecutionModule = new Dictionary<Type, IFixedExecutionModule>();
        private Dictionary<Type, IExecutionModule> _executionModule = new Dictionary<Type, IExecutionModule>();
        private Dictionary<Type, ILaterExecutionModule> _laterExecutionModule = new Dictionary<Type, ILaterExecutionModule>();
        private Dictionary<Type, IModule> _modules = new Dictionary<Type, IModule>();

        public static void Create()
        {
            var go = new GameObject("ModuleManager");
            go.AddComponent<ModuleManager>().Init();
        }

        private void Init()
        {
            GameObject.DontDestroyOnLoad(gameObject);
            // 实例化所有SingletonBase类
            CreateSingletonModule();
            // 实例化所有MonoSingleton类
            CreateMonoSingletonModule();
            // InitModules();
            EventManager.instance.onStartGame.AddListener(OnStartGame);
            EventManager.instance.onResetGame.AddListener(OnResetGame);
        }

        private void CreateSingletonModule()
        {
            var res = ReflectionUtils.GetInstantiableSubtype(typeof(SingletonBase<>));
            res.Sort((x, y) =>
                {
                    var xOrder = x.GetCustomAttribute<ModuleInitOrder>()?.order ?? 99999;
                    var yOrder = y.GetCustomAttribute<ModuleInitOrder>()?.order ?? 99999;
                    return xOrder.CompareTo(yOrder);
                });
            foreach (var type in res)
            {
                CreateModule(type);
            }
        }

        private void CreateMonoSingletonModule()
        {
            var res = ReflectionUtils.GetInstantiableSubtype(typeof(MonoSingleton<>));
            res.Sort((x, y) =>
            {
                var xOrder = x.GetCustomAttribute<ModuleInitOrder>()?.order ?? 99999;
                var yOrder = y.GetCustomAttribute<ModuleInitOrder>()?.order ?? 99999;
                return xOrder.CompareTo(yOrder);
            });
            foreach (var type in res)
            {
                CreateModule(type);
            }
        }

        private bool CreateModule(Type type)
        {
            if (_modules.ContainsKey(type)) return true;

            // 依赖处理
            var dependAttributes = type.GetCustomAttributes<ModuleDependence>();
            foreach (var dependAttribute in dependAttributes)
            {
                var dependType = dependAttribute.DependModuleType;
                if (!CreateModule(dependType))
                {
                    ModuleLog.LogError($"Failed to create module {type.Name} because dependency {dependType.Name} failed to create.");
                    return false;
                }
            }
            var autoInit = type.GetCustomAttribute<DonotInitModuleAutoly>() == null;
            var property = type.GetProperty("instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            var isMonoBehavior = ReflectionUtils.IsSubTypeOf(type, typeof(MonoBehaviour));
            object typeInstance = null;
            try
            {
                if (property == null)
                {
                    // 非静态的单例类，直接实例化
                    if (!autoInit)
                    {
                        ModuleLog.LogWarning($"Module {type.Name} is marked with DonotInitModuleAutoly, but it does not have an instance property. Skipping initialization.");
                        return false;
                    }
                    if (isMonoBehavior)
                    {
                        var go = new GameObject(type.Name);
                        typeInstance = go.AddComponent(type);
                        go.transform.SetParent(transform);
                    }
                    else
                    {
                        typeInstance = Activator.CreateInstance(type);
                    }
                }
                else
                {
                    // 静态的单例类，获取instance属性的值，如果为null则实例化
                    typeInstance = property.GetValue(null, null);
                    if (typeInstance == null && autoInit)
                    {
                        if (isMonoBehavior)
                        {
                            var go = new GameObject(type.Name);
                            typeInstance = go.AddComponent(type);
                            go.transform.SetParent(transform);
                        }
                        else
                        {
                            typeInstance = Activator.CreateInstance(type);
                            property.SetValue(null, typeInstance);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ModuleLog.LogError($"Failed to create module {type.Name}: {e}");
                return false;
            }

            if (typeInstance == null) return false;
            if (typeInstance is IModule module)
            {
                module.OnInit();
                AddModule(type, module);
            }
#if UNITY_EDITOR
            moduleInfos.Add(new ModuleInfo
            {
                name = type.Name,
                mono = null,
                inExecution = typeInstance is IExecutionModule,
                inLaterExecution = typeInstance is ILaterExecutionModule
            });
#endif
            return true;
        }

        protected override void Deinit()
        {
            base.Deinit();
            EventManager.instance.onStartGame.RemoveListener(OnStartGame);
            EventManager.instance.onResetGame.RemoveListener(OnResetGame);
            // var modules =_modules.Values.Reverse().ToList();
            // foreach (var module in modules)
            // {
            //     if (module is IEventModule eventModule)
            //     {
            //         eventModule.UnRegisterEvent();
            //     }
            //     module.Dispose();
            // }
            _modules = null;
            _executionModule = null;
            _laterExecutionModule = null;
            _fixedExecutionModule = null;
        }

        private void OnStartGame()
        {
            foreach (var (key, value) in _modules)
            {
                if (!(value is IOnGameStartModule)) continue;
                (value as IOnGameStartModule).OnGameStart();
            }
        }

        private void OnResetGame()
        {
            foreach (var (key, value) in _modules)
            {
                if (value is IOnGameResetModule onGameResetModule)
                    onGameResetModule.OnGameReset();
            }
        }

        private void AddModule<T>(Type type, T module) where T : class, IModule
        {
            var isMonoBehavior = module is MonoBehaviour;
            if (module is IEventModule eventModule)
            {
                eventModule.RegisterEvent();
            }
            if (module is IFixedExecutionModule fixedExecutionModule && _fixedExecutionModule != null && !isMonoBehavior)
            {
                fixedExecutionModule.inExecution = true;
                _fixedExecutionModule[type] = fixedExecutionModule;
            }
            if (module is IExecutionModule executionModule && _executionModule != null && !isMonoBehavior)
            {
                executionModule.inExecution = true;
                _executionModule[type] = executionModule;
            }
            if (module is ILaterExecutionModule laterExecutionModule && _laterExecutionModule != null && !isMonoBehavior)
            {
                laterExecutionModule.inExecution = true;
                _laterExecutionModule[type] = laterExecutionModule;
            }
            if(_modules != null) _modules[type] = module;
        }
        
        public void AddModule<T>(T module) where T : class, IModule
        {
            var type = module.GetType();
            AddModule(type, module);
        }
        
        private void RemoveModule(Type type)
        {
            _fixedExecutionModule?.Remove(type);
            _executionModule?.Remove(type);
            _laterExecutionModule?.Remove(type);
            if(_modules?.TryGetValue(type, out var module) ?? false)
            {
                if (module is IEventModule eventModule)
                {
                    eventModule.UnRegisterEvent();
                }
                module.Dispose();
            }
            // type是monoSingleton类型时，删除对应的GameObject
            if (type.BaseType != null && type.BaseType.IsGenericType && type.BaseType.GetGenericTypeDefinition() == typeof(MonoSingleton<>))
            {
                var instance = GameObject.Find(type.Name);
                if (instance != null)
                {
                    GameObject.Destroy(instance.gameObject);
                }
            }
#if UNITY_EDITOR
            moduleInfos.RemoveAll(o => o.name.Equals(type.Name));
#endif
        }

        public void RemoveModule<T>() where T : class, IModule
        {
            var type = typeof(T);
            RemoveModule(type);
        }

        public T GetModule<T>() where T : class, IModule
        {
            var type = typeof(T);
            if (_modules != null && _modules.TryGetValue(type, out var module))
            {
                return module as T;
            }
            ModuleLog.LogError<T>($"Module {type.Name} not found.");
            return null;
        }

        public T GetOrAddModule<T>() where T : class, IModule, new()
        {
            var type = typeof(T);
            if (_modules != null && _modules.TryGetValue(type, out var module))
            {
                return module as T;
            }
            if (CreateModule(type))
            {
                return GetModule<T>();
            }
            return null;
        }

        private void FixedUpdate()
        {
            if(_fixedExecutionModule == null) return;
            foreach (var (key, value) in _fixedExecutionModule)
            {
                if(!value.inExecution) continue;
                value.FixedExecute(Time.fixedDeltaTime);
            }
        }
        
        private void Update()
        {
            if(_executionModule == null) return;
            foreach (var (key, value) in _executionModule)
            {
                if(!value.inExecution) continue;
                value.Execute(Time.deltaTime);
            }
        }

        private void LateUpdate()
        {
            if(_laterExecutionModule == null) return;
            foreach (var (key, value) in _laterExecutionModule)
            {
                if (!value.inExecution) continue;
                value.LaterExecute(Time.deltaTime);
            }
        }
    }
}