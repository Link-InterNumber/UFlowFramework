using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    public class LongRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("long");
        }
        
        public override string TypeName()
        {
            return "long";
        }
        
        public static long Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            return string.IsNullOrEmpty(stringValue) ? 0L : long.Parse(stringValue);
        }

        public static void WriteItemData(long item, BinaryWriter writer, Encoding encoding)
        {
            writer.Write(item);
        }

        public static long ReadItemData(BinaryReader reader, Encoding encoding)
        {
            return reader.ReadInt64();
        }
    }
}
