using System;
using UnityEngine;

namespace PowerCellStudio
{
    /// <summary>
    /// 程序化二阶动态曲线，用于根据输入值生成带阻尼、响应和惯性的平滑输出。
    /// Procedural second-order dynamic curve used to generate a smoothed output with damping, response, and inertia from an input value.
    /// </summary>
    [Serializable]
    public class ProceduralCurve
    {
        /// <summary>
        /// 曲线响应频率，数值越高输出越快跟随输入。
        /// Curve response frequency. Higher values make the output follow the input faster.
        /// </summary>
        [Tooltip("频率")] [Range(0.1f,10f)]public float frequency;

        /// <summary>
        /// 阻尼系数，用于控制震荡衰减强度。
        /// Damping coefficient used to control oscillation decay strength.
        /// </summary>
        [Tooltip("阻尼")] [Min(0f)] public float damping = 0.5f;

        /// <summary>
        /// 响应系数，用于控制输出对输入变化速度的预响应程度。
        /// Response coefficient used to control how much the output anticipates input velocity changes.
        /// </summary>
        [Tooltip("响应")] public float response;
        private bool _inited = false;

        /// <summary>
        /// 是否已初始化计算状态。
        /// Whether the calculation state has been initialized.
        /// </summary>
        public bool inited => _inited;

        private float _k1, _k2, _k3;
        private float _previousInput, _output, _outputDelta, _initValue;
        // private float dtCrit;
        
        private static float PI => Mathf.PI;

        /// <summary>
        /// 初始化曲线计算状态，并将当前输出设置为初始输入值。
        /// Initializes the curve calculation state and sets the current output to the initial input value.
        /// </summary>
        /// <param name="initInput">初始输入值。The initial input value.</param>
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

        /// <summary>
        /// 根据时间步长和输入值更新曲线输出。
        /// Updates the curve output according to the time step and input value.
        /// </summary>
        /// <param name="dt">时间步长。The time step.</param>
        /// <param name="input">当前输入值。The current input value.</param>
        /// <param name="inputDelta">输入变化速度；为 0 时会根据上一帧输入自动计算。The input velocity; when 0, it is calculated automatically from the previous input.</param>
        /// <returns>更新后的曲线输出值。The updated curve output value.</returns>
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

            _outputDelta = _outputDelta + dt * (input + _k3 * inputDelta - _output - _k1 * _outputDelta) / k2Stable;
            _output = _output + dt * _outputDelta;
            // int iterations = Mathf.CeilToInt(dt / dtCrit);
            // dt = dt / iterations;
            // for (int i = 0; i < iterations; i++)
            // {
            //     Output = Output + dt * OutputDelta;
            //     OutputDelta = OutputDelta + dt * (Output + k3 * inputDelta - Output - k1 * OutputDelta) / k2;
            // }
            return _output;
        }

        /// <summary>
        /// 当前输出在初始值到上一输入值之间的插值比例。
        /// The interpolation ratio of the current output between the initial value and the previous input value.
        /// </summary>
        public float LerpValue => _previousInput == _initValue ? 0 : (_output - _initValue) / (_previousInput - _initValue);
        
#if UNITY_EDITOR
        /// <summary>
        /// 初始化编辑器 GUI 预览用的曲线计算状态。
        /// Initializes the curve calculation state used by editor GUI preview.
        /// </summary>
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

        /// <summary>
        /// 在编辑器 GUI 预览中根据时间步长和输入值更新曲线输出。
        /// Updates the curve output in editor GUI preview according to the time step and input value.
        /// </summary>
        /// <param name="dt">时间步长。The time step.</param>
        /// <param name="input">当前输入值。The current input value.</param>
        /// <param name="inputDelta">输入变化速度；为 0 时会根据上一帧输入自动计算。The input velocity; when 0, it is calculated automatically from the previous input.</param>
        /// <returns>更新后的编辑器预览输出值。The updated editor preview output value.</returns>
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
