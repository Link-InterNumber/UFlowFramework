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
}