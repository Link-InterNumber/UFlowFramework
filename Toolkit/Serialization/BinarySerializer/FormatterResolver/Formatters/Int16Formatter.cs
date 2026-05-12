using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal sealed class Int16Formatter : BinaryFormatterBase<short>
    {
        public override void Write(BinaryWriter writer, short value, Encoding encoding) => writer.Write(value);
        public override short Read(BinaryReader reader, Encoding encoding) => reader.ReadInt16();
    }
}