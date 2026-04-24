using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

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
        private static readonly Dictionary<Type, IBinarySerializerTypeSelector> _customSelectorMap = new Dictionary<Type, IBinarySerializerTypeSelector>();
        
        private static readonly Dictionary<Type, GenericTypeInfo> _genericColletionTypeInfoMap = new Dictionary<Type, GenericTypeInfo>();

        private static readonly Dictionary<Type, MethodInfo> _collectionAddMethodCache = new Dictionary<Type, MethodInfo>();

        static BinarySerializeTypeBuffer()
        {
            RegisterCustomSelector(new IntPtrSelector());
            RegisterCustomSelector(new UIntPtrSelector());
            RegisterCustomSelector(new GuidSelector());
            RegisterCustomSelector(new TimeSpanSelector());
            RegisterCustomSelector(new DateTimeOffsetSelector());
        }

        // 支持的ICollection类型/接口，以及实例化回退类型
        private static readonly Dictionary<Type, Type> _supportedCollection = new Dictionary<Type, Type>
        {
            { typeof(IList<>), typeof(List<>) },
            { typeof(IDictionary<,>), typeof(Dictionary<,>) },
            { typeof(ISet<>), typeof(HashSet<>) },
            { typeof(Queue<>), typeof(Queue<>) },
            { typeof(Stack<>), typeof(Stack<>) },
            { typeof(ICollection<>), typeof(List<>) },
        };
        
        public static bool IsSupportedType(Type type)
        {
            if (type.IsInterface)
            {
                var genericType = GetCollectionGenericTypeInfo(type).genericDefinition;
                return genericType != null;
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
                    .Where(f => (f.IsPublic && !f.IsNotSerialized)
# if UNITY_5_3_OR_NEWER
                                || f.IsDefined(typeof(UnityEngine.SerializeField))
# endif
                                )
                    .OrderBy(f => f.MetadataToken)
                    .ToArray();
                _fieldCache[type] = fields;
            }
            return fields;
        }

        public static GenericTypeInfo GetCollectionGenericTypeInfo(Type type)
        {
            if (_genericColletionTypeInfoMap.TryGetValue(type, out var info))
            {
                return info;
            }
            foreach (var keyValue in _supportedCollection)
            {
                var (genericType, fallback) = (keyValue.Key, keyValue.Value);
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
            _genericColletionTypeInfoMap[type] = info;
            return info;
        }

        public static MethodInfo GetCollectionAddMethod(Type collectionType, Type elementType, string methodName)
        {
            if (_collectionAddMethodCache.TryGetValue(collectionType, out var method))
            {
                return method;
            }
            method = collectionType.GetMethod((methodName), new[] { elementType });
            _collectionAddMethodCache[collectionType] = method;
            return method;
        }

        public delegate object CollectionReadDelegate(BinaryReader reader, object collection, Encoding encoding);
        private static readonly Dictionary<string, CollectionReadDelegate> _collectionReadDelegateCache= new Dictionary<string, CollectionReadDelegate>();

        public static CollectionReadDelegate GetCollectionReadDelegate(Type collectionKind, Type elementType)
        {
            string key = collectionKind.FullName + "|" + elementType.FullName;
            if (_collectionReadDelegateCache.TryGetValue(key, out var del))
                return del;

            MethodInfo openMethod;
            if (collectionKind == typeof(ISet<>))
                openMethod = typeof(BinaryDeserializeHandler).GetMethod("ReadSetGeneric", BindingFlags.NonPublic | BindingFlags.Static);
            else if (collectionKind == typeof(Queue<>))
                openMethod = typeof(BinaryDeserializeHandler).GetMethod("ReadQueueGeneric", BindingFlags.NonPublic | BindingFlags.Static);
            else if (collectionKind == typeof(Stack<>))
                openMethod = typeof(BinaryDeserializeHandler).GetMethod("ReadStackGeneric", BindingFlags.NonPublic | BindingFlags.Static);
            else
                throw new NotSupportedException();

            MethodInfo closedMethod = openMethod.MakeGenericMethod(elementType);
            del = (CollectionReadDelegate)Delegate.CreateDelegate(typeof(CollectionReadDelegate), closedMethod);
            _collectionReadDelegateCache[key] = del;
            return del;
        }

        public static void RegisterCustomSelector(IBinarySerializerTypeSelector selector)
        {
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));
            _customSelectorMap[selector.TargetType] = selector;
        }

        public static IBinarySerializerTypeSelector GetCustomSelector(Type type)
        {
            _customSelectorMap.TryGetValue(type, out var selector);
            return selector;
        }
    }
}