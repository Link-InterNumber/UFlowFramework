using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    public class DoubleRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("double");
        }
        
        public override string TypeName()
        {
            return "double";
        }
        
        public static double Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            return string.IsNullOrEmpty(stringValue) ? 0d : double.Parse(stringValue);
        }

        public static void WriteItemData(double item, BinaryWriter writer, Encoding encoding)
        {
            writer.Write(item);
        }

        public static double ReadItemData(BinaryReader reader, Encoding encoding)
        {
            return reader.ReadDouble();
        }
    }
}
