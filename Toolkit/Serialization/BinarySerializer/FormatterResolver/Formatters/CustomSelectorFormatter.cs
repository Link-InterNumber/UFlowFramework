using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal sealed class CustomSelectorFormatter<T> : BinaryFormatterBase<T>
    {
        private readonly IBinarySerializerTypeSelector _selector;
        private readonly IBinarySerializerTypeSelector<T> _typedSelector;

        public CustomSelectorFormatter(IBinarySerializerTypeSelector selector)
        {
            _selector = selector;
            _typedSelector = selector as IBinarySerializerTypeSelector<T>;
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

            if (_typedSelector != null)
            {
                _typedSelector.Write(writer, value, encoding);
                return;
            }

            _selector.Write(writer, value, encoding);
        }

        public override T Read(BinaryReader reader, Encoding encoding)
        {
            if (!typeof(T).IsValueType && reader.ReadByte() == 0)
                return default;

            if (_typedSelector != null)
                return _typedSelector.Read(reader, encoding);

            return (T)_selector.Read(reader, encoding);
        }
    }
}