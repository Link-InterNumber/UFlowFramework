using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace PowerCellStudio
{
    public class BinaryDeserializeHandler
    {
        #region 读取核心逻辑

        public static object ReadValue(BinaryReader reader, Type type, Encoding encoding)
        {
            byte notNullFlag = reader.ReadByte();
            if (notNullFlag == 0)
                return null;

            var customSelector = BinarySerializeTypeBuffer.GetCustomSelector(type);
            if (customSelector != null)
                return customSelector.Read(reader, encoding);

#if UNITY_5_3_OR_NEWER
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                throw new NotSupportedException($"[BinaryDeserializeHandler] UnityEngine.Object 类型不支持直接反序列化。类型: {type}");
#endif

            if (type.IsEnum)
            {
                Type underlyingType = Enum.GetUnderlyingType(type);
                object underlyingValue = ReadValue(reader, underlyingType, encoding);
                return Enum.ToObject(type, underlyingValue);
            }

            if (type == typeof(IntPtr))
                return new IntPtr(reader.ReadInt64());

            if (type == typeof(UIntPtr))
                return new UIntPtr(reader.ReadUInt64());

            if (type == typeof(Guid))
                return new Guid(reader.ReadBytes(16));

            if (type == typeof(TimeSpan))
                return new TimeSpan(reader.ReadInt64());

            if (type == typeof(DateTimeOffset))
                return new DateTimeOffset(reader.ReadInt64(), new TimeSpan(reader.ReadInt64()));

            TypeCode typeCode = Type.GetTypeCode(type);
            switch (typeCode)
            {
                case TypeCode.Boolean: return reader.ReadBoolean();
                case TypeCode.Byte: return reader.ReadByte();
                case TypeCode.SByte: return reader.ReadSByte();
                case TypeCode.Int16: return reader.ReadInt16();
                case TypeCode.UInt16: return reader.ReadUInt16();
                case TypeCode.Int32: return reader.ReadInt32();
                case TypeCode.UInt32: return reader.ReadUInt32();
                case TypeCode.Int64: return reader.ReadInt64();
                case TypeCode.UInt64: return reader.ReadUInt64();
                case TypeCode.Single: return reader.ReadSingle();
                case TypeCode.Double: return reader.ReadDouble();
                case TypeCode.Decimal: return reader.ReadDecimal();
                case TypeCode.Char: return reader.ReadChar();
                case TypeCode.String:
                    return ReadString(reader, encoding);
                case TypeCode.DateTime:
                    return DateTime.FromBinary(reader.ReadInt64());
                case TypeCode.Object:
                    return ReadObject(reader, type, encoding);
                default:
                    throw new NotSupportedException($"Unsupported type: {type}");
            }
        }

        private static string ReadString(BinaryReader reader, Encoding encoding)
        {
            int length = reader.ReadInt32();
            if (length < 0) return null;
            byte[] bytes = reader.ReadBytes(length);
            return encoding.GetString(bytes);
        }

        private static object ReadObject(BinaryReader reader, Type type, Encoding encoding)
        {
            if (type.IsArray)
                return ReadArray(reader, type, encoding);

            if (TryResolveCollectionType(type, typeof(IList<>), typeof(List<>), out Type listConcreteType, out Type[] listArgs))
                return ReadList(reader, listConcreteType, listArgs[0], encoding);

            if (TryResolveCollectionType(type, typeof(IDictionary<,>), typeof(Dictionary<,>), out Type dictionaryConcreteType, out Type[] dictionaryArgs))
                return ReadDictionary(reader, dictionaryConcreteType, dictionaryArgs, encoding);

            if (TryResolveCollectionType(type, typeof(ISet<>), typeof(HashSet<>), out Type setConcreteType, out Type[] setArgs))
                return ReadSet(reader, setConcreteType, setArgs[0], encoding);

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Queue<>))
                return ReadQueue(reader, type, encoding);

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Stack<>))
                return ReadStack(reader, type, encoding);

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                return ReadKeyValuePair(reader, type, encoding);
            

            // 普通对象
            return ReadFields(reader, type, encoding);
        }

        private static object ReadArray(BinaryReader reader, Type arrayType, Encoding encoding)
        {
            int length = reader.ReadInt32();
            Type elementType = arrayType.GetElementType();
            Array array = Array.CreateInstance(elementType, length);
            for (int i = 0; i < length; i++)
            {
                array.SetValue(ReadValue(reader, elementType, encoding), i);
            }

            return array;
        }

        private static object ReadList(BinaryReader reader, Type listType, Type elementType, Encoding encoding)
        {
            int count = reader.ReadInt32();
            IList list = CreateObjectInstance(listType) as IList;
            if (list == null)
                throw new NotSupportedException($"[BinaryDeserializeHandler] 类型未实现 IList，无法作为列表反序列化。类型: {listType}");

            for (int i = 0; i < count; i++)
            {
                list.Add(ReadValue(reader, elementType, encoding));
            }

            return list;
        }

        private static object ReadDictionary(BinaryReader reader, Type dictType, Type[] args, Encoding encoding)
        {
            int count = reader.ReadInt32();
            Type keyType = args[0];
            Type valueType = args[1];
            IDictionary dict = CreateObjectInstance(dictType) as IDictionary;
            if (dict == null)
                throw new NotSupportedException($"[BinaryDeserializeHandler] 类型未实现 IDictionary，无法作为字典反序列化。类型: {dictType}");

            for (int i = 0; i < count; i++)
            {
                object key = ReadValue(reader, keyType, encoding);
                object value = ReadValue(reader, valueType, encoding);
                dict.Add(key, value);
            }

            return dict;
        }

        private static object ReadSet(BinaryReader reader, Type setType, Type elementType, Encoding encoding)
        {
            int count = reader.ReadInt32();
            object set = CreateObjectInstance(setType);
            MethodInfo addMethod = setType.GetMethod("Add", new[] { elementType });
            if (addMethod == null)
                throw new NotSupportedException($"[BinaryDeserializeHandler] 集合类型缺少 Add 方法，无法作为集合反序列化。类型: {setType}");

            for (int i = 0; i < count; i++)
            {
                addMethod.Invoke(set, new[] { ReadValue(reader, elementType, encoding) });
            }

            return set;
        }

        private static object ReadQueue(BinaryReader reader, Type queueType, Encoding encoding)
        {
            int count = reader.ReadInt32();
            Type elementType = queueType.GetGenericArguments()[0];
            object queue = CreateObjectInstance(queueType);
            MethodInfo enqueueMethod = queueType.GetMethod("Enqueue", new[] { elementType });
            if (enqueueMethod == null)
                throw new NotSupportedException($"[BinaryDeserializeHandler] 集合类型缺少 Enqueue 方法，无法作为队列反序列化。类型: {queueType}");

            for (int i = 0; i < count; i++)
            {
                enqueueMethod.Invoke(queue, new[] { ReadValue(reader, elementType, encoding) });
            }

            return queue;
        }

        private static object ReadStack(BinaryReader reader, Type stackType, Encoding encoding)
        {
            int count = reader.ReadInt32();
            Type elementType = stackType.GetGenericArguments()[0];
            object stack = CreateObjectInstance(stackType);
            MethodInfo pushMethod = stackType.GetMethod("Push", new[] { elementType });
            if (pushMethod == null)
                throw new NotSupportedException($"[BinaryDeserializeHandler] 集合类型缺少 Push 方法，无法作为栈反序列化。类型: {stackType}");

            for (int i = 0; i < count; i++)
            {
                pushMethod.Invoke(stack, new[] { ReadValue(reader, elementType, encoding) });
            }

            return stack;
        }

        private static object ReadKeyValuePair(BinaryReader reader, Type pairType, Encoding encoding)
        {
            Type[] genericArgs = pairType.GetGenericArguments();
            object key = ReadValue(reader, genericArgs[0], encoding);
            object value = ReadValue(reader, genericArgs[1], encoding);
            ConstructorInfo ctor = pairType.GetConstructor(genericArgs);
            if (ctor == null)
                throw new NotSupportedException($"[BinaryDeserializeHandler] KeyValuePair 缺少可调用构造函数。类型: {pairType}");

            return ctor.Invoke(new[] { key, value });
        }

        private static object ReadFields(BinaryReader reader, Type type, Encoding encoding)
        {
            object obj = CreateObjectInstance(type);
            FieldInfo[] fields = BinarySerializeTypeBuffer.GetSerializableFields(type);
            foreach (FieldInfo field in fields)
            {
                object fieldValue = ReadValue(reader, field.FieldType, encoding);
                field.SetValue(obj, fieldValue);
            }

            return obj;
        }

        private static object CreateObjectInstance(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);

            ConstructorInfo ctor = type.GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null);

            if (ctor != null && !ctor.IsPrivate)
                return ctor.Invoke(null);

            if (type.IsSerializable)
                return FormatterServices.GetUninitializedObject(type);

            throw new NotSupportedException(
                $"[BinaryDeserializeHandler] 类型缺少可调用的无参构造函数，且未显式标记为 [Serializable]，拒绝使用未初始化对象回退。类型: {type}");
        }

        private static bool TryResolveCollectionType(Type type, Type interfaceTypeDefinition, Type defaultConcreteTypeDefinition, out Type concreteType, out Type[] genericArgs)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == interfaceTypeDefinition)
            {
                genericArgs = type.GetGenericArguments();
                concreteType = defaultConcreteTypeDefinition.MakeGenericType(genericArgs);
                return true;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == defaultConcreteTypeDefinition)
            {
                genericArgs = type.GetGenericArguments();
                concreteType = type;
                return true;
            }

            Type[] interfaces = type.GetInterfaces();
            for (int i = 0; i < interfaces.Length; i++)
            {
                Type interfaceType = interfaces[i];
                if (!interfaceType.IsGenericType || interfaceType.GetGenericTypeDefinition() != interfaceTypeDefinition)
                    continue;

                genericArgs = interfaceType.GetGenericArguments();
                concreteType = type.IsInterface ? defaultConcreteTypeDefinition.MakeGenericType(genericArgs) : type;
                return true;
            }

            concreteType = null;
            genericArgs = null;
            return false;
        }

        #endregion
    }
}