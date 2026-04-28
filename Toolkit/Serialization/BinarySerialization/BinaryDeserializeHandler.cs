using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace PowerCellStudio
{
    internal class BinaryDeserializeHandler
    {
        #region 读取核心逻辑

        public static object ReadValue(BinaryReader reader, Type type, Encoding encoding)
        {
            if (type.IsEnum)
            {
                Type underlyingType = Enum.GetUnderlyingType(type);
                object underlyingValue = ReadValue(reader, underlyingType, encoding);
                return Enum.ToObject(type, underlyingValue);
            }

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
            byte notNullFlag = reader.ReadByte();
            if (notNullFlag == 0)
                return null;

            var customSelector = BinarySerializeTypeBuffer.GetCustomSelector(type);
            if (customSelector != null)
                return customSelector.Read(reader, encoding);

            if (type.IsArray)
                return ReadArray(reader, type, encoding);

            if (!BinarySerializeTypeBuffer.IsSupportedType(type))
                throw new NotSupportedException($"[BinaryDeserializeHandler] 不支持的类型，无法反序列化。类型: {type}");

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                return ReadKeyValuePair(reader, type, encoding);

            var collectionTypeInfo = BinarySerializeTypeBuffer.GetCollectionGenericTypeInfo(type);
            if (collectionTypeInfo.genericDefinition != null)
            {
                var obj = ReadFields(reader, collectionTypeInfo.resolvedType, encoding);
                
                if (collectionTypeInfo.genericDefinition == typeof(IList<>))
                    return ReadList(reader, obj as IList, collectionTypeInfo.genericArguments[0], encoding);

                if (collectionTypeInfo.genericDefinition == typeof(IDictionary<,>))
                    return ReadDictionary(reader, obj as IDictionary, collectionTypeInfo.genericArguments, encoding);

                if (collectionTypeInfo.genericDefinition == typeof(ISet<>))
                    return ReadSet(reader, obj, collectionTypeInfo.resolvedType, collectionTypeInfo.genericArguments[0], encoding);

                if (collectionTypeInfo.genericDefinition == typeof(Queue<>))
                    return ReadQueue(reader, obj, collectionTypeInfo.resolvedType, collectionTypeInfo.genericArguments[0], encoding);

                if (collectionTypeInfo.genericDefinition == typeof(Stack<>))
                    return ReadStack(reader, obj, collectionTypeInfo.resolvedType, collectionTypeInfo.genericArguments[0], encoding);
                return obj;
            }
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

        private static object ReadList(BinaryReader reader, IList list, Type elementType, Encoding encoding)
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                list.Add(ReadValue(reader, elementType, encoding));
            }

            return list;
        }

        private static object ReadDictionary(BinaryReader reader, IDictionary dict, Type[] args, Encoding encoding)
        {
            int count = reader.ReadInt32();
            Type keyType = args[0];
            Type valueType = args[1];
            for (int i = 0; i < count; i++)
            {
                object key = ReadValue(reader, keyType, encoding);
                object value = ReadValue(reader, valueType, encoding);
                dict.Add(key, value);
            }

            return dict;
        }

        private static object ReadSet(BinaryReader reader, object set, Type setType, Type elementType, Encoding encoding)
        {
            var readerDelegate = BinarySerializeTypeBuffer.GetCollectionReadDelegate(typeof(ISet<>), elementType);
            return readerDelegate(reader, set, encoding);
        }

        private static object ReadQueue(BinaryReader reader, object queue, Type queueType, Type elementType, Encoding encoding)
        {
            var readerDelegate = BinarySerializeTypeBuffer.GetCollectionReadDelegate(typeof(Queue<>), elementType);
            return readerDelegate(reader, queue, encoding);
        }

        private static object ReadStack(BinaryReader reader, object stack, Type stackType, Type elementType, Encoding encoding)
        {
            var readerDelegate = BinarySerializeTypeBuffer.GetCollectionReadDelegate(typeof(Stack<>), elementType);
            return readerDelegate(reader, stack, encoding);
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
            var layout = BinarySerializeTypeBuffer.GetSerializableFields(type);
            object obj = layout.CreateInstance();
            foreach (var field in layout.Fields)
            {
                object fieldValue = ReadValue(reader, field.FieldType, encoding);
                field.Field.SetValue(obj, fieldValue);
            }

            return obj;
        }

        #endregion

        #region 集合类型特殊处理

        private static object ReadSetGeneric<T>(BinaryReader reader, object collection, Encoding encoding)
        {
            int count = reader.ReadInt32();
            ISet<T> set = (ISet<T>)collection;
            for (int i = 0; i < count; i++)
            {
                set.Add((T)ReadValue(reader, typeof(T), encoding));
            }
            return set;
        }

        private static object ReadQueueGeneric<T>(BinaryReader reader, object collection, Encoding encoding)
        {
            int count = reader.ReadInt32();
            Queue<T> queue = (Queue<T>)collection;
            for (int i = 0; i < count; i++)
            {
                queue.Enqueue((T)ReadValue(reader, typeof(T), encoding));
            }
            return queue;
        }

        private static object ReadStackGeneric<T>(BinaryReader reader, object collection, Encoding encoding)
        {
            int count = reader.ReadInt32();
            Stack<T> stack = (Stack<T>)collection;
            for (int i = 0; i < count; i++)
            {
                stack.Push((T)ReadValue(reader, typeof(T), encoding));
            }
            return stack;
        }
        #endregion

        private static T CreateInstance<T>(Type type) where T : new()
        {
            return new T();
        }
    }
}