using System;

namespace PowerCellStudio
{
    public class FloatRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("float");
        }
        
        public override string TypeName()
        {
            return "float";
        }
        
        public static float Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            return string.IsNullOrEmpty(stringValue) ? 0f : float.Parse(stringValue);
        }
    }
}
