using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PowerCellStudio
{
    public class Vector3IntRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("vector3int") || lowerRawType.Equals("vec3int");
        }

        public override string TypeName()
        {
            return "Vector3Int";
        }

        public static Vector3Int Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            if (string.IsNullOrEmpty(stringValue)) return Vector3Int.zero;
            var values = stringValue.Split(new []{'|', ';', ','}, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => int.Parse(value.Trim()))
                .ToArray();
            return new Vector3Int(values.Length > 0 ? values[0] : 0, values.Length > 1 ? values[1] : 0, values.Length > 2 ? values[2] : 0);
        }

        public static void WriteItemData(Vector3Int item, BinaryWriter writer, Encoding encoding)
        {
            writer.Write(item.x);
            writer.Write(item.y);
            writer.Write(item.z);
        }

        public static Vector3Int ReadItemData(BinaryReader reader, Encoding encoding)
        {
            return new Vector3Int(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
        }
    }
}
