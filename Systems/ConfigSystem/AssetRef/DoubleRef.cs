using System;

namespace PowerCellStudio
{
    public class DoubleRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("double");
        }
        
        public override string TypeName()
        {
            return "double";
        }
        
        public static double Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            return string.IsNullOrEmpty(stringValue) ? 0d : double.Parse(stringValue);
        }
    }
}
