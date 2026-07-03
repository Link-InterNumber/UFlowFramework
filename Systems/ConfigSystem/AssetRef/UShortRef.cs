using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    public class UShortRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("ushort") || lowerRawType.Equals("uint16");
        }

        public override string TypeName()
        {
            return "ushort";
        }

        public static ushort Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            return string.IsNullOrEmpty(stringValue) ? (ushort)0 : ushort.Parse(stringValue);
        }

        public static void WriteItemData(ushort item, BinaryWriter writer, Encoding encoding)
        {
            writer.Write(item);
        }

        public static ushort ReadItemData(BinaryReader reader, Encoding encoding)
        {
            return reader.ReadUInt16();
        }
    }
}
