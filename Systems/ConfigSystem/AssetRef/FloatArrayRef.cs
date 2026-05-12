using System;
using System.Linq;

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
    }
}
