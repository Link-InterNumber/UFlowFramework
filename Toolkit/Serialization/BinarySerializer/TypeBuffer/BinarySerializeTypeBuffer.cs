using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace PowerCellStudio
{
    internal class BinarySerializeTypeBuffer
    {
        private static readonly Dictionary<Type, TypeLayout> FieldCache = new Dictionary<Type, TypeLayout>();
        private static readonly Dictionary<Type, Func<object>> CreatorCache = new Dictionary<Type, Func<object>>();
        private static readonly Dictionary<(Type, Type), MethodInfo> CollectionMethodCache = new Dictionary<(Type, Type), MethodInfo>();
        private static readonly Dictionary<Type, IBinarySerializerTypeSelector> CustomSelectorMap = new Dictionary<Type, IBinarySerializerTypeSelector>();
        private static readonly Dictionary<Type, GenericTypeInfo> GenericCollectionTypeInfoMap = new Dictionary<Type, GenericTypeInfo>();

        static BinarySerializeTypeBuffer()
        {
            RegisterCustomSelector(new IntPtrSelector());
            RegisterCustomSelector(new UIntPtrSelector());
            RegisterCustomSelector(new GuidSelector());
            RegisterCustomSelector(new TimeSpanSelector());
            RegisterCustomSelector(new DateTimeOffsetSelector());
        }

        private static readonly Dictionary<Type, Type> SupportedCollection = new Dictionary<Type, Type>
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
                return GetCollectionGenericTypeInfo(type).genericDefinition != null;
            }

            if (type.IsAbstract || type.ContainsGenericParameters)
                return false;

#if UNITY_5_3_OR_NEWER
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                return false;
            }
#endif
            return true;
        }

        public static TypeLayout GetSerializableFields(Type type)
        {
            if (FieldCache.TryGetValue(type, out var layout))
                return layout;

            layout = BuildTypeLayout(type);
            FieldCache[type] = layout;
            return layout;
        }

        public static Func<object> GetBoxedCreator(Type type)
        {
            if (CreatorCache.TryGetValue(type, out var creator))
                return creator;

            creator = BuildCreator(type);
            CreatorCache[type] = creator;
            return creator;
        }

        public static Func<T> GetTypedCreator<T>()
        {
            return BinarySerializeTypeBufferTypedCreatorCache<T>.Instance;
        }

        public static Func<TBase> GetTypedCreator<TBase>(Type concreteType)
        {
            if (concreteType == null)
                throw new ArgumentNullException(nameof(concreteType));

            return BinarySerializeTypeBufferTypedCreatorCache<TBase>.GetOrCreate(concreteType);
        }

        public static MethodInfo GetEnqueueMethod(Type collectionType, Type elementType)
        {
            return GetCollectionMethod(collectionType, elementType, "Enqueue");
        }

        public static MethodInfo GetPushMethod(Type collectionType, Type elementType)
        {
            return GetCollectionMethod(collectionType, elementType, "Push");
        }

        public static GenericTypeInfo GetCollectionGenericTypeInfo(Type type)
        {
            if (GenericCollectionTypeInfoMap.TryGetValue(type, out var info))
                return info;

            foreach (var entry in SupportedCollection)
            {
                Type genericType = entry.Key;
                Type fallback = entry.Value;
                if (genericType.IsInterface)
                {
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == genericType)
                    {
                        info = new GenericTypeInfo
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

                        info = new GenericTypeInfo
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

                        info = new GenericTypeInfo
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
                    break;
            }

            GenericCollectionTypeInfoMap[type] = info;
            return info;
        }

        public static void RegisterCustomSelector(IBinarySerializerTypeSelector selector)
        {
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            CustomSelectorMap[selector.TargetType] = selector;
        }

        public static IBinarySerializerTypeSelector GetCustomSelector(Type type)
        {
            CustomSelectorMap.TryGetValue(type, out var selector);
            return selector;
        }

        private static TypeLayout BuildTypeLayout(Type type)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => (f.IsPublic && !f.IsNotSerialized)
#if UNITY_5_3_OR_NEWER
                            || f.IsDefined(typeof(UnityEngine.SerializeField))
#endif
                            )
                .OrderBy(f => f.MetadataToken)
                .ToArray();

            var accessors = new FieldAccessor[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                accessors[i] = new FieldAccessor
                {
                    FieldType = fields[i].FieldType,
                    Field = fields[i]
                };
            }

            return new TypeLayout
            {
                CreateInstance = GetBoxedCreator(type),
                Fields = accessors
            };
        }

        private static Func<object> BuildCreator(Type type)
        {
            if (type.IsValueType)
                return () => Activator.CreateInstance(type);

            ConstructorInfo ctor = type.GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null);

            if (ctor != null && !ctor.IsPrivate)
                return () => ctor.Invoke(null);

            if (type.IsSerializable)
                return () => FormatterServices.GetUninitializedObject(type);

            throw new NotSupportedException($"[BinaryDeserializeHandler] 类型缺少可调用的无参构造函数，且未显式标记为 [Serializable]，拒绝使用未初始化对象回退。类型: {type}");
        }

        internal static Func<T> BuildTypedCreator<T>()
        {
            Type type = typeof(T);
            if (type.IsValueType)
                return () => default;

            ConstructorInfo ctor = type.GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null);

            if (ctor != null && !ctor.IsPrivate)
                return () => (T)ctor.Invoke(null);

            if (type.IsSerializable)
                return () => (T)FormatterServices.GetUninitializedObject(type);

            throw new NotSupportedException($"[BinaryDeserializeHandler] 类型缺少可调用的无参构造函数，且未显式标记为 [Serializable]，拒绝使用未初始化对象回退。类型: {type}");
        }
        private static MethodInfo GetCollectionMethod(Type collectionType, Type elementType, string methodName)
        {
            var key = (collectionType, elementType);
            if (CollectionMethodCache.TryGetValue(key, out var method))
                return method;

            method = collectionType.GetMethod(methodName, new[] { elementType });
            if (method == null)
                throw new NotSupportedException($"[BinarySerializeTypeBuffer] 集合类型缺少 {methodName} 方法。类型: {collectionType}");

            CollectionMethodCache[key] = method;
            return method;
        }
    }
}
