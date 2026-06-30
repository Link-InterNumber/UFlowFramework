using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    public class ULongRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("ulong") || lowerRawType.Equals("uint64");
        }

        public override string TypeName()
        {
            return "ulong";
        }

        public static ulong Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            return string.IsNullOrEmpty(stringValue) ? 0ul : ulong.Parse(stringValue);
        }

        public static void WriteItemData(ulong item, BinaryWriter writer, Encoding encoding)
        {
            writer.Write(item);
        }

        public static ulong ReadItemData(BinaryReader reader, Encoding encoding)
        {
            return reader.ReadUInt64();
        }
    }
}
