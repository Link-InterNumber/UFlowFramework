using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal sealed class SByteFormatter : BinaryFormatterBase<sbyte>
    {
        public override void Write(BinaryWriter writer, sbyte value, Encoding encoding) => writer.Write(value);
        public override sbyte Read(BinaryReader reader, Encoding encoding) => reader.ReadSByte();
    }
}