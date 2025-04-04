using System;
using UnityEngine;

namespace PowerCellStudio
{
    [Serializable]
    public class PIDCurve
    {
        /// <summary>
        /// 比例系数
        /// </summary>
        [Min(0f)]public float P;
        /// <summary>
        /// 积分系数
        /// </summary>
        [Min(0f)]public float I;
        /// <summary>
        /// 微分系数
        /// </summary>
        [Range(0, 0.99f)]public float D;

        private float _targetValue;
        /// <summary>
        /// 目标值
        /// </summary>
        public float targetValue => _targetValue;
        private float _previousDelta;
        private float _integralAccumulate;

        /// <summary>
        /// PID曲线
        /// </summary>
        /// <param name="targetVal">目标值</param>
        /// <param name="curVal">当前值</param>
        /// <param name="p">比例系数，应 >=0</param>
        /// <param name="i">积分系数，应 >=0</param>
        /// <param name="d">微分系数，应在[0, 1)</param>
        public PIDCurve(float targetVal, float curVal, float p, float i, float d)
        {
            _targetValue = targetVal;
            _previousDelta = targetVal - curVal;
            P = Mathf.Max(0, p);
            I = Mathf.Max(0, i);
            D = Mathf.Clamp(d, 0f, 0.99f);
            _integralAccumulate = 0;
        }

        /// <summary>
        /// 计算PID负反馈值
        /// </summary>
        /// <param name="dt">时间差</param>
        /// <param name="curValue">当前值</param>
        /// <returns>负反馈值</returns>
        public float Update(float dt, float curValue)
        {
            if (dt == 0f) return 0f;
            var delta = _targetValue - curValue;
            var pValue = P * delta * dt;
            var iValue = I * _integralAccumulate * dt;
            var dValue = D * (_previousDelta - delta);
            _integralAccumulate += (delta + _previousDelta) * 0.5f * dt;
            _previousDelta = delta;
            return pValue + iValue + dValue;
        }

        public void ResetTarget(float targetVal, float curVal)
        {
            _targetValue = targetVal;
            _previousDelta = targetVal - curVal;
            _integralAccumulate = 0;
        }
        
#if UNITY_EDITOR

        private float _integralAccumulateOnGUI;
        private float _previousDeltaOnGUI;

        public void OnGUIInit()
        {
            _integralAccumulateOnGUI = 0;
            _previousDeltaOnGUI = 1;
        }

        public float OnGUIUpdateValue(float dt, float input)
        {
            if (dt == 0f) return 0f;
            var delta = 0f - input;
            var pValue = P * delta * dt;
            var iValue = I * _integralAccumulateOnGUI * dt;
            var dValue = D * (_previousDeltaOnGUI - delta);
            _integralAccumulateOnGUI += (delta + _previousDeltaOnGUI) * 0.5f * dt;
            _previousDeltaOnGUI = delta;
            return pValue + iValue + dValue;
        }
#endif
    }
}