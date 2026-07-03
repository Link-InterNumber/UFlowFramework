using System;
using System.IO;
using System.Linq;
using System.Text;

namespace PowerCellStudio
{
    public class ULongArrayRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("ulong[]") || lowerRawType.Equals("uint64[]");
        }

        public override string TypeName()
        {
            return "ulong[]";
        }

        public static ulong[] Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            if (string.IsNullOrEmpty(stringValue)) return Array.Empty<ulong>();
            var stringArray = stringValue.Split(new []{'|', ';', ','});
            return stringArray.Select(ulong.Parse).ToArray();
        }

        public static void WriteItemData(ulong[] item, BinaryWriter writer, Encoding encoding)
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

        public static ulong[] ReadItemData(BinaryReader reader, Encoding encoding)
        {
            int length = reader.ReadInt32();
            if (length < 0)
                return null;

            if (length == 0)
                return Array.Empty<ulong>();

            var result = new ulong[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = reader.ReadUInt64();
            }

            return result;
        }
    }
}
