using System;
using System.Linq;

namespace PowerCellStudio
{
    public class BoolArrayRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("bool[]");
        }

        public override string TypeName()
        {
            return "bool[]";
        }

        public static bool[] Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            if (string.IsNullOrEmpty(stringValue)) return Array.Empty<bool>();
            var stringArray = stringValue.Split(new []{'|', ';', ','});
            return stringArray.Select(o => !string.IsNullOrEmpty(o) && !o.Equals("0")).ToArray();
        }
    }
}
