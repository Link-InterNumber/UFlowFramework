using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal sealed class GuidSelector : BinarySerializerTypeSelector<Guid>
    {
        public override void Write(BinaryWriter writer, Guid value, Encoding encoding)
        {
            writer.Write(value.ToByteArray());
        }

        public override Guid Read(BinaryReader reader, Encoding encoding)
        {
            return new Guid(reader.ReadBytes(16));
        }
    }
}