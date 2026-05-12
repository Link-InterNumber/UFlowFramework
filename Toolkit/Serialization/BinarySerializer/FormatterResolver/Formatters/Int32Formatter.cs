using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal sealed class Int32Formatter : BinaryFormatterBase<int>
    {
        public override void Write(BinaryWriter writer, int value, Encoding encoding) => writer.Write(value);
        public override int Read(BinaryReader reader, Encoding encoding) => reader.ReadInt32();
    }
}