using System;

namespace PowerCellStudio
{
    [Serializable]
    public class AttributeDouble: AttributeValue<double>
    {
        public AttributeDouble(double initValue) : base(initValue) { }
        
        public static implicit operator double(AttributeDouble i)
        {
            return i.GetCurrent();
        }
        
        public static implicit operator int(AttributeDouble i)
        {
            return Convert.ToInt32(i.GetCurrent());
        }
        
        public static implicit operator float(AttributeDouble i)
        {
            return (float)i.GetCurrent();
        }
        
        public static implicit operator long(AttributeDouble i)
        {
            return Convert.ToInt64(i.GetCurrent());
        }
    }
}