using System;
using System.IO;
using System.Linq;
using System.Text;

namespace PowerCellStudio
{
    public class LongArrayRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("long[]");
        }

        public override string TypeName()
        {
            return "long[]";
        }

        public static long[] Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            if (string.IsNullOrEmpty(stringValue)) return Array.Empty<long>();
            var stringArray = stringValue.Split(new []{'|', ';', ','});
            return stringArray.Select(long.Parse).ToArray();
        }

        public static void WriteItemData(long[] item, BinaryWriter writer, Encoding encoding)
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

        public static long[] ReadItemData(BinaryReader reader, Encoding encoding)
        {
            int length = reader.ReadInt32();
            if (length < 0)
                return null;

            if (length == 0)
                return Array.Empty<long>();

            var result = new long[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = reader.ReadInt64();
            }

            return result;
        }
    }
}
