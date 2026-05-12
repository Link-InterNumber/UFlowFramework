using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal sealed class StackCollectionFormatter<TCollection, TElement> : BinaryFormatterBase<TCollection>
        where TCollection : class, IEnumerable<TElement>
    {
        private readonly Func<TCollection> _creator;
        private readonly IBinaryFormatter<TElement> _elementFormatter = BinaryFormatterResolver.GetFormatter<TElement>();
        private readonly Action<TCollection, TElement> _push;

        public StackCollectionFormatter(Func<TCollection> creator)
        {
            _creator = creator;
            _push = (Action<TCollection, TElement>)BinarySerializeTypeBuffer
                .GetPushMethod(typeof(TCollection), typeof(TElement))
                .CreateDelegate(typeof(Action<TCollection, TElement>));
        }

        public override void Write(BinaryWriter writer, TCollection value, Encoding encoding)
        {
            if (value == null)
            {
                writer.Write((byte)0);
                return;
            }

            writer.Write((byte)1);
            var items = new List<TElement>();
            foreach (var element in value)
            {
                items.Add(element);
            }

            writer.Write(items.Count);
            for (int i = items.Count - 1; i >= 0; i--)
            {
                _elementFormatter.Write(writer, items[i], encoding);
            }
        }

        public override TCollection Read(BinaryReader reader, Encoding encoding)
        {
            if (reader.ReadByte() == 0)
                return default;

            int count = reader.ReadInt32();
            var stack = _creator();
            for (int i = 0; i < count; i++)
            {
                _push(stack, _elementFormatter.Read(reader, encoding));
            }

            return stack;
        }
    }
}