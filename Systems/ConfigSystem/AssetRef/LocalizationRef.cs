using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    [Serializable]
    public abstract class LocalizationRef<T> : TypeRef, IBinaryData
    {
        public string rawString;
        public string localizationKey;

        public abstract T Get();

        public abstract void WriteData(BinaryWriter writer, Encoding encoding);
        public abstract void ReadData(BinaryReader reader, Encoding encoding);
    }
}