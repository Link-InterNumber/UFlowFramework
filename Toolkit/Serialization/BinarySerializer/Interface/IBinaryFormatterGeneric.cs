using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal interface IBinaryFormatter<T> : IBinaryFormatter
    {
        void Write(BinaryWriter writer, T value, Encoding encoding);

        new T Read(BinaryReader reader, Encoding encoding);
    }
}