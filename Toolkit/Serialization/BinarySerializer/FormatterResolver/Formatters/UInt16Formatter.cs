using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal sealed class UInt16Formatter : BinaryFormatterBase<ushort>
    {
        public override void Write(BinaryWriter writer, ushort value, Encoding encoding) => writer.Write(value);
        public override ushort Read(BinaryReader reader, Encoding encoding) => reader.ReadUInt16();
    }
}