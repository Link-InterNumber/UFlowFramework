using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal sealed class SingleFormatter : BinaryFormatterBase<float>
    {
        public override void Write(BinaryWriter writer, float value, Encoding encoding) => writer.Write(value);
        public override float Read(BinaryReader reader, Encoding encoding) => reader.ReadSingle();
    }
}