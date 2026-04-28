using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace PowerCellStudio
{
    internal interface IBinaryFormatter
    {
        Type TargetType { get; }

        void Write(BinaryWriter writer, object value, Encoding encoding);

        object Read(BinaryReader reader, Encoding encoding);
    }

    internal interface IBinaryFormatter<T> : IBinaryFormatter
    {
        void Write(BinaryWriter writer, T value, Encoding encoding);

        new T Read(BinaryReader reader, Encoding encoding);
    }

    internal abstract class BinaryFormatterBase<T> : IBinaryFormatter<T>
    {
        public Type TargetType => typeof(T);

        public abstract void Write(BinaryWriter writer, T value, Encoding encoding);

        public abstract T Read(BinaryReader reader, Encoding encoding);

        void IBinaryFormatter.Write(BinaryWriter writer, object value, Encoding encoding)
        {
            Write(writer, (T)value, encoding);
        }

        object IBinaryFormatter.Read(BinaryReader reader, Encoding encoding)
        {
            return Read(reader, encoding);
        }
    }

    internal static class BinaryFormatterResolver
    {
        private static readonly Dictionary<Type, IBinaryFormatter> FormatterCache = new Dictionary<Type, IBinaryFormatter>();

        public static IBinaryFormatter<T> GetFormatter<T>()
        {
            return Cache<T>.Instance;
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
                        BinarySerializeTypeBuffer.GetCreator(collectionTypeInfo.resolvedType));
                }

                if (collectionTypeInfo.genericDefinition == typeof(IDictionary<,>))
                {
                    return (IBinaryFormatter)Activator.CreateInstance(
                        typeof(DictionaryCollectionFormatter<,,>).MakeGenericType(type, collectionTypeInfo.genericArguments[0], collectionTypeInfo.genericArguments[1]),
                        BinarySerializeTypeBuffer.GetCreator(collectionTypeInfo.resolvedType));
                }

                if (collectionTypeInfo.genericDefinition == typeof(ISet<>))
                {
                    return (IBinaryFormatter)Activator.CreateInstance(
                        typeof(SetCollectionFormatter<,>).MakeGenericType(type, collectionTypeInfo.genericArguments[0]),
                        BinarySerializeTypeBuffer.GetCreator(collectionTypeInfo.resolvedType));
                }

                if (collectionTypeInfo.genericDefinition == typeof(Queue<>))
                {
                    return (IBinaryFormatter)Activator.CreateInstance(
                        typeof(QueueCollectionFormatter<,>).MakeGenericType(type, collectionTypeInfo.genericArguments[0]),
                        BinarySerializeTypeBuffer.GetCreator(collectionTypeInfo.resolvedType));
                }

                if (collectionTypeInfo.genericDefinition == typeof(Stack<>))
                {
                    return (IBinaryFormatter)Activator.CreateInstance(
                        typeof(StackCollectionFormatter<,>).MakeGenericType(type, collectionTypeInfo.genericArguments[0]),
                        BinarySerializeTypeBuffer.GetCreator(collectionTypeInfo.resolvedType));
                }
            }

            return (IBinaryFormatter)Activator.CreateInstance(typeof(ObjectFormatter<>).MakeGenericType(type));
        }

        private static class Cache<T>
        {
            internal static readonly IBinaryFormatter<T> Instance = (IBinaryFormatter<T>)GetFormatter(typeof(T));
        }
    }

    internal sealed class BooleanFormatter : BinaryFormatterBase<bool>
    {
        public override void Write(BinaryWriter writer, bool value, Encoding encoding) => writer.Write(value);
        public override bool Read(BinaryReader reader, Encoding encoding) => reader.ReadBoolean();
    }

    internal sealed class ByteFormatter : BinaryFormatterBase<byte>
    {
        public override void Write(BinaryWriter writer, byte value, Encoding encoding) => writer.Write(value);
        public override byte Read(BinaryReader reader, Encoding encoding) => reader.ReadByte();
    }

    internal sealed class SByteFormatter : BinaryFormatterBase<sbyte>
    {
        public override void Write(BinaryWriter writer, sbyte value, Encoding encoding) => writer.Write(value);
        public override sbyte Read(BinaryReader reader, Encoding encoding) => reader.ReadSByte();
    }

    internal sealed class Int16Formatter : BinaryFormatterBase<short>
    {
        public override void Write(BinaryWriter writer, short value, Encoding encoding) => writer.Write(value);
        public override short Read(BinaryReader reader, Encoding encoding) => reader.ReadInt16();
    }

    internal sealed class UInt16Formatter : BinaryFormatterBase<ushort>
    {
        public override void Write(BinaryWriter writer, ushort value, Encoding encoding) => writer.Write(value);
        public override ushort Read(BinaryReader reader, Encoding encoding) => reader.ReadUInt16();
    }

    internal sealed class Int32Formatter : BinaryFormatterBase<int>
    {
        public override void Write(BinaryWriter writer, int value, Encoding encoding) => writer.Write(value);
        public override int Read(BinaryReader reader, Encoding encoding) => reader.ReadInt32();
    }

    internal sealed class UInt32Formatter : BinaryFormatterBase<uint>
    {
        public override void Write(BinaryWriter writer, uint value, Encoding encoding) => writer.Write(value);
        public override uint Read(BinaryReader reader, Encoding encoding) => reader.ReadUInt32();
    }

    internal sealed class Int64Formatter : BinaryFormatterBase<long>
    {
        public override void Write(BinaryWriter writer, long value, Encoding encoding) => writer.Write(value);
        public override long Read(BinaryReader reader, Encoding encoding) => reader.ReadInt64();
    }

    internal sealed class UInt64Formatter : BinaryFormatterBase<ulong>
    {
        public override void Write(BinaryWriter writer, ulong value, Encoding encoding) => writer.Write(value);
        public override ulong Read(BinaryReader reader, Encoding encoding) => reader.ReadUInt64();
    }

    internal sealed class SingleFormatter : BinaryFormatterBase<float>
    {
        public override void Write(BinaryWriter writer, float value, Encoding encoding) => writer.Write(value);
        public override float Read(BinaryReader reader, Encoding encoding) => reader.ReadSingle();
    }

    internal sealed class DoubleFormatter : BinaryFormatterBase<double>
    {
        public override void Write(BinaryWriter writer, double value, Encoding encoding) => writer.Write(value);
        public override double Read(BinaryReader reader, Encoding encoding) => reader.ReadDouble();
    }

    internal sealed class DecimalFormatter : BinaryFormatterBase<decimal>
    {
        public override void Write(BinaryWriter writer, decimal value, Encoding encoding) => writer.Write(value);
        public override decimal Read(BinaryReader reader, Encoding encoding) => reader.ReadDecimal();
    }

    internal sealed class CharFormatter : BinaryFormatterBase<char>
    {
        public override void Write(BinaryWriter writer, char value, Encoding encoding) => writer.Write(value);
        public override char Read(BinaryReader reader, Encoding encoding) => reader.ReadChar();
    }

    internal sealed class StringFormatter : BinaryFormatterBase<string>
    {
        public override void Write(BinaryWriter writer, string value, Encoding encoding)
        {
            if (value == null)
            {
                writer.Write(-1);
                return;
            }

            byte[] bytes = encoding.GetBytes(value);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        public override string Read(BinaryReader reader, Encoding encoding)
        {
            int length = reader.ReadInt32();
            if (length < 0)
                return null;

            return encoding.GetString(reader.ReadBytes(length));
        }
    }

    internal sealed class DateTimeFormatter : BinaryFormatterBase<DateTime>
    {
        public override void Write(BinaryWriter writer, DateTime value, Encoding encoding) => writer.Write(value.ToBinary());
        public override DateTime Read(BinaryReader reader, Encoding encoding) => DateTime.FromBinary(reader.ReadInt64());
    }

    internal sealed class EnumFormatter<T> : BinaryFormatterBase<T>
        where T : struct, Enum
    {
        private readonly Type _underlyingType = Enum.GetUnderlyingType(typeof(T));
        private readonly TypeCode _underlyingTypeCode;
        private readonly IBinaryFormatter _underlyingFormatter;

        public EnumFormatter()
        {
            _underlyingTypeCode = Type.GetTypeCode(_underlyingType);
            _underlyingFormatter = BinaryFormatterResolver.GetFormatter(_underlyingType);
        }

        public override void Write(BinaryWriter writer, T value, Encoding encoding)
        {
            switch (_underlyingTypeCode)
            {
                case TypeCode.Byte:
                    writer.Write(EnumUnsafeCaster<T, byte>.Cast(value));
                    return;
                case TypeCode.SByte:
                    writer.Write(EnumUnsafeCaster<T, sbyte>.Cast(value));
                    return;
                case TypeCode.Int16:
                    writer.Write(EnumUnsafeCaster<T, short>.Cast(value));
                    return;
                case TypeCode.UInt16:
                    writer.Write(EnumUnsafeCaster<T, ushort>.Cast(value));
                    return;
                case TypeCode.Int32:
                    writer.Write(EnumUnsafeCaster<T, int>.Cast(value));
                    return;
                case TypeCode.UInt32:
                    writer.Write(EnumUnsafeCaster<T, uint>.Cast(value));
                    return;
                case TypeCode.Int64:
                    writer.Write(EnumUnsafeCaster<T, long>.Cast(value));
                    return;
                case TypeCode.UInt64:
                    writer.Write(EnumUnsafeCaster<T, ulong>.Cast(value));
                    return;
                default:
                    _underlyingFormatter.Write(writer, value, encoding);
                    return;
            }
        }

        public override T Read(BinaryReader reader, Encoding encoding)
        {
            return (T)Enum.ToObject(typeof(T), _underlyingFormatter.Read(reader, encoding));
        }
    }

    internal static class EnumUnsafeCaster<TEnum, TUnderlying>
        where TEnum : struct, Enum
        where TUnderlying : struct
    {
        public static TUnderlying Cast(TEnum value)
        {
            return Unsafe.As<TEnum, TUnderlying>(ref value);
        }
    }

    internal sealed class CustomSelectorFormatter<T> : BinaryFormatterBase<T>
    {
        private readonly IBinarySerializerTypeSelector _selector;

        public CustomSelectorFormatter(IBinarySerializerTypeSelector selector)
        {
            _selector = selector;
        }

        public override void Write(BinaryWriter writer, T value, Encoding encoding)
        {
            if (!typeof(T).IsValueType)
            {
                if (ReferenceEquals(value, null))
                {
                    writer.Write((byte)0);
                    return;
                }

                writer.Write((byte)1);
            }

            _selector.Write(writer, value, encoding);
        }

        public override T Read(BinaryReader reader, Encoding encoding)
        {
            if (!typeof(T).IsValueType && reader.ReadByte() == 0)
                return default;

            return (T)_selector.Read(reader, encoding);
        }
    }

    internal sealed class ArrayFormatter<TElement> : BinaryFormatterBase<TElement[]>
    {
        private readonly IBinaryFormatter<TElement> _elementFormatter = BinaryFormatterResolver.GetFormatter<TElement>();

        public override void Write(BinaryWriter writer, TElement[] value, Encoding encoding)
        {
            if (value == null)
            {
                writer.Write((byte)0);
                return;
            }

            writer.Write((byte)1);
            writer.Write(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                _elementFormatter.Write(writer, value[i], encoding);
            }
        }

        public override TElement[] Read(BinaryReader reader, Encoding encoding)
        {
            if (reader.ReadByte() == 0)
                return null;

            int length = reader.ReadInt32();
            var array = new TElement[length];
            for (int i = 0; i < length; i++)
            {
                array[i] = _elementFormatter.Read(reader, encoding);
            }

            return array;
        }
    }

    internal sealed class KeyValuePairFormatter<TKey, TValue> : BinaryFormatterBase<KeyValuePair<TKey, TValue>>
    {
        private readonly IBinaryFormatter<TKey> _keyFormatter = BinaryFormatterResolver.GetFormatter<TKey>();
        private readonly IBinaryFormatter<TValue> _valueFormatter = BinaryFormatterResolver.GetFormatter<TValue>();

        public override void Write(BinaryWriter writer, KeyValuePair<TKey, TValue> value, Encoding encoding)
        {
            _keyFormatter.Write(writer, value.Key, encoding);
            _valueFormatter.Write(writer, value.Value, encoding);
        }

        public override KeyValuePair<TKey, TValue> Read(BinaryReader reader, Encoding encoding)
        {
            return new KeyValuePair<TKey, TValue>(
                _keyFormatter.Read(reader, encoding),
                _valueFormatter.Read(reader, encoding));
        }
    }

    internal sealed class ListCollectionFormatter<TCollection, TElement> : BinaryFormatterBase<TCollection>
        where TCollection : class, IEnumerable<TElement>
    {
        private readonly Func<object> _creator;
        private readonly IBinaryFormatter<TElement> _elementFormatter = BinaryFormatterResolver.GetFormatter<TElement>();

        public ListCollectionFormatter(Func<object> creator)
        {
            _creator = creator;
        }

        public override void Write(BinaryWriter writer, TCollection value, Encoding encoding)
        {
            if (value == null)
            {
                writer.Write((byte)0);
                return;
            }

            writer.Write((byte)1);
            var collection = (ICollection<TElement>)value;
            writer.Write(collection.Count);
            foreach (var element in collection)
            {
                _elementFormatter.Write(writer, element, encoding);
            }
        }

        public override TCollection Read(BinaryReader reader, Encoding encoding)
        {
            if (reader.ReadByte() == 0)
                return default;

            int count = reader.ReadInt32();
            var collection = (ICollection<TElement>)_creator();
            for (int i = 0; i < count; i++)
            {
                collection.Add(_elementFormatter.Read(reader, encoding));
            }

            return (TCollection)collection;
        }
    }

    internal sealed class DictionaryCollectionFormatter<TCollection, TKey, TValue> : BinaryFormatterBase<TCollection>
        where TCollection : class, IDictionary<TKey, TValue>
    {
        private readonly Func<object> _creator;
        private readonly IBinaryFormatter<TKey> _keyFormatter = BinaryFormatterResolver.GetFormatter<TKey>();
        private readonly IBinaryFormatter<TValue> _valueFormatter = BinaryFormatterResolver.GetFormatter<TValue>();

        public DictionaryCollectionFormatter(Func<object> creator)
        {
            _creator = creator;
        }

        public override void Write(BinaryWriter writer, TCollection value, Encoding encoding)
        {
            if (value == null)
            {
                writer.Write((byte)0);
                return;
            }

            writer.Write((byte)1);
            writer.Write(value.Count);
            foreach (var entry in value)
            {
                _keyFormatter.Write(writer, entry.Key, encoding);
                _valueFormatter.Write(writer, entry.Value, encoding);
            }
        }

        public override TCollection Read(BinaryReader reader, Encoding encoding)
        {
            if (reader.ReadByte() == 0)
                return default;

            int count = reader.ReadInt32();
            var collection = (TCollection)_creator();
            for (int i = 0; i < count; i++)
            {
                collection.Add(_keyFormatter.Read(reader, encoding), _valueFormatter.Read(reader, encoding));
            }

            return collection;
        }
    }

    internal sealed class SetCollectionFormatter<TCollection, TElement> : BinaryFormatterBase<TCollection>
        where TCollection : class, ISet<TElement>
    {
        private readonly Func<object> _creator;
        private readonly IBinaryFormatter<TElement> _elementFormatter = BinaryFormatterResolver.GetFormatter<TElement>();

        public SetCollectionFormatter(Func<object> creator)
        {
            _creator = creator;
        }

        public override void Write(BinaryWriter writer, TCollection value, Encoding encoding)
        {
            if (value == null)
            {
                writer.Write((byte)0);
                return;
            }

            writer.Write((byte)1);
            writer.Write(value.Count);
            foreach (var element in value)
            {
                _elementFormatter.Write(writer, element, encoding);
            }
        }

        public override TCollection Read(BinaryReader reader, Encoding encoding)
        {
            if (reader.ReadByte() == 0)
                return default;

            int count = reader.ReadInt32();
            var collection = (TCollection)_creator();
            for (int i = 0; i < count; i++)
            {
                collection.Add(_elementFormatter.Read(reader, encoding));
            }

            return collection;
        }
    }

    internal sealed class QueueCollectionFormatter<TCollection, TElement> : BinaryFormatterBase<TCollection>
        where TCollection : class, IEnumerable<TElement>
    {
        private readonly Func<object> _creator;
        private readonly IBinaryFormatter<TElement> _elementFormatter = BinaryFormatterResolver.GetFormatter<TElement>();
        private readonly MethodInfo _enqueueMethod;

        public QueueCollectionFormatter(Func<object> creator)
        {
            _creator = creator;
            _enqueueMethod = BinarySerializeTypeBuffer.GetEnqueueMethod(typeof(TCollection), typeof(TElement));
        }

        public override void Write(BinaryWriter writer, TCollection value, Encoding encoding)
        {
            if (value == null)
            {
                writer.Write((byte)0);
                return;
            }

            writer.Write((byte)1);
            var collection = (ICollection)value;
            writer.Write(collection.Count);
            foreach (var element in value)
            {
                _elementFormatter.Write(writer, element, encoding);
            }
        }

        public override TCollection Read(BinaryReader reader, Encoding encoding)
        {
            if (reader.ReadByte() == 0)
                return default;

            int count = reader.ReadInt32();
            var queue = (TCollection)_creator();
            for (int i = 0; i < count; i++)
            {
                _enqueueMethod.Invoke(queue, new object[] { _elementFormatter.Read(reader, encoding) });
            }

            return queue;
        }
    }

    internal sealed class StackCollectionFormatter<TCollection, TElement> : BinaryFormatterBase<TCollection>
        where TCollection : class, IEnumerable<TElement>
    {
        private readonly Func<object> _creator;
        private readonly IBinaryFormatter<TElement> _elementFormatter = BinaryFormatterResolver.GetFormatter<TElement>();
        private readonly MethodInfo _pushMethod;

        public StackCollectionFormatter(Func<object> creator)
        {
            _creator = creator;
            _pushMethod = BinarySerializeTypeBuffer.GetPushMethod(typeof(TCollection), typeof(TElement));
        }

        public override void Write(BinaryWriter writer, TCollection value, Encoding encoding)
        {
            if (value == null)
            {
                writer.Write((byte)0);
                return;
            }

            writer.Write((byte)1);
            var items = new List<TElement>();
            foreach (var element in value)
            {
                items.Add(element);
            }

            writer.Write(items.Count);
            for (int i = items.Count - 1; i >= 0; i--)
            {
                _elementFormatter.Write(writer, items[i], encoding);
            }
        }

        public override TCollection Read(BinaryReader reader, Encoding encoding)
        {
            if (reader.ReadByte() == 0)
                return default;

            int count = reader.ReadInt32();
            var stack = (TCollection)_creator();
            for (int i = 0; i < count; i++)
            {
                _pushMethod.Invoke(stack, new object[] { _elementFormatter.Read(reader, encoding) });
            }

            return stack;
        }
    }

    internal sealed class ObjectFormatter<T> : BinaryFormatterBase<T>
    {
        private readonly TypeLayout _layout;
        private readonly IBinaryFormatter[] _fieldFormatters;

        public ObjectFormatter()
        {
            _layout = BinarySerializeTypeBuffer.GetSerializableFields(typeof(T));
            _fieldFormatters = new IBinaryFormatter[_layout.Fields.Length];
            for (int i = 0; i < _layout.Fields.Length; i++)
            {
                _fieldFormatters[i] = BinaryFormatterResolver.GetFormatter(_layout.Fields[i].FieldType);
            }
        }

        public override void Write(BinaryWriter writer, T value, Encoding encoding)
        {
            if (!typeof(T).IsValueType)
            {
                if (ReferenceEquals(value, null))
                {
                    writer.Write((byte)0);
                    return;
                }

                writer.Write((byte)1);
            }

            object boxed = value;
            for (int i = 0; i < _layout.Fields.Length; i++)
            {
                var field = _layout.Fields[i];
                _fieldFormatters[i].Write(writer, field.Field.GetValue(boxed), encoding);
            }
        }

        public override T Read(BinaryReader reader, Encoding encoding)
        {
            if (!typeof(T).IsValueType && reader.ReadByte() == 0)
                return default;

            object boxed = _layout.CreateInstance();
            for (int i = 0; i < _layout.Fields.Length; i++)
            {
                var field = _layout.Fields[i];
                field.Field.SetValue(boxed, _fieldFormatters[i].Read(reader, encoding));
            }

            return (T)boxed;
        }
    }
}
