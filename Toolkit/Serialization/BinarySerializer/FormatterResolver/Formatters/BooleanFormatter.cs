using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal sealed class BooleanFormatter : BinaryFormatterBase<bool>
    {
        public override void Write(BinaryWriter writer, bool value, Encoding encoding) => writer.Write(value);
        public override bool Read(BinaryReader reader, Encoding encoding) => reader.ReadBoolean();
    }
}