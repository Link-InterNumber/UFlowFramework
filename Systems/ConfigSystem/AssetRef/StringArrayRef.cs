using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    public class StringArrayRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("string[]");
        }

        public override string TypeName()
        {
            return "string[]";
        }

        public static string[] Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            if (string.IsNullOrEmpty(stringValue)) return Array.Empty<string>();
            var stringArray = stringValue.Split('|');
            return stringArray;
        }

        public static void WriteItemData(string[] item, BinaryWriter writer, Encoding encoding)
        {
            if (item == null)
            {
                writer.Write(-1);
                return;
            }

            writer.Write(item.Length);
            for (int i = 0; i < item.Length; i++)
            {
                item[i].WriteString(writer, encoding);
            }
        }

        public static string[] ReadItemData(BinaryReader reader, Encoding encoding)
        {
            int length = reader.ReadInt32();
            if (length < 0)
                return null;

            if (length == 0)
                return Array.Empty<string>();

            var result = new string[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = StringExtension.ReadString(reader, encoding);
            }

            return result;
        }
    }
}
