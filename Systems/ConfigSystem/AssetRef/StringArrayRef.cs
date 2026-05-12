using System;

namespace PowerCellStudio
{
    public class StringArrayRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("string[]");
        }

        public override string TypeName()
        {
            return "string[]";
        }

        public static string[] Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            if (string.IsNullOrEmpty(stringValue)) return Array.Empty<string>();
            var stringArray = stringValue.Split('|');
            return stringArray;
        }
    }
}
