using System;
using System.IO;
using System.Linq;
using System.Text;

namespace PowerCellStudio
{
    public class DoubleArrayRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("double[]");
        }

        public override string TypeName()
        {
            return "double[]";
        }

        public static double[] Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            if (string.IsNullOrEmpty(stringValue)) return Array.Empty<double>();
            var stringArray = stringValue.Split(new []{'|', ';', ','});
            return stringArray.Select(double.Parse).ToArray();
        }

        public static void WriteItemData(double[] item, BinaryWriter writer, Encoding encoding)
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

        public static double[] ReadItemData(BinaryReader reader, Encoding encoding)
        {
            int length = reader.ReadInt32();
            if (length < 0)
                return null;

            if (length == 0)
                return Array.Empty<double>();

            var result = new double[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = reader.ReadDouble();
            }

            return result;
        }
    }
}
