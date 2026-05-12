using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal sealed class QueueCollectionFormatter<TCollection, TElement> : BinaryFormatterBase<TCollection>
        where TCollection : class, IEnumerable<TElement>
    {
        private readonly Func<TCollection> _creator;
        private readonly IBinaryFormatter<TElement> _elementFormatter = BinaryFormatterResolver.GetFormatter<TElement>();
        private readonly Action<TCollection, TElement> _enqueue;

        public QueueCollectionFormatter(Func<TCollection> creator)
        {
            _creator = creator;
            _enqueue = (Action<TCollection, TElement>)BinarySerializeTypeBuffer
                .GetEnqueueMethod(typeof(TCollection), typeof(TElement))
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
            var collection = (ICollection)value;
            writer.Write(collection.Count);
            foreach (var element in value)
            {
                _elementFormatter.Write(writer, element, encoding);
            }
        }

        public override TCollection Read(BinaryReader reader, Encoding encoding)
        {
            if (reader.ReadByte() == 0)
                return default;

            int count = reader.ReadInt32();
            var queue = _creator();
            for (int i = 0; i < count; i++)
            {
                _enqueue(queue, _elementFormatter.Read(reader, encoding));
            }

            return queue;
        }
    }
}