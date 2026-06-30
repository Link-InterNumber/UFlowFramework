using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PowerCellStudio
{
    public class Vector2Ref: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("vector2") || lowerRawType.Equals("vec2");
        }

        public override string TypeName()
        {
            return "Vector2";
        }

        public static Vector2 Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            if (string.IsNullOrEmpty(stringValue)) return Vector2.zero;
            var values = stringValue.Split(new []{'|', ';', ','}, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => float.Parse(value.Trim(), CultureInfo.InvariantCulture))
                .ToArray();
            return new Vector2(values.Length > 0 ? values[0] : 0f, values.Length > 1 ? values[1] : 0f);
        }

        public static void WriteItemData(Vector2 item, BinaryWriter writer, Encoding encoding)
        {
            writer.Write(item.x);
            writer.Write(item.y);
        }

        public static Vector2 ReadItemData(BinaryReader reader, Encoding encoding)
        {
            return new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }
    }
}
