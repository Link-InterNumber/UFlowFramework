using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    [Serializable]
    public struct ChunkInfo : IBinaryData
    {
        public int index;
        public long offset;
        // public long length;
        /// <summary>
        /// 原始数据为TKey[]
        /// The original data is TKey[]
        /// </summary>
        public byte[] keyData;

        public void WriteData(BinaryWriter writer, Encoding encoding)
        {
            writer.Write(index);
            writer.Write(offset);
            writer.Write(keyData.Length);
            writer.Write(keyData);
        }

        public void ReadData(BinaryReader reader, Encoding encoding)
        {
            index = reader.ReadInt32();
            offset = reader.ReadInt64();
            var keyLength = reader.ReadInt32();
            keyData = reader.ReadBytes(keyLength);
        }

        public void ReadOnlyOffset(BinaryReader reader)
        {
            index = reader.ReadInt32();
            offset = reader.ReadInt64();
            var keyLength = reader.ReadInt32();
            reader.BaseStream.Seek(keyLength, SeekOrigin.Current);
        }
    }
}