using System;
using System.IO;
using System.Linq;
using System.Text;

namespace PowerCellStudio
{
    public class UShortArrayRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("ushort[]") || lowerRawType.Equals("uint16[]");
        }

        public override string TypeName()
        {
            return "ushort[]";
        }

        public static ushort[] Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            if (string.IsNullOrEmpty(stringValue)) return Array.Empty<ushort>();
            var stringArray = stringValue.Split(new []{'|', ';', ','});
            return stringArray.Select(ushort.Parse).ToArray();
        }

        public static void WriteItemData(ushort[] item, BinaryWriter writer, Encoding encoding)
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

        public static ushort[] ReadItemData(BinaryReader reader, Encoding encoding)
        {
            int length = reader.ReadInt32();
            if (length < 0)
                return null;

            if (length == 0)
                return Array.Empty<ushort>();

            var result = new ushort[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = reader.ReadUInt16();
            }

            return result;
        }
    }
}
