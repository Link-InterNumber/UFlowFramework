using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal sealed class IntPtrSelector : IBinarySerializerTypeSelector
    {
        private Type _targetType =  typeof(IntPtr);
        public Type TargetType => _targetType;

        public void Write(BinaryWriter writer, object value, Encoding encoding)
        {
            writer.Write(((IntPtr)value).ToInt64());
        }

        public object Read(BinaryReader reader, Encoding encoding)
        {
            return new IntPtr(reader.ReadInt64());
        }
    }

    internal sealed class UIntPtrSelector : IBinarySerializerTypeSelector
    {
        private Type _targetType = typeof(UIntPtr);
        public Type TargetType => _targetType;

        public void Write(BinaryWriter writer, object value, Encoding encoding)
        {
            writer.Write(((UIntPtr)value).ToUInt64());
        }

        public object Read(BinaryReader reader, Encoding encoding)
        {
            return new UIntPtr(reader.ReadUInt64());
        }
    }

    internal sealed class GuidSelector : IBinarySerializerTypeSelector
    {
        private Type _targetType = typeof(Guid);
        public Type TargetType => _targetType;

        public void Write(BinaryWriter writer, object value, Encoding encoding)
        {
            writer.Write(((Guid)value).ToByteArray());
        }

        public object Read(BinaryReader reader, Encoding encoding)
        {
            return new Guid(reader.ReadBytes(16));
        }
    }

    internal sealed class TimeSpanSelector : IBinarySerializerTypeSelector
    {
        private Type _targetType = typeof(TimeSpan);
        public Type TargetType => _targetType;

        public void Write(BinaryWriter writer, object value, Encoding encoding)
        {
            writer.Write(((TimeSpan)value).Ticks);
        }

        public object Read(BinaryReader reader, Encoding encoding)
        {
            return new TimeSpan(reader.ReadInt64());
        }
    }

    internal sealed class DateTimeOffsetSelector : IBinarySerializerTypeSelector
    {
        private Type _targetType = typeof(DateTimeOffset);
        public Type TargetType => _targetType;

        public void Write(BinaryWriter writer, object value, Encoding encoding)
        {
            DateTimeOffset dateTimeOffset = (DateTimeOffset)value;
            writer.Write(dateTimeOffset.Ticks);
            writer.Write(dateTimeOffset.Offset.Ticks);
        }

        public object Read(BinaryReader reader, Encoding encoding)
        {
            return new DateTimeOffset(reader.ReadInt64(), new TimeSpan(reader.ReadInt64()));
        }
    }
}