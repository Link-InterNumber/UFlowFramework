using System;
using UnityEngine;

namespace PowerCellStudio
{
    [Serializable]
    public class LissajousCurve
    {
        [Tooltip("宽度")] [Min(0f)] public float width;
        [Tooltip("高度")] [Min(0f)] public float height;
        [Tooltip("x频率")] [Min(0f)] public float frequencyX;
        [Tooltip("y频率")] [Min(0f)] public float frequencyY;
        [Tooltip("偏移")] [Range(0f, Mathf.PI * 0.5f)] public float offset;

        private float _curTime;
        private Vector2 _curPos;

        /// <summary>
        /// 构造一个新的Lissajous曲线实例。
        /// Construct a new instance of LissajousCurve.
        /// </summary>
        /// <param name="width">曲线的宽度 / Width of the curve</param>
        /// <param name="height">曲线的高度 / Height of the curve</param>
        /// <param name="frequencyX">X轴的频率 / Frequency on the X-axis</param>
        /// <param name="frequencyY">Y轴的频率 / Frequency on the Y-axis</param>
        /// <param name="offset">曲线的偏移 / Offset of the curve</param>
        /// <param name="startTime">起始时间 / Start time</param>
        public LissajousCurve(float width, float height, float frequencyX, float frequencyY, float offset, float startTime = 0f)
        {
            this.width = Mathf.Max(width, 0f);
            this.height = Mathf.Max(height, 0f);
            this.frequencyX = Mathf.Max(frequencyX, 0f);
            this.frequencyY = Mathf.Max(frequencyY, 0f);
            this.offset = Mathf.Clamp(offset, 0f, Mathf.PI * 0.5f);
            UpdateTime(startTime);
        }

        /// <summary>
        /// 更新曲线的当前位置，推进时间。
        /// Update the current position of the curve, advancing the time.
        /// </summary>
        /// <param name="dt">时间增量 / Time increment</param>
        /// <returns>曲线的当前二维位置 / Current 2D position of the curve</returns>
        public Vector2 Update(float dt)
        {
            _curTime += dt;
            _curPos.x = width * Mathf.Sin(frequencyX * _curTime);
            _curPos.y = height * Mathf.Sin(frequencyY * _curTime + offset);
            return _curPos;
        }
        
        /// <summary>
        /// 设置曲线在给定的时间点。
        /// Set the curve at a given time point.
        /// </summary>
        /// <param name="time">设置时间点 / Set the time point</param>
        /// <returns>曲线的当前二维位置 / Current 2D position of the curve</returns>
        public Vector2 UpdateTime(float time)
        {
            _curTime = time;
            _curPos.x = width * Mathf.Sin(frequencyX * _curTime);
            _curPos.y = height * Mathf.Sin(frequencyY * _curTime + offset);
            return _curPos;
        }
    }
}