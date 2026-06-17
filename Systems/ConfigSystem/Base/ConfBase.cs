using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    [Serializable]
    public class ConfBase// : IBinaryData
    {
        public ConfBase()
        {
            
        }

        public static implicit operator bool(ConfBase conf)
        {
            return conf != null;
        }

        public virtual void WriteData(BinaryWriter writer, Encoding encoding)
        {
            ConfigLogger.LogError($"[ConfBase] 序列化失败，未重写WriteData方法。ConfName: {GetType().Name}");
        }

        public virtual void ReadData(BinaryReader reader, Encoding encoding)
        {
            ConfigLogger.LogError($"[ConfBase] 反序列化失败，未重写ReadData方法。ConfName: {GetType().Name}");
        }
    }
}