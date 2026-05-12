using System;

namespace PowerCellStudio
{
    public class IntRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("int");
        }

        public override string TypeName()
        {
            return "int";
        }

        public static int Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            return string.IsNullOrEmpty(stringValue) ? 0 : int.Parse(stringValue);
        }
    }
}
