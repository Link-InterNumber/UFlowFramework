using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    public class StringRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("string");
        }
        
        public override string TypeName()
        {
            return "string";
        }
        
        public static string Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            return string.IsNullOrEmpty(stringValue) ? string.Empty : stringValue;
        }

        public static void WriteItemData(string item, BinaryWriter writer, Encoding encoding)
        {
            item.WriteString(writer, encoding);
        }

        public static string ReadItemData(BinaryReader reader, Encoding encoding)
        {
            return StringExtension.ReadString(reader, encoding);
        }
    }
}
