using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal abstract class BinaryFormatterBase<T> : IBinaryFormatter<T>
    {
        public Type TargetType => typeof(T);

        public abstract void Write(BinaryWriter writer, T value, Encoding encoding);

        public abstract T Read(BinaryReader reader, Encoding encoding);

        void IBinaryFormatter.Write(BinaryWriter writer, object value, Encoding encoding)
        {
            Write(writer, (T)value, encoding);
        }

        object IBinaryFormatter.Read(BinaryReader reader, Encoding encoding)
        {
            return Read(reader, encoding);
        }
    }
}