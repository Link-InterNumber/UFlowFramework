using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    public class FloatRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("float");
        }
        
        public override string TypeName()
        {
            return "float";
        }
        
        public static float Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            return string.IsNullOrEmpty(stringValue) ? 0f : float.Parse(stringValue);
        }

        public static void WriteItemData(float item, BinaryWriter writer, Encoding encoding)
        {
            writer.Write(item);
        }

        public static float ReadItemData(BinaryReader reader, Encoding encoding)
        {
            return reader.ReadSingle();
        }
    }
}
