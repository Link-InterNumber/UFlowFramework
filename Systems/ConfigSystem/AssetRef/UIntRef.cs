using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    public class UIntRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("uint") || lowerRawType.Equals("uint32");
        }

        public override string TypeName()
        {
            return "uint";
        }

        public static uint Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            return string.IsNullOrEmpty(stringValue) ? 0u : uint.Parse(stringValue);
        }

        public static void WriteItemData(uint item, BinaryWriter writer, Encoding encoding)
        {
            writer.Write(item);
        }

        public static uint ReadItemData(BinaryReader reader, Encoding encoding)
        {
            return reader.ReadUInt32();
        }
    }
}
