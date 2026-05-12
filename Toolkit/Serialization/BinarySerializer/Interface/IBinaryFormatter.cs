using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal interface IBinaryFormatter
    {
        Type TargetType { get; }

        void Write(BinaryWriter writer, object value, Encoding encoding);

        object Read(BinaryReader reader, Encoding encoding);
    }
}