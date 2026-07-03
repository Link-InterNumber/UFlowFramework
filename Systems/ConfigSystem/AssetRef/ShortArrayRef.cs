using System;
using System.IO;
using System.Linq;
using System.Text;

namespace PowerCellStudio
{
    public class ShortArrayRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("short[]") || lowerRawType.Equals("int16[]");
        }

        public override string TypeName()
        {
            return "short[]";
        }

        public static short[] Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            if (string.IsNullOrEmpty(stringValue)) return Array.Empty<short>();
            var stringArray = stringValue.Split(new []{'|', ';', ','});
            return stringArray.Select(short.Parse).ToArray();
        }

        public static void WriteItemData(short[] item, BinaryWriter writer, Encoding encoding)
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

        public static short[] ReadItemData(BinaryReader reader, Encoding encoding)
        {
            int length = reader.ReadInt32();
            if (length < 0)
                return null;

            if (length == 0)
                return Array.Empty<short>();

            var result = new short[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = reader.ReadInt16();
            }

            return result;
        }
    }
}
