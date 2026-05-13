using System;
using System.IO;
using System.Linq;
using System.Text;

namespace PowerCellStudio
{
    public class IntArrayRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("int[]");
        }

        public override string TypeName()
        {
            return "int[]";
        }

        public static int[] Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            if (string.IsNullOrEmpty(stringValue)) return Array.Empty<int>();
            var stringArray = stringValue.Split(new []{'|', ';', ','});
            return stringArray.Select(int.Parse).ToArray();
        }

        public static void WriteItemData(int[] item, BinaryWriter writer, Encoding encoding)
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

        public static int[] ReadItemData(BinaryReader reader, Encoding encoding)
        {
            int length = reader.ReadInt32();
            if (length < 0)
                return null;

            if (length == 0)
                return Array.Empty<int>();

            var result = new int[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = reader.ReadInt32();
            }

            return result;
        }
    }
}
