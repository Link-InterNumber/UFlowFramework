using System;
using System.Linq;

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
    
    public class IntArrayRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("int[]");
        }

        public override string TypeName()
        {
            return "int[]";
        }

        public static int[] Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            if (string.IsNullOrEmpty(stringValue)) return Array.Empty<int>();
            var stringArray = stringValue.Split(new []{'|', ';', ','});
            return stringArray.Select(int.Parse).ToArray();
        }
    }
    
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
    
    public class FloatArrayRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("float[]");
        }

        public override string TypeName()
        {
            return "float[]";
        }

        public static float[] Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            if (string.IsNullOrEmpty(stringValue)) return Array.Empty<float>();
            var stringArray = stringValue.Split(new []{'|', ';', ','});
            return stringArray.Select(float.Parse).ToArray();
        }
    }
    
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
    
    public class BoolRef: TypeRef
    {
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("bool") || lowerRawType.Equals("boolean");;
        }
        
        public override string TypeName()
        {
            return "bool";
        }
        
        public static bool Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            return !string.IsNullOrEmpty(stringValue);
        }
    }
    
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
            return stringArray.Select(o=>!string.IsNullOrEmpty(o)).ToArray();
        }
    }
}