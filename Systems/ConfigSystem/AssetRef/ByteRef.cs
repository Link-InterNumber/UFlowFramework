using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    public class ByteRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("byte") || lowerRawType.Equals("uint8");
        }

        public override string TypeName()
        {
            return "byte";
        }

        public static byte Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            return string.IsNullOrEmpty(stringValue) ? (byte)0 : byte.Parse(stringValue);
        }

        public static void WriteItemData(byte item, BinaryWriter writer, Encoding encoding)
        {
            writer.Write(item);
        }

        public static byte ReadItemData(BinaryReader reader, Encoding encoding)
        {
            return reader.ReadByte();
        }
    }
}
