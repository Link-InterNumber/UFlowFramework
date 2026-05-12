using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal sealed class TimeSpanSelector : BinarySerializerTypeSelector<TimeSpan>
    {
        public override void Write(BinaryWriter writer, TimeSpan value, Encoding encoding)
        {
            writer.Write(value.Ticks);
        }

        public override TimeSpan Read(BinaryReader reader, Encoding encoding)
        {
            return new TimeSpan(reader.ReadInt64());
        }
    }
}