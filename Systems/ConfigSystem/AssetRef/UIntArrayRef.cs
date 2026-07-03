using System;
using System.IO;
using System.Linq;
using System.Text;

namespace PowerCellStudio
{
    public class UIntArrayRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("uint[]") || lowerRawType.Equals("uint32[]");
        }

        public override string TypeName()
        {
            return "uint[]";
        }

        public static uint[] Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            if (string.IsNullOrEmpty(stringValue)) return Array.Empty<uint>();
            var stringArray = stringValue.Split(new []{'|', ';', ','});
            return stringArray.Select(uint.Parse).ToArray();
        }

        public static void WriteItemData(uint[] item, BinaryWriter writer, Encoding encoding)
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

        public static uint[] ReadItemData(BinaryReader reader, Encoding encoding)
        {
            int length = reader.ReadInt32();
            if (length < 0)
                return null;

            if (length == 0)
                return Array.Empty<uint>();

            var result = new uint[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = reader.ReadUInt32();
            }

            return result;
        }
    }
}
