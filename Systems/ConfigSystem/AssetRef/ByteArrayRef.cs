using System;
using System.IO;
using System.Linq;
using System.Text;

namespace PowerCellStudio
{
    public class ByteArrayRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("byte[]") || lowerRawType.Equals("uint8[]");
        }

        public override string TypeName()
        {
            return "byte[]";
        }

        public static byte[] Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            if (string.IsNullOrEmpty(stringValue)) return Array.Empty<byte>();
            var stringArray = stringValue.Split(new []{'|', ';', ','});
            return stringArray.Select(byte.Parse).ToArray();
        }

        public static void WriteItemData(byte[] item, BinaryWriter writer, Encoding encoding)
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

        public static byte[] ReadItemData(BinaryReader reader, Encoding encoding)
        {
            int length = reader.ReadInt32();
            if (length < 0)
                return null;

            if (length == 0)
                return Array.Empty<byte>();

            var result = new byte[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = reader.ReadByte();
            }

            return result;
        }
    }
}
