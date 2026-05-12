using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal sealed class DateTimeOffsetSelector : BinarySerializerTypeSelector<DateTimeOffset>
    {
        public override void Write(BinaryWriter writer, DateTimeOffset value, Encoding encoding)
        {
            writer.Write(value.Ticks);
            writer.Write(value.Offset.Ticks);
        }

        public override DateTimeOffset Read(BinaryReader reader, Encoding encoding)
        {
            return new DateTimeOffset(reader.ReadInt64(), new TimeSpan(reader.ReadInt64()));
        }
    }
}