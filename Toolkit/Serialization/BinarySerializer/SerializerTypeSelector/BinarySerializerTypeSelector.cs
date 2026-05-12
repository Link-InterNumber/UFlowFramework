using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    public abstract class BinarySerializerTypeSelector<T> : IBinarySerializerTypeSelector<T>
    {
        public Type TargetType => typeof(T);

        public abstract void Write(BinaryWriter writer, T value, Encoding encoding);

        public abstract T Read(BinaryReader reader, Encoding encoding);

        void IBinarySerializerTypeSelector.Write(BinaryWriter writer, object value, Encoding encoding)
        {
            Write(writer, (T)value, encoding);
        }

        object IBinarySerializerTypeSelector.Read(BinaryReader reader, Encoding encoding)
        {
            return Read(reader, encoding);
        }
    }
}