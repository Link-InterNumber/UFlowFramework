using System;
using UnityEngine;

namespace PowerCellStudio
{
    [Serializable]
    public class ProceduralCurve
    {
        [Tooltip("频率")] [Range(0.1f,10f)]public float frequency;
        [Tooltip("阻尼")] [Min(0f)] public float damping = 0.5f;
        [Tooltip("响应")] public float response;
        private bool _inited = false;
        public bool inited => _inited;

        private float _k1, _k2, _k3;
        private float _previousInput, _output, _outputDelta, _initValue;
        // private float dtCrit;
        
        private static float PI => Mathf.PI;

        public void InitCal(float initInput)
        {
            frequency = Mathf.Clamp(frequency, 0.05f, 20f);
            damping = Mathf.Abs(damping);
            _k1 = damping / (PI * frequency);
            _k2 = 1f / (4f * PI * frequency * PI * frequency);
            _k3 = response * damping / (2f * PI * frequency);
            // dtCrit = 0.8f * (Mathf.Sqrt(4 * k2 + k1 * k1) - k1);
            _previousInput = initInput;
            _output = initInput;
            _outputDelta = 0;
            _initValue = initInput;
            _inited = true;
        }

        public float UpdateValue(float dt, float input, float inputDelta = 0)
        {
            if (!_inited) return input;
            if (inputDelta == 0)
            {
                inputDelta = (input - _previousInput) / dt;
                _previousInput = input;
            }

            float k2Stable = Mathf.Max(_k2, 1.1f * (dt * dt / 4f + dt * _k1 / 2f));
            // float k2Stable = Mathf.Max(k2, dt * dt / 2f + dt * k1 / 2f, dt * k1);

            _output = _output + dt * _outputDelta;
            _outputDelta = _outputDelta + dt * (input + _k3 * inputDelta - _output - _k1 * _outputDelta) / k2Stable;
            // int iterations = Mathf.CeilToInt(dt / dtCrit);
            // dt = dt / iterations;
            // for (int i = 0; i < iterations; i++)
            // {
            //     Output = Output + dt * OutputDelta;
            //     OutputDelta = OutputDelta + dt * (Output + k3 * inputDelta - Output - k1 * OutputDelta) / k2;
            // }
            return _output;
        }

        public float LerpValue => (_output - _initValue) / (_previousInput - _initValue);
        
#if UNITY_EDITOR
        public void OnGUIInit()
        {
            frequency = Mathf.Clamp(frequency, 0.05f, 20f);
            damping = Mathf.Abs(damping);
            _k1 = damping / (PI * frequency);
            _k2 = 1f / (4f * PI * frequency * PI * frequency);
            _k3 = response * damping / (2f * PI * frequency);
            // dtCrit = 0.8f * (Mathf.Sqrt(4 * k2 + k1 * k1) - k1);
            _previousInput = -1f;
            _output = -1f;
            _outputDelta = 0f;
        }

        public float OnGUIUpdateValue(float dt, float input, float inputDelta = 0)
        {
            if (inputDelta == 0)
            {
                inputDelta = (input - _previousInput) / dt;
                _previousInput = input;
            }

            float k2Stable = Mathf.Max(_k2, 1.1f * (dt * dt / 4f + dt * _k1 / 2f));
            // float k2Stable = Mathf.Max(k2, dt * dt / 2f + dt * k1 / 2f, dt * k1);

            _output = _output + dt * _outputDelta;
            _outputDelta = _outputDelta + dt * (input + _k3 * inputDelta - _output - _k1 * _outputDelta) / k2Stable;
            // int iterations = Mathf.CeilToInt(dt / dtCrit);
            // dt = dt / iterations;
            // for (int i = 0; i < iterations; i++)
            // {
            //     Output = Output + dt * OutputDelta;
            //     OutputDelta = OutputDelta + dt * (Output + k3 * inputDelta - Output - k1 * OutputDelta) / k2;
            // }
            return _output;
        }
#endif

    }
}
