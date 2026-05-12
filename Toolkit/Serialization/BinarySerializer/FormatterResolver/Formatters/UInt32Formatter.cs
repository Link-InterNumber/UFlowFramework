using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal sealed class UInt32Formatter : BinaryFormatterBase<uint>
    {
        public override void Write(BinaryWriter writer, uint value, Encoding encoding) => writer.Write(value);
        public override uint Read(BinaryReader reader, Encoding encoding) => reader.ReadUInt32();
    }
}