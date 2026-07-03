using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PowerCellStudio
{
    public class ColorRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("color") || lowerRawType.Equals("colour");
        }

        public override string TypeName()
        {
            return "Color";
        }

        public static Color Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            if (string.IsNullOrEmpty(stringValue)) return Color.clear;
            var rawValue = stringValue.Trim();
            if (ColorUtility.TryParseHtmlString(rawValue.StartsWith("#") ? rawValue : $"#{rawValue}", out var htmlColor))
            {
                return htmlColor;
            }

            var values = rawValue.Split(new []{'|', ';', ','}, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => float.Parse(value.Trim(), CultureInfo.InvariantCulture))
                .ToArray();
            return new Color(
                values.Length > 0 ? values[0] : 0f,
                values.Length > 1 ? values[1] : 0f,
                values.Length > 2 ? values[2] : 0f,
                values.Length > 3 ? values[3] : 1f);
        }

        public static void WriteItemData(Color item, BinaryWriter writer, Encoding encoding)
        {
            writer.Write(item.r);
            writer.Write(item.g);
            writer.Write(item.b);
            writer.Write(item.a);
        }

        public static Color ReadItemData(BinaryReader reader, Encoding encoding)
        {
            return new Color(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
    }
}
