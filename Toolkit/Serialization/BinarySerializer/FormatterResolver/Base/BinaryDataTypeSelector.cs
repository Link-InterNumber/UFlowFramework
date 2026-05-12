using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal sealed class BinaryDataTypeSelector<T> : BinaryFormatterBase<T>
         where T : IBinaryData
    {
        private readonly Func<T> _creator;

        public BinaryDataTypeSelector(Func<T> creator)
        {
            _creator = creator;
        }

        public override void Write(BinaryWriter writer, T value, Encoding encoding)
        {
            value.WriteData(writer, encoding);
        }

        public override T Read(BinaryReader reader, Encoding encoding)
        {
            var instance = _creator();
            instance.ReadData(reader, encoding);
            return instance;
        }
    }
}