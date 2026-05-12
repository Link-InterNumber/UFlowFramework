using System;
using System.Linq;

namespace PowerCellStudio
{
    public class LongArrayRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("long[]");
        }

        public override string TypeName()
        {
            return "long[]";
        }

        public static long[] Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            if (string.IsNullOrEmpty(stringValue)) return Array.Empty<long>();
            var stringArray = stringValue.Split(new []{'|', ';', ','});
            return stringArray.Select(long.Parse).ToArray();
        }
    }
}
