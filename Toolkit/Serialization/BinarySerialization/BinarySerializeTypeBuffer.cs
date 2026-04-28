using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
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

    internal sealed class TypeLayout
    {
        public Func<object> CreateInstance;
        public FieldAccessor[] Fields;
    }

    internal delegate void RefObjectSetter(ref object target, object value);
    
    internal sealed class FieldAccessor
    {
        // public string Name;
        public Type FieldType;
        public FieldInfo Field;
    }

    internal class BinarySerializeTypeBuffer
    {
        // 类型缓存
        private static readonly Dictionary<Type, TypeLayout> _fieldCache = new Dictionary<Type, TypeLayout>();
        
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

        public static TypeLayout GetSerializableFields(Type type)
        {
            if (_fieldCache.TryGetValue(type, out var layout))
                return layout;

            layout = BuildTypeLayout(type);
            _fieldCache[type] = layout;
            return layout;
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
        
        private static Func<object, object> BuildGetter(Type declaringType, FieldInfo field)
        {
            var objParam = Expression.Parameter(typeof(object), "obj");
            var typedObj = Expression.Convert(objParam, declaringType);
            var fieldExpr = Expression.Field(typedObj, field);
            var boxedField = Expression.Convert(fieldExpr, typeof(object));

            return Expression.Lambda<Func<object, object>>(boxedField, objParam).Compile();
        }

        private static TypeLayout BuildTypeLayout(Type type)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(f => (f.IsPublic && !f.IsNotSerialized)
# if UNITY_5_3_OR_NEWER
                                || f.IsDefined(typeof(UnityEngine.SerializeField))
# endif
                                )
                    .OrderBy(f => f.MetadataToken)
                    .ToArray();
            
            FieldAccessor[] accessors = new FieldAccessor[fields.Length];

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                accessors[i] = new FieldAccessor
                {
                    // Name = field.Name,
                    FieldType = field.FieldType,
                    Field = field
                };
            }

            return new TypeLayout
            {
                CreateInstance = BuildCreator(type),
                Fields = accessors
            };
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

        public delegate object CollectionReadDelegate(BinaryReader reader, object collection, Encoding encoding);
        private static readonly Dictionary<(Type, Type), CollectionReadDelegate> _collectionReadDelegateCache= new Dictionary<(Type, Type), CollectionReadDelegate>();

        public static CollectionReadDelegate GetCollectionReadDelegate(Type collectionKind, Type elementType)
        {
            (Type, Type) key = (collectionKind, elementType);
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