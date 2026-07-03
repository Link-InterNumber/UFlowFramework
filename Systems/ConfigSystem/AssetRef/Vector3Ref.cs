using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PowerCellStudio
{
    public class Vector3Ref: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("vector3") || lowerRawType.Equals("vec3");
        }

        public override string TypeName()
        {
            return "Vector3";
        }

        public static Vector3 Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            if (string.IsNullOrEmpty(stringValue)) return Vector3.zero;
            var values = stringValue.Split(new []{'|', ';', ','}, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => float.Parse(value.Trim(), CultureInfo.InvariantCulture))
                .ToArray();
            return new Vector3(values.Length > 0 ? values[0] : 0f, values.Length > 1 ? values[1] : 0f, values.Length > 2 ? values[2] : 0f);
        }

        public static void WriteItemData(Vector3 item, BinaryWriter writer, Encoding encoding)
        {
            writer.Write(item.x);
            writer.Write(item.y);
            writer.Write(item.z);
        }

        public static Vector3 ReadItemData(BinaryReader reader, Encoding encoding)
        {
            return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
    }
}
