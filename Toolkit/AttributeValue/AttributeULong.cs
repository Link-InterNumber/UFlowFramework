using System;

namespace PowerCellStudio
{
    [Serializable]
    public class AttributeULong: AttributeValue<ulong>
    {
        public AttributeULong(ulong initValue) : base(initValue) { }
        
        public static implicit operator int(AttributeULong i)
        {
            return Convert.ToInt32(i.GetCurrent());
        }
        
        public static implicit operator float(AttributeULong i)
        {
            return Convert.ToSingle(i.GetCurrent());
        }
        
        public static implicit operator ulong(AttributeULong i)
        {
            return i.GetCurrent();
        }
    }
}