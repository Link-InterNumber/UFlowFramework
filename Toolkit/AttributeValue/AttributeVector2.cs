using System;
using UnityEngine;

namespace PowerCellStudio
{
    [Serializable]
    public class AttributeVector2: AttributeValue<Vector2>
    {
        public AttributeVector2(Vector2 initValue) : base(initValue) { }
        
        public static implicit operator Vector2(AttributeVector2 i)
        {
            return i.GetCurrent();
        }
        
        public static implicit operator Vector3(AttributeVector2 i)
        {
            return i.GetCurrent();
        }
    }
    
    public static class AttributeVector2Extension
    {
        public static Vector2 Normalize(this AttributeVector2 i)
        {
            return Vector3.Normalize(i.GetCurrent());
        }
        
        public static float Magnitude(this AttributeVector2 i)
        {
            return Vector3.Magnitude(i.GetCurrent());
        }
    }
}