using System;
using UnityEngine;

namespace PowerCellStudio
{
    /// <summary>
    /// PID曲线控制器类，用于处理比例、积分和微分控制。
    /// PID curve controller class for handling proportional, integral, and derivative control.
    /// </summary>
    [Serializable]
    public class PIDCurve
    {
        private const float MaxIntegralMagnitude = 1000f;

        /// <summary>
        /// 比例系数
        /// Proportional coefficient
        /// </summary>
        [Min(0f)]
        public float P;

        /// <summary>
        /// 积分系数
        /// Integral coefficient
        /// </summary>
        [Min(0f)]
        public float I;

        /// <summary>
        /// 微分系数
        /// Derivative coefficient
        /// </summary>
        [Range(0, 0.99f)]
        public float D;

        private float _targetValue;
        
        /// <summary>
        /// 目标值
        /// Target value
        /// </summary>
        public float targetValue => _targetValue;
        
        private float _previousDelta;
        private float _integralAccumulate;

        /// <summary>
        /// 构造一个新的PID曲线实例
        /// Construct a new instance of PIDCurve.
        /// </summary>
        /// <param name="targetVal">目标值 / Target value</param>
        /// <param name="curVal">当前值 / Current value</param>
        /// <param name="p">比例系数，应 >= 0 / Proportional coefficient, should be >= 0</param>
        /// <param name="i">积分系数，应 >= 0 / Integral coefficient, should be >= 0</param>
        /// <param name="d">微分系数，应在[0, 1) / Derivative coefficient, should be within [0, 1)</param>
        public PIDCurve(float targetVal, float curVal, float p, float i, float d)
        {
            _targetValue = targetVal;
            _previousDelta = targetVal - curVal;
            P = Mathf.Max(0, p);
            I = Mathf.Max(0, i);
            D = Mathf.Clamp(d, 0f, 0.99f);
            _integralAccumulate = 0f;
        }

        /// <summary>
        /// 计算本帧增加量
        /// Calculate the increment for the current frame.
        /// </summary>
        /// <param name="dt">时间差 / Time delta</param>
        /// <param name="curValue">当前值 / Current value</param>
        /// <returns>本帧增加量 / Increment for the current frame</returns>
        public float Update(float dt, float curValue)
        {
            var delta = _targetValue - curValue;
            return Step(dt, delta, ref _previousDelta, ref _integralAccumulate);
        }

        /// <summary>
        /// 重置目标值和当前值
        /// Reset the target and current values.
        /// </summary>
        /// <param name="targetVal">新目标值 / New target value</param>
        /// <param name="curVal">当前值 / Current value</param>
        public void ResetTarget(float targetVal, float curVal)
        {
            _targetValue = targetVal;
            _previousDelta = targetVal - curVal;
            _integralAccumulate = 0f;
        }

        private float Step(float dt, float delta, ref float previousDelta, ref float integralAccumulate)
        {
            if (dt <= 0f) return 0f;

            integralAccumulate = Mathf.Clamp(
                integralAccumulate + (delta + previousDelta) * 0.5f * dt,
                -MaxIntegralMagnitude,
                MaxIntegralMagnitude);

            var pValue = P * delta * dt;
            var iValue = I * integralAccumulate * dt;
            var dValue = D * (previousDelta - delta);

            previousDelta = delta;

            return pValue + iValue + dValue;
        }

#if UNITY_EDITOR

        private float _integralAccumulateOnGUI;
        private float _previousDeltaOnGUI;

        /// <summary>
        /// 初始化用于GUI的PID曲线
        /// Initialize PID curve for GUI use.
        /// </summary>
        public void OnGUIInit()
        {
            _integralAccumulateOnGUI = 0f;
            _previousDeltaOnGUI = 1f;
        }

        /// <summary>
        /// 更新用于GUI的PID值
        /// Update PID value for GUI use.
        /// </summary>
        /// <param name="dt">时间差 / Time delta</param>
        /// <param name="input">输入值 / Input value</param>
        /// <returns>更新后的反馈值 / Updated feedback value</returns>
        public float OnGUIUpdateValue(float dt, float input)
        {
            var delta = -input;
            return Step(dt, delta, ref _previousDeltaOnGUI, ref _integralAccumulateOnGUI);
        }
#endif
    }
}