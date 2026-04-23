using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace PowerCellStudio
{
    internal class BinarySerializeHandler
    {
        #region 写入核心逻辑

        public static void WriteValue(BinaryWriter writer, object value, Type type, Encoding encoding)
        {
            // 处理 null 值
            if (value == null)
            {
                writer.Write((byte)0); // 标记为 null
                return;
            }

            writer.Write((byte)1); // 标记为非 null

            var customSelector = BinarySerializeTypeBuffer.GetCustomSelector(type);
            if (customSelector != null)
            {
                customSelector.Write(writer, value, encoding);
                return;
            }

#if UNITY_5_3_OR_NEWER
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                throw new NotSupportedException($"[BinarySerializeHandler] UnityEngine.Object 类型不支持直接序列化。类型: {type}");
#endif

            if (type.IsEnum)
            {
                Type underlyingType = Enum.GetUnderlyingType(type);
                WriteValue(writer, Convert.ChangeType(value, underlyingType), underlyingType, encoding);
                return;
            }

            TypeCode typeCode = Type.GetTypeCode(type);

            // 处理基元类型
            switch (typeCode)
            {
                case TypeCode.Boolean: writer.Write((bool)value); break;
                case TypeCode.Byte: writer.Write((byte)value); break;
                case TypeCode.SByte: writer.Write((sbyte)value); break;
                case TypeCode.Int16: writer.Write((short)value); break;
                case TypeCode.UInt16: writer.Write((ushort)value); break;
                case TypeCode.Int32: writer.Write((int)value); break;
                case TypeCode.UInt32: writer.Write((uint)value); break;
                case TypeCode.Int64: writer.Write((long)value); break;
                case TypeCode.UInt64: writer.Write((ulong)value); break;
                case TypeCode.Single: writer.Write((float)value); break;
                case TypeCode.Double: writer.Write((double)value); break;
                case TypeCode.Decimal: writer.Write((decimal)value); break;
                case TypeCode.Char: writer.Write((char)value); break;
                case TypeCode.String:
                    WriteString(writer, (string)value, encoding);
                    break;
                case TypeCode.DateTime:
                    writer.Write(((DateTime)value).ToBinary());
                    break;
                case TypeCode.Object:
                    WriteObject(writer, value, type, encoding);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported type: {type}");
            }
        }

        private static void WriteString(BinaryWriter writer, string str, Encoding encoding)
        {
            if (str == null)
            {
                writer.Write(-1); // 用负长度表示 null
                return;
            }

            byte[] bytes = encoding.GetBytes(str);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        private static void WriteObject(BinaryWriter writer, object obj, Type type, Encoding encoding)
        {
            // 处理数组
            if (type.IsArray)
            {
                WriteArray(writer, (Array)obj, type.GetElementType(), encoding);
                return;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                WriteKeyValuePair(writer, obj, type.GetGenericArguments(), encoding);
                return;
            }

            var collectionTypeInfo = BinarySerializeTypeBuffer.GetCollectionGenericTypeInfo(type);
            if (collectionTypeInfo.genericDefinition != null)
            {
                if (collectionTypeInfo.genericDefinition == typeof(IList<>))
                {
                    WriteList(writer, (IList)obj, collectionTypeInfo.genericArguments[0], encoding);
                    return;
                }
                if (collectionTypeInfo.genericDefinition == typeof(IDictionary<,>))
                {
                    WriteDictionary(writer, (IDictionary)obj, collectionTypeInfo.genericArguments, encoding);
                    return;
                }
                if (collectionTypeInfo.genericDefinition == typeof(ISet<>))
                {
                    WriteEnumerable(writer, (IEnumerable)obj, type, collectionTypeInfo.genericArguments[0], encoding);
                    return;
                }

                if (collectionTypeInfo.genericDefinition == typeof(Queue<>))
                {
                    WriteEnumerable(writer, (IEnumerable)obj, type, collectionTypeInfo.genericArguments[0], encoding);
                    return;
                }

                if (collectionTypeInfo.genericDefinition == typeof(Stack<>))
                {
                    WriteStack(writer, (IEnumerable)obj, collectionTypeInfo.genericArguments[0], encoding);
                    return;
                }
            }

            // 处理普通类/结构体：遍历所有字段
            WriteFields(writer, obj, type, encoding);
        }

        private static void WriteArray(BinaryWriter writer, Array array, Type elementType, Encoding encoding)
        {
            writer.Write(array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                WriteValue(writer, array.GetValue(i), elementType, encoding);
            }
        }

        private static void WriteList(BinaryWriter writer, IList list, Type elementType, Encoding encoding)
        {
            writer.Write(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                WriteValue(writer, list[i], elementType, encoding);
            }
        }

        private static void WriteDictionary(BinaryWriter writer, IDictionary dict, Type[] genericArgs, Encoding encoding)
        {
            writer.Write(dict.Count);
            Type keyType = genericArgs[0];
            Type valueType = genericArgs[1];
            foreach (DictionaryEntry entry in dict)
            {
                WriteValue(writer, entry.Key, keyType, encoding);
                WriteValue(writer, entry.Value, valueType, encoding);
            }
        }

        private static void WriteEnumerable(BinaryWriter writer, IEnumerable enumerable, Type collectionType, Type elementType, Encoding encoding)
        {
            int count = GetCount(collectionType, enumerable);
            writer.Write(count);
            foreach (object element in enumerable)
            {
                WriteValue(writer, element, elementType, encoding);
            }
        }

        private static void WriteStack(BinaryWriter writer, IEnumerable enumerable, Type elementType, Encoding encoding)
        {
            List<object> elements = new List<object>();
            foreach (object element in enumerable)
            {
                elements.Add(element);
            }

            writer.Write(elements.Count);
            for (int i = elements.Count - 1; i >= 0; i--)
            {
                WriteValue(writer, elements[i], elementType, encoding);
            }
        }

        private static void WriteKeyValuePair(BinaryWriter writer, object obj, Type[] genericArgs, Encoding encoding)
        {
            PropertyInfo keyProperty = obj.GetType().GetProperty("Key");
            PropertyInfo valueProperty = obj.GetType().GetProperty("Value");
            WriteValue(writer, keyProperty.GetValue(obj, null), genericArgs[0], encoding);
            WriteValue(writer, valueProperty.GetValue(obj, null), genericArgs[1], encoding);
        }

        private static int GetCount(Type collectionType, object collection)
        {
            if (collection is ICollection nonGenericCollection)
                return nonGenericCollection.Count;

            Type runtimeType = collection.GetType();
            PropertyInfo countProperty = runtimeType.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance)
                ?? collectionType.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
            if (countProperty == null)
                throw new NotSupportedException($"[BinarySerializeHandler] 集合类型缺少 Count 属性，无法序列化。声明类型: {collectionType}, 运行时类型: {runtimeType}");

            return (int)countProperty.GetValue(collection, null);
        }

        private static void WriteFields(BinaryWriter writer, object obj, Type type, Encoding encoding)
        {
            FieldInfo[] fields = BinarySerializeTypeBuffer.GetSerializableFields(type);
            foreach (FieldInfo field in fields)
            {
                object fieldValue = field.GetValue(obj);
                WriteValue(writer, fieldValue, field.FieldType, encoding);
            }
        }
        #endregion
    }
}