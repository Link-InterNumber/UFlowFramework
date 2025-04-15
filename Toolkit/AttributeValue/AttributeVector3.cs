using System;
using UnityEngine;

namespace PowerCellStudio
{
    [Serializable]
    public class AttributeVector3: AttributeValue<Vector3>
    {
        public AttributeVector3(Vector3 initValue) : base(initValue) { }
        
        public static implicit operator Vector2(AttributeVector3 i)
        {
            return i.GetCurrent();
        }
        
        public static implicit operator Vector3(AttributeVector3 i)
        {
            return i.GetCurrent();
        }
    }

    public static class AttributeVector3Extension
    {
        public static Vector3 Normalize(this AttributeVector3 i)
        {
            return Vector3.Normalize(i.GetCurrent());
        }
        
        public static float Magnitude(this AttributeVector3 i)
        {
            return Vector3.Magnitude(i.GetCurrent());
        }
    }
}