using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal sealed class DictionaryCollectionFormatter<TCollection, TKey, TValue> : BinaryFormatterBase<TCollection>
        where TCollection : class, IDictionary<TKey, TValue>
    {
        private readonly Func<TCollection> _creator;
        private readonly IBinaryFormatter<TKey> _keyFormatter = BinaryFormatterResolver.GetFormatter<TKey>();
        private readonly IBinaryFormatter<TValue> _valueFormatter = BinaryFormatterResolver.GetFormatter<TValue>();

        public DictionaryCollectionFormatter(Func<TCollection> creator)
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
            writer.Write(value.Count);
            foreach (var entry in value)
            {
                _keyFormatter.Write(writer, entry.Key, encoding);
                _valueFormatter.Write(writer, entry.Value, encoding);
            }
        }

        public override TCollection Read(BinaryReader reader, Encoding encoding)
        {
            if (reader.ReadByte() == 0)
                return default;

            int count = reader.ReadInt32();
            var collection = _creator();
            for (int i = 0; i < count; i++)
            {
                collection.Add(_keyFormatter.Read(reader, encoding), _valueFormatter.Read(reader, encoding));
            }

            return collection;
        }
    }
}