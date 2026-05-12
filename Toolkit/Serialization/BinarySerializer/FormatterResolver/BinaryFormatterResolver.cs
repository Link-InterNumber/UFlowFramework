using System;
using System.Collections.Generic;
using System.Reflection;

namespace PowerCellStudio
{
    internal static class BinaryFormatterResolver
    {
        private static readonly Dictionary<Type, IBinaryFormatter> FormatterCache = new Dictionary<Type, IBinaryFormatter>();

        public static IBinaryFormatter<T> GetFormatter<T>()
        {
            return BinaryFormatterResolverCache<T>.Instance;
        }

        public static IBinaryFormatter GetFormatter(Type type)
        {
            if (FormatterCache.TryGetValue(type, out var formatter))
                return formatter;

            formatter = CreateFormatter(type);
            FormatterCache[type] = formatter;
            return formatter;
        }

        private static IBinaryFormatter CreateFormatter(Type type)
        {
            if (typeof(IBinaryData).IsAssignableFrom(type))
            {
                MethodInfo getCreatorMethod = typeof(BinarySerializeTypeBuffer)
                    .GetMethod(nameof(BinarySerializeTypeBuffer.GetTypedCreator), BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null)
                    .MakeGenericMethod(type);
                object builder = getCreatorMethod.Invoke(null, null);
                var wrappedBuilder = typeof(BinaryDataTypeSelector<>).MakeGenericType(type);
                return (IBinaryFormatter)Activator.CreateInstance(wrappedBuilder, builder);
            }

            var customSelector = BinarySerializeTypeBuffer.GetCustomSelector(type);
            if (customSelector != null)
                return (IBinaryFormatter)Activator.CreateInstance(typeof(CustomSelectorFormatter<>).MakeGenericType(type), customSelector);

            if (type.IsEnum)
                return (IBinaryFormatter)Activator.CreateInstance(typeof(EnumFormatter<>).MakeGenericType(type));

            if (type == typeof(bool)) return new BooleanFormatter();
            if (type == typeof(byte)) return new ByteFormatter();
            if (type == typeof(sbyte)) return new SByteFormatter();
            if (type == typeof(short)) return new Int16Formatter();
            if (type == typeof(ushort)) return new UInt16Formatter();
            if (type == typeof(int)) return new Int32Formatter();
            if (type == typeof(uint)) return new UInt32Formatter();
            if (type == typeof(long)) return new Int64Formatter();
            if (type == typeof(ulong)) return new UInt64Formatter();
            if (type == typeof(float)) return new SingleFormatter();
            if (type == typeof(double)) return new DoubleFormatter();
            if (type == typeof(decimal)) return new DecimalFormatter();
            if (type == typeof(char)) return new CharFormatter();
            if (type == typeof(string)) return new StringFormatter();
            if (type == typeof(DateTime)) return new DateTimeFormatter();

            if (type.IsArray)
            {
                return (IBinaryFormatter)Activator.CreateInstance(typeof(ArrayFormatter<>).MakeGenericType(type.GetElementType()));
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                return (IBinaryFormatter)Activator.CreateInstance(typeof(KeyValuePairFormatter<,>).MakeGenericType(type.GetGenericArguments()));
            }

            var collectionTypeInfo = BinarySerializeTypeBuffer.GetCollectionGenericTypeInfo(type);
            if (collectionTypeInfo.genericDefinition != null)
            {
                if (collectionTypeInfo.genericDefinition == typeof(IList<>) || collectionTypeInfo.genericDefinition == typeof(ICollection<>))
                {
                    return (IBinaryFormatter)Activator.CreateInstance(
                        typeof(ListCollectionFormatter<,>).MakeGenericType(type, collectionTypeInfo.genericArguments[0]),
                        CreateTypedCreator(type, collectionTypeInfo.resolvedType));
                }

                if (collectionTypeInfo.genericDefinition == typeof(IDictionary<,>))
                {
                    return (IBinaryFormatter)Activator.CreateInstance(
                        typeof(DictionaryCollectionFormatter<,,>).MakeGenericType(type, collectionTypeInfo.genericArguments[0], collectionTypeInfo.genericArguments[1]),
                        CreateTypedCreator(type, collectionTypeInfo.resolvedType));
                }

                if (collectionTypeInfo.genericDefinition == typeof(ISet<>))
                {
                    return (IBinaryFormatter)Activator.CreateInstance(
                        typeof(SetCollectionFormatter<,>).MakeGenericType(type, collectionTypeInfo.genericArguments[0]),
                        CreateTypedCreator(type, collectionTypeInfo.resolvedType));
                }

                if (collectionTypeInfo.genericDefinition == typeof(Queue<>))
                {
                    return (IBinaryFormatter)Activator.CreateInstance(
                        typeof(QueueCollectionFormatter<,>).MakeGenericType(type, collectionTypeInfo.genericArguments[0]),
                        CreateTypedCreator(type, collectionTypeInfo.resolvedType));
                }

                if (collectionTypeInfo.genericDefinition == typeof(Stack<>))
                {
                    return (IBinaryFormatter)Activator.CreateInstance(
                        typeof(StackCollectionFormatter<,>).MakeGenericType(type, collectionTypeInfo.genericArguments[0]),
                        CreateTypedCreator(type, collectionTypeInfo.resolvedType));
                }
            }

            return (IBinaryFormatter)Activator.CreateInstance(typeof(ObjectFormatter<>).MakeGenericType(type));
        }

        private static object CreateTypedCreator(Type targetType, Type concreteType)
        {
            MethodInfo method = typeof(BinarySerializeTypeBuffer)
                .GetMethod(nameof(BinarySerializeTypeBuffer.GetTypedCreator), new[] { typeof(Type) })
                .MakeGenericMethod(targetType);
            return method.Invoke(null, new object[] { concreteType });
        }
    }
}