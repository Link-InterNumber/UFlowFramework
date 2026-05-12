using System;
using System.Linq;

namespace PowerCellStudio
{
    public class IntArrayRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("int[]");
        }

        public override string TypeName()
        {
            return "int[]";
        }

        public static int[] Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            if (string.IsNullOrEmpty(stringValue)) return Array.Empty<int>();
            var stringArray = stringValue.Split(new []{'|', ';', ','});
            return stringArray.Select(int.Parse).ToArray();
        }
    }
}
