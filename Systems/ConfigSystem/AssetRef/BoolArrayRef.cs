using System;
using System.IO;
using System.Linq;
using System.Text;

namespace PowerCellStudio
{
    public class BoolArrayRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("bool[]");
        }

        public override string TypeName()
        {
            return "bool[]";
        }

        public static bool[] Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            if (string.IsNullOrEmpty(stringValue)) return Array.Empty<bool>();
            var stringArray = stringValue.Split(new []{'|', ';', ','});
            return stringArray.Select(o => !string.IsNullOrEmpty(o) && !o.Equals("0")).ToArray();
        }

        public static void WriteItemData(bool[] item, BinaryWriter writer, Encoding encoding)
        {
            if (item == null)
            {
                writer.Write(-1);
                return;
            }

            writer.Write(item.Length);
            for (int i = 0; i < item.Length; i++)
            {
                writer.Write(item[i]);
            }
        }

        public static bool[] ReadItemData(BinaryReader reader, Encoding encoding)
        {
            int length = reader.ReadInt32();
            if (length < 0)
                return null;

            if (length == 0)
                return Array.Empty<bool>();

            var result = new bool[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = reader.ReadBoolean();
            }

            return result;
        }
    }
}
