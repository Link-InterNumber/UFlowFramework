using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal sealed class KeyValuePairFormatter<TKey, TValue> : BinaryFormatterBase<KeyValuePair<TKey, TValue>>
    {
        private readonly IBinaryFormatter<TKey> _keyFormatter = BinaryFormatterResolver.GetFormatter<TKey>();
        private readonly IBinaryFormatter<TValue> _valueFormatter = BinaryFormatterResolver.GetFormatter<TValue>();

        public override void Write(BinaryWriter writer, KeyValuePair<TKey, TValue> value, Encoding encoding)
        {
            _keyFormatter.Write(writer, value.Key, encoding);
            _valueFormatter.Write(writer, value.Value, encoding);
        }

        public override KeyValuePair<TKey, TValue> Read(BinaryReader reader, Encoding encoding)
        {
            return new KeyValuePair<TKey, TValue>(
                _keyFormatter.Read(reader, encoding),
                _valueFormatter.Read(reader, encoding));
        }
    }
}