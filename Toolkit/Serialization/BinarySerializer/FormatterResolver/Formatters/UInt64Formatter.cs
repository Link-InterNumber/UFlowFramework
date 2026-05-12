using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal sealed class UInt64Formatter : BinaryFormatterBase<ulong>
    {
        public override void Write(BinaryWriter writer, ulong value, Encoding encoding) => writer.Write(value);
        public override ulong Read(BinaryReader reader, Encoding encoding) => reader.ReadUInt64();
    }
}