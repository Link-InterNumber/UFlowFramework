using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal sealed class ObjectFormatter<T> : BinaryFormatterBase<T>
    {
        private readonly TypeLayout _layout;
        private readonly IBinaryFormatter[] _fieldFormatters;

        public ObjectFormatter()
        {
            _layout = BinarySerializeTypeBuffer.GetSerializableFields(typeof(T));
            _fieldFormatters = new IBinaryFormatter[_layout.Fields.Length];
            for (int i = 0; i < _layout.Fields.Length; i++)
            {
                _fieldFormatters[i] = BinaryFormatterResolver.GetFormatter(_layout.Fields[i].FieldType);
            }
        }

        public override void Write(BinaryWriter writer, T value, Encoding encoding)
        {
            if (!typeof(T).IsValueType)
            {
                if (ReferenceEquals(value, null))
                {
                    writer.Write((byte)0);
                    return;
                }

                writer.Write((byte)1);
            }

            object boxed = value;
            for (int i = 0; i < _layout.Fields.Length; i++)
            {
                var field = _layout.Fields[i];
                _fieldFormatters[i].Write(writer, field.Field.GetValue(boxed), encoding);
            }
        }

        public override T Read(BinaryReader reader, Encoding encoding)
        {
            if (!typeof(T).IsValueType && reader.ReadByte() == 0)
                return default;

            object boxed = _layout.CreateInstance();
            for (int i = 0; i < _layout.Fields.Length; i++)
            {
                var field = _layout.Fields[i];
                field.Field.SetValue(boxed, _fieldFormatters[i].Read(reader, encoding));
            }

            return (T)boxed;
        }
    }
}