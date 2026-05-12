using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal sealed class DecimalFormatter : BinaryFormatterBase<decimal>
    {
        public override void Write(BinaryWriter writer, decimal value, Encoding encoding) => writer.Write(value);
        public override decimal Read(BinaryReader reader, Encoding encoding) => reader.ReadDecimal();
    }
}