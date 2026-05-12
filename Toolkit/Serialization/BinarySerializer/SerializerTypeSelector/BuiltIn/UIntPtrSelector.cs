using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal sealed class UIntPtrSelector : BinarySerializerTypeSelector<UIntPtr>
    {
        public override void Write(BinaryWriter writer, UIntPtr value, Encoding encoding)
        {
            writer.Write(value.ToUInt64());
        }

        public override UIntPtr Read(BinaryReader reader, Encoding encoding)
        {
            return new UIntPtr(reader.ReadUInt64());
        }
    }
}