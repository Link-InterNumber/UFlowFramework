using System.IO;
using System.Text;

namespace PowerCellStudio
{
    public interface IBinarySerializerTypeSelector<T> : IBinarySerializerTypeSelector
    {
        void Write(BinaryWriter writer, T value, Encoding encoding);

        new T Read(BinaryReader reader, Encoding encoding);
    }
}