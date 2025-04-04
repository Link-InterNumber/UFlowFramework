using UnityEngine;

namespace PowerCellStudio
{
    public class ProcessValue
    {
        private float _value;
        private float _currentValue;
        private float _processValue;
        
        public float value
        {
            get => _value;
            set
            {
                _value = value;
                _currentValue = value;
            }
        }
        
        public float currentValue => _currentValue;

        public float Precess(float dt)
        {
            _currentValue -= dt;
            _processValue = _currentValue / _value;
            return Mathf.Clamp01(_processValue);
        }

        public float processValue => _processValue;
    }
}