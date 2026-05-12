using System.Text;
using System.IO;

namespace PowerCellStudio
{
    public interface IBinaryData
    {
        public void WriteData(BinaryWriter writer, Encoding encoding);
        
        public void ReadData(BinaryReader reader, Encoding encoding);
    }
}