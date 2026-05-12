using System;

namespace PowerCellStudio
{
    public class LongRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("long");
        }
        
        public override string TypeName()
        {
            return "long";
        }
        
        public static long Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            return string.IsNullOrEmpty(stringValue) ? 0L : long.Parse(stringValue);
        }
    }
}
