using System;

namespace PowerCellStudio
{
    public class StringRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("string");
        }
        
        public override string TypeName()
        {
            return "string";
        }
        
        public static string Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            return string.IsNullOrEmpty(stringValue) ? string.Empty : stringValue;
        }
    }
}
