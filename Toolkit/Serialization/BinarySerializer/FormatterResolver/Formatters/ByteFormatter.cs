using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal sealed class ByteFormatter : BinaryFormatterBase<byte>
    {
        public override void Write(BinaryWriter writer, byte value, Encoding encoding) => writer.Write(value);
        public override byte Read(BinaryReader reader, Encoding encoding) => reader.ReadByte();
    }
}