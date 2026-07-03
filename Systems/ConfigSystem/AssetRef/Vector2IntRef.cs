using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PowerCellStudio
{
    public class Vector2IntRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("vector2int") || lowerRawType.Equals("vec2int");
        }

        public override string TypeName()
        {
            return "Vector2Int";
        }

        public static Vector2Int Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            if (string.IsNullOrEmpty(stringValue)) return Vector2Int.zero;
            var values = stringValue.Split(new []{'|', ';', ','}, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => int.Parse(value.Trim()))
                .ToArray();
            return new Vector2Int(values.Length > 0 ? values[0] : 0, values.Length > 1 ? values[1] : 0);
        }

        public static void WriteItemData(Vector2Int item, BinaryWriter writer, Encoding encoding)
        {
            writer.Write(item.x);
            writer.Write(item.y);
        }

        public static Vector2Int ReadItemData(BinaryReader reader, Encoding encoding)
        {
            return new Vector2Int(reader.ReadInt32(), reader.ReadInt32());
        }
    }
}
