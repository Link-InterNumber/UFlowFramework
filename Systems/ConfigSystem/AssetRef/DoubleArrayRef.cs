using System;
using System.Linq;

namespace PowerCellStudio
{
    public class DoubleArrayRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("double[]");
        }

        public override string TypeName()
        {
            return "double[]";
        }

        public static double[] Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            if (string.IsNullOrEmpty(stringValue)) return Array.Empty<double>();
            var stringArray = stringValue.Split(new []{'|', ';', ','});
            return stringArray.Select(double.Parse).ToArray();
        }
    }
}
