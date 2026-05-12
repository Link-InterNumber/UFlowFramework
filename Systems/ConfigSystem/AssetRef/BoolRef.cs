using System;

namespace PowerCellStudio
{
    public class BoolRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("bool") || lowerRawType.Equals("boolean");
        }
        
        public override string TypeName()
        {
            return "bool";
        }
        
        public static bool Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            return !string.IsNullOrEmpty(stringValue) && !stringValue.Equals("0");
        }
    }
}
