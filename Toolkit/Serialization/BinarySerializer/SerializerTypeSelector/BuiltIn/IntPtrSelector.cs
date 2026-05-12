using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal sealed class IntPtrSelector : BinarySerializerTypeSelector<IntPtr>
    {
        public override void Write(BinaryWriter writer, IntPtr value, Encoding encoding)
        {
            writer.Write(value.ToInt64());
        }

        public override IntPtr Read(BinaryReader reader, Encoding encoding)
        {
            return new IntPtr(reader.ReadInt64());
        }
    }
}