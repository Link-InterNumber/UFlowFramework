using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PowerCellStudio
{
    internal struct GenericTypeInfo
    {
        public Type type;
        public Type genericDefinition;
        public Type[] genericArguments;
        public Type resolvedType;
    }

    internal class BinarySerializeTypeBuffer
    {
        // 类型缓存
        private static readonly Dictionary<Type, FieldInfo[]> _fieldCache = new Dictionary<Type, FieldInfo[]>();
        
        // 特定类型自定义写入方式
        private static readonly Dictionary<Type, IBinarySerializerTypeSelector> _customSelectorCache = new Dictionary<Type, IBinarySerializerTypeSelector>();
        
        private static readonly Dictionary<Type, GenericTypeInfo> _genericTypeInfoCache = new Dictionary<Type, GenericTypeInfo>();
        
        private static readonly Type _IListType = typeof(IList<>);
        private static readonly Type _IDictionaryType = typeof(IDictionary<,>);
        private static readonly Type _ISetType = typeof(ISet<>);
        private static readonly Type _QueueType = typeof(Queue<>);
        private static readonly Type _StackType = typeof(Stack<>);
        private static readonly Type _KeyValuePairType = typeof(KeyValuePair<,>);

        public static Type IListType => _IListType;
        public static Type IDictionaryType => _IDictionaryType;
        public static Type ISetType => _ISetType;
        public static Type QueueType => _QueueType;
        public static Type StackType => _StackType;
        public static Type KeyValuePairType => _KeyValuePairType;

        static BinarySerializeTypeBuffer()
        {
            RegisterCustomSelector(new IntPtrSelector());
            RegisterCustomSelector(new UIntPtrSelector());
            RegisterCustomSelector(new GuidSelector());
            RegisterCustomSelector(new TimeSpanSelector());
            RegisterCustomSelector(new DateTimeOffsetSelector());
        }

        private static readonly Dictionary<Type, Type> _supportedCollection = new Dictionary<Type, Type>
        {
            { typeof(IList<>), typeof(List<>) },
            { typeof(IDictionary<,>), typeof(Dictionary<,>) },
            { typeof(ISet<>), typeof(HashSet<>) },
            { typeof(Queue<>), typeof(Queue<>) },
            { typeof(Stack<>), typeof(Stack<>) },
        };
        
        public static bool IsSupportedType(Type type)
        {
            if (type.IsInterface)
            {
                return _supportedCollection.ContainsKey(type.GetGenericTypeDefinition());
            }
            if (type.IsAbstract || type.ContainsGenericParameters)
                return false;
#if UNITY_5_3_OR_NEWER
            // 处理 UnityEngine.Object 类型（不支持直接序列化）
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                return false;
            }
#endif
            return true;
        }

        public static FieldInfo[] GetSerializableFields(Type type)
        {
            if (!_fieldCache.TryGetValue(type, out var fields))
            {
                fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(f => !f.IsNotSerialized 
# if UNITY_5_3_OR_NEWER
                                && (f.IsPublic || f.IsDefined(typeof(UnityEngine.SerializeField), false))
# endif
                                )
                    .OrderBy(f => f.MetadataToken)
                    .ToArray();
                _fieldCache[type] = fields;
            }
            return fields;
        }

        internal static GenericTypeInfo GetCollectionGenericTypeInfo(Type type)
        {
            if (!_genericTypeInfoCache.TryGetValue(type, out var info))
            {
                foreach (var keyValue in _supportedCollection)
                {
                    var (genericType, fallback) =  (keyValue.Key, keyValue.Value);
                    if (genericType.IsInterface)
                    {
                        if (type.IsGenericType && type.GetGenericTypeDefinition() == genericType)
                        {
                            info = new GenericTypeInfo()
                            {
                                type = type,
                                genericDefinition = genericType,
                                genericArguments = type.GetGenericArguments(),
                                resolvedType = fallback.MakeGenericType(type.GetGenericArguments())
                            };
                            break;
                        }

                        Type[] interfaces = type.GetInterfaces();
                        for (int i = 0; i < interfaces.Length; i++)
                        {
                            Type interfaceType = interfaces[i];
                            if (!interfaceType.IsGenericType || interfaceType.GetGenericTypeDefinition() != genericType)
                                continue;
                            info = new GenericTypeInfo()
                            {
                                type = type,
                                genericDefinition = genericType,
                                genericArguments = interfaceType.GetGenericArguments(),
                                resolvedType = type.IsInterface ? fallback.MakeGenericType(interfaceType.GetGenericArguments()) : type
                            };
                            break;
                        }
                    }
                    else
                    {
                        var tempType = type;
                        while (tempType != null && tempType != typeof(object))
                        {
                            if (!tempType.IsGenericType || tempType.GetGenericTypeDefinition() != genericType)
                            {
                                tempType = tempType.BaseType;
                                continue;
                            }
                            info = new GenericTypeInfo()
                            {
                                type = type,
                                genericDefinition = genericType,
                                genericArguments = tempType.GetGenericArguments(),
                                resolvedType = type
                            };
                            break;
                        }
                    }

                    if (info.genericDefinition != null)
                    {
                        break;
                    }
                }
                // 找不到也塞个空值 
                _genericTypeInfoCache[type] = info;
            }
            return info;
        }

        public static void RegisterCustomSelector(IBinarySerializerTypeSelector selector)
        {
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));
            _customSelectorCache[selector.TargetType] = selector;
        }

        public static IBinarySerializerTypeSelector GetCustomSelector(Type type)
        {
            _customSelectorCache.TryGetValue(type, out var selector);
            return selector;
        }
    }
}