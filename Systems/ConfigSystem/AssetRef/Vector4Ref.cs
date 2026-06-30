using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PowerCellStudio
{
    public class Vector4Ref: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("vector4") || lowerRawType.Equals("vec4");
        }

        public override string TypeName()
        {
            return "Vector4";
        }

        public static Vector4 Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            if (string.IsNullOrEmpty(stringValue)) return Vector4.zero;
            var values = stringValue.Split(new []{'|', ';', ','}, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => float.Parse(value.Trim(), CultureInfo.InvariantCulture))
                .ToArray();
            return new Vector4(values.Length > 0 ? values[0] : 0f, values.Length > 1 ? values[1] : 0f, values.Length > 2 ? values[2] : 0f, values.Length > 3 ? values[3] : 0f);
        }

        public static void WriteItemData(Vector4 item, BinaryWriter writer, Encoding encoding)
        {
            writer.Write(item.x);
            writer.Write(item.y);
            writer.Write(item.z);
            writer.Write(item.w);
        }

        public static Vector4 ReadItemData(BinaryReader reader, Encoding encoding)
        {
            return new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
    }
}
