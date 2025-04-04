using System;

namespace PowerCellStudio
{
    [Serializable]
    public class AttributeFloat: AttributeValue<float>
    {
        public AttributeFloat(float initValue) : base(initValue) { }
        
        public static implicit operator int(AttributeFloat i)
        {
            return Convert.ToInt32(i.GetCurrent());
        }
        
        public static implicit operator float(AttributeFloat i)
        {
            return i.GetCurrent();
        }
        
        public static implicit operator long(AttributeFloat i)
        {
            return Convert.ToInt64(i.GetCurrent());
        }
    }
}