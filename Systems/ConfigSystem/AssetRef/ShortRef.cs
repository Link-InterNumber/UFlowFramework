using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    public class ShortRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("short") || lowerRawType.Equals("int16");
        }

        public override string TypeName()
        {
            return "short";
        }

        public static short Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            return string.IsNullOrEmpty(stringValue) ? (short)0 : short.Parse(stringValue);
        }

        public static void WriteItemData(short item, BinaryWriter writer, Encoding encoding)
        {
            writer.Write(item);
        }

        public static short ReadItemData(BinaryReader reader, Encoding encoding)
        {
            return reader.ReadInt16();
        }
    }
}
