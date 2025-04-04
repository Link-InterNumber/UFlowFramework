using System;

namespace PowerCellStudio
{
    [Serializable]
    public class AttributeInt: AttributeValue<int>
    {
        public AttributeInt(int initValue): base(initValue)
        {
        }
        
        public static implicit operator int(AttributeInt i)
        {
            return i.GetCurrent();
        }
        
        public static implicit operator float(AttributeInt i)
        {
            return (float) i.GetCurrent();
        }
        
        public static implicit operator long(AttributeInt i)
        {
            return Convert.ToInt64(i.GetCurrent());
        }
    }
}