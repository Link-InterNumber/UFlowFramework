using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal sealed class DoubleFormatter : BinaryFormatterBase<double>
    {
        public override void Write(BinaryWriter writer, double value, Encoding encoding) => writer.Write(value);
        public override double Read(BinaryReader reader, Encoding encoding) => reader.ReadDouble();
    }
}