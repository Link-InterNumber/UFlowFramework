using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal sealed class DateTimeFormatter : BinaryFormatterBase<DateTime>
    {
        public override void Write(BinaryWriter writer, DateTime value, Encoding encoding) => writer.Write(value.ToBinary());
        public override DateTime Read(BinaryReader reader, Encoding encoding) => DateTime.FromBinary(reader.ReadInt64());
    }
}