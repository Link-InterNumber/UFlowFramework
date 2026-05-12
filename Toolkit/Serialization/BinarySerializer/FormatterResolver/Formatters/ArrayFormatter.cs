using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal sealed class ArrayFormatter<TElement> : BinaryFormatterBase<TElement[]>
    {
        private readonly IBinaryFormatter<TElement> _elementFormatter = BinaryFormatterResolver.GetFormatter<TElement>();

        public override void Write(BinaryWriter writer, TElement[] value, Encoding encoding)
        {
            if (value == null)
            {
                writer.Write((byte)0);
                return;
            }

            writer.Write((byte)1);
            writer.Write(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                _elementFormatter.Write(writer, value[i], encoding);
            }
        }

        public override TElement[] Read(BinaryReader reader, Encoding encoding)
        {
            if (reader.ReadByte() == 0)
                return null;

            int length = reader.ReadInt32();
            var array = new TElement[length];
            for (int i = 0; i < length; i++)
            {
                array[i] = _elementFormatter.Read(reader, encoding);
            }

            return array;
        }
    }
}