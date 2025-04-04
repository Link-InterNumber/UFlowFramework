using System;

namespace PowerCellStudio
{
    [Serializable]
    public class AttributeLong: AttributeValue<long>
    {
        public AttributeLong(long initValue) : base(initValue) { }
        
        public static implicit operator int(AttributeLong i)
        {
            return Convert.ToInt32(i.GetCurrent());
        }
        
        public static implicit operator float(AttributeLong i)
        {
            return Convert.ToSingle(i.GetCurrent());
        }
        
        public static implicit operator long(AttributeLong i)
        {
            return i.GetCurrent();
        }
    }
}