using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
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
            object boxedValue = value;

            switch (_underlyingTypeCode)
            {
                case TypeCode.Byte:
                    writer.Write(Convert.ToByte(boxedValue));
                    return;
                case TypeCode.SByte:
                    writer.Write(Convert.ToSByte(boxedValue));
                    return;
                case TypeCode.Int16:
                    writer.Write(Convert.ToInt16(boxedValue));
                    return;
                case TypeCode.UInt16:
                    writer.Write(Convert.ToUInt16(boxedValue));
                    return;
                case TypeCode.Int32:
                    writer.Write(Convert.ToInt32(boxedValue));
                    return;
                case TypeCode.UInt32:
                    writer.Write(Convert.ToUInt32(boxedValue));
                    return;
                case TypeCode.Int64:
                    writer.Write(Convert.ToInt64(boxedValue));
                    return;
                case TypeCode.UInt64:
                    writer.Write(Convert.ToUInt64(boxedValue));
                    return;
                default:
                    throw new NotSupportedException($"Unsupported enum underlying type '{_underlyingType}'.");
            }
        }

        public override T Read(BinaryReader reader, Encoding encoding)
        {
            return (T)Enum.ToObject(typeof(T), _underlyingFormatter.Read(reader, encoding));
        }
    }
}