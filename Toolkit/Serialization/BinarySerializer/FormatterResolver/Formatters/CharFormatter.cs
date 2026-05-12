using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal sealed class CharFormatter : BinaryFormatterBase<char>
    {
        public override void Write(BinaryWriter writer, char value, Encoding encoding) => writer.Write(value);
        public override char Read(BinaryReader reader, Encoding encoding) => reader.ReadChar();
    }
}