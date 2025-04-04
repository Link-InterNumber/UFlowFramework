using System;
using UnityEngine;

namespace PowerCellStudio
{
    [Serializable]
    public class LissajousCurve
    {
        [Tooltip("宽度")] [Min(0f)] public float weight;
        [Tooltip("高度")] [Min(0f)] public float height;
        [Tooltip("x频率")] [Min(0f)] public float frequencyX;
        [Tooltip("y频率")] [Min(0f)] public float frequencyY;
        [Tooltip("偏移")] [Range(0f, Mathf.PI * 0.5f)] public float offset;

        private float _curTime;
        private Vector2 _curPos;
        
        public LissajousCurve(float weight, float height, float frequencyX, float frequencyY, float offset, float startTime = 0f)
        {
            this.weight = Mathf.Max(weight, 0f);
            this.height = Mathf.Max(height, 0f);
            this.frequencyX = Mathf.Max(frequencyX, 0f);
            this.frequencyY = Mathf.Max(frequencyY, 0f);
            this.offset = Mathf.Clamp(offset, 0f, Mathf.PI * 0.5f);
            _curTime = startTime;
            _curPos.x = this.weight * Mathf.Sin(this.frequencyX * _curTime);
            _curPos.y = this.height * Mathf.Sin(this.frequencyY * _curTime + this.offset);
        }

        /// <summary>
        /// 推进曲线更新
        /// </summary>
        /// <param name="dt">更新时间差</param>
        /// <returns>二维位置</returns>
        public Vector2 Update(float dt)
        {
            _curTime += dt;
            _curPos.x = weight * Mathf.Sin(frequencyX * _curTime);
            _curPos.y = height * Mathf.Sin(frequencyY * _curTime + offset);
            return _curPos;
        }
        
        /// <summary>
        /// 将曲线设置在给定的时间点
        /// </summary>
        /// <param name="time">时间点位置</param>
        /// <returns>二维位置</returns>
        public Vector2 UpdateTime(float time)
        {
            _curTime = time;
            _curPos.x = weight * Mathf.Sin(frequencyX * _curTime);
            _curPos.y = height * Mathf.Sin(frequencyY * _curTime + offset);
            return _curPos;
        }
    }
}