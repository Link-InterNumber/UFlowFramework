using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal sealed class ListCollectionFormatter<TCollection, TElement> : BinaryFormatterBase<TCollection>
        where TCollection : class, IEnumerable<TElement>
    {
        private readonly Func<TCollection> _creator;
        private readonly IBinaryFormatter<TElement> _elementFormatter = BinaryFormatterResolver.GetFormatter<TElement>();

        public ListCollectionFormatter(Func<TCollection> creator)
        {
            _creator = creator;
        }

        public override void Write(BinaryWriter writer, TCollection value, Encoding encoding)
        {
            if (value == null)
            {
                writer.Write((byte)0);
                return;
            }

            writer.Write((byte)1);
            var collection = (ICollection<TElement>)value;
            writer.Write(collection.Count);
            foreach (var element in collection)
            {
                _elementFormatter.Write(writer, element, encoding);
            }
        }

        public override TCollection Read(BinaryReader reader, Encoding encoding)
        {
            if (reader.ReadByte() == 0)
                return default;

            int count = reader.ReadInt32();
            var typedCollection = _creator();
            var collection = (ICollection<TElement>)typedCollection;
            for (int i = 0; i < count; i++)
            {
                collection.Add(_elementFormatter.Read(reader, encoding));
            }

            return typedCollection;
        }
    }
}