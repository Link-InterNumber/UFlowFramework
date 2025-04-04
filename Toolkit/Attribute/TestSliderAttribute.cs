using System;

namespace PowerCellStudio
{
    [AttributeUsage(AttributeTargets.Method)]
    public class TestSliderAttribute : Attribute
    {
        private float _min;
        private float _max;
        private float _defaultValue;
        
        public float Min => _min;
        public float Max => _max;
        public float DefaultValue => _defaultValue;
        
        public TestSliderAttribute(float min, float max)
        {
            _min = min;
            _max = max;
            _defaultValue = min;
        }
        
        public TestSliderAttribute(float min, float max, float defaultValue)
        {
            _min = min;
            _max = max;
            _defaultValue = defaultValue;
        }
    }
}