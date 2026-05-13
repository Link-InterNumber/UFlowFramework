using System;
using System.IO;
using System.Linq;
using System.Text;

namespace PowerCellStudio
{
    public class FloatArrayRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("float[]");
        }

        public override string TypeName()
        {
            return "float[]";
        }

        public static float[] Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            if (string.IsNullOrEmpty(stringValue)) return Array.Empty<float>();
            var stringArray = stringValue.Split(new []{'|', ';', ','});
            return stringArray.Select(float.Parse).ToArray();
        }

        public static void WriteItemData(float[] item, BinaryWriter writer, Encoding encoding)
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

        public static float[] ReadItemData(BinaryReader reader, Encoding encoding)
        {
            int length = reader.ReadInt32();
            if (length < 0)
                return null;

            if (length == 0)
                return Array.Empty<float>();

            var result = new float[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = reader.ReadSingle();
            }

            return result;
        }
    }
}
