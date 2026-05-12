using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal sealed class Int64Formatter : BinaryFormatterBase<long>
    {
        public override void Write(BinaryWriter writer, long value, Encoding encoding) => writer.Write(value);
        public override long Read(BinaryReader reader, Encoding encoding) => reader.ReadInt64();
    }
}