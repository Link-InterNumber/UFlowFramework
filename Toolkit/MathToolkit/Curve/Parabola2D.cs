using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PowerCellStudio
{
    /// <summary>
    /// Parabola represented by the equation: y = -k(x-a)^2 + b
    /// 抛物线方程：y = -k(x-a)^2 + b
    /// </summary>
    [Serializable]
    public class Parabola2D
    {
        public Vector3 StartPos;
        public Vector3 EndPos;

        private float a;
        private float b;
        private float k;

        /// <summary>
        /// 构造一个新的Parabola2D实例。
        /// Construct a new instance of Parabola2D.
        /// </summary>
        /// <param name="startPoint">抛物线的起点 / Start point of the parabola</param>
        /// <param name="endPoint">抛物线的终点 / End point of the parabola</param>
        /// <param name="heightRelateTo2Point">抛物线的顶点相对于起点和终点的高度 / Relative height of the parabola's vertex</param>
        public Parabola2D(Vector3 startPoint, Vector3 endPoint, float heightRelateTo2Point)
        {
            StartPos = startPoint;
            EndPos = endPoint;
            heightRelateTo2Point = Mathf.Max(heightRelateTo2Point, 0.001f);
            b = heightRelateTo2Point + Mathf.Max(startPoint.y, endPoint.y);
            var tempValue = Mathf.Sqrt((startPoint.y - b) / (endPoint.y - b));
            a = (tempValue * endPoint.x + startPoint.x) / (1 + tempValue);
            k = (b - startPoint.y) / ((startPoint.x - a) * (startPoint.x - a));
        }

        /// <summary>
        /// 根据X轴位置获得抛物线上坐标。
        /// Get the coordinate on the parabola given an X-axis position.
        /// </summary>
        /// <param name="PosX">X坐标 / X coordinate</param>
        /// <returns>抛物线上的坐标 / Coordinate on the parabola</returns>
        public Vector2 GetDotByX(float PosX)
        {
            return new Vector2(PosX, GetHeightByX(PosX));
        }

        /// <summary>
        /// 根据Y轴位置获得抛物线上坐标。
        /// Get coordinates on the parabola given a Y-axis position.
        /// </summary>
        /// <param name="PosY">Y坐标 / Y coordinate</param>
        /// <returns>抛物线上的坐标列表 / List of coordinates on the parabola</returns>
        public List<Vector2> GetDotByY(float PosY)
        {
            if (PosY >= b)
                return new List<Vector2> { new Vector2(a, b) };

            List<Vector2> result = new List<Vector2>
            {
                new Vector2(a - Mathf.Sqrt((b - PosY) / k), PosY),
                new Vector2(Mathf.Sqrt((b - PosY) / k) + a, PosY)
            };

            return result;
        }

        /// <summary>
        /// 从给定位置获得最近的抛物线上的点。
        /// Get the nearest point on the parabola from a given position.
        /// </summary>
        /// <param name="pos">给定的位置 / Given position</param>
        /// <returns>抛物线上距离最近的点 / Nearest point on the parabola</returns>
        public Vector2 GetNearDot(Vector2 pos)
        {
            var dots = GetDotByY(pos.y).OrderBy(o => o.x);
            return pos.x + 0.1f >= a ? dots.LastOrDefault() : dots.FirstOrDefault();
        }

        /// <summary>
        /// 根据X轴位置获得抛物线上Y轴高度。
        /// Get the Y-axis height on the parabola given an X-axis position.
        /// </summary>
        /// <param name="PosX">X坐标 / X coordinate</param>
        /// <returns>抛物线上的Y高度 / Y height on the parabola</returns>
        public float GetHeightByX(float posX)
        {
            return -k * (posX - a) * (posX - a) + b;
        }

        /// <summary>
        /// 根据给定位置获得抛物线上Y轴高度。
        /// Get the Y-axis height on the parabola given a point location.
        /// </summary>
        /// <param name="curPoint">当前位置 / Current point</param>
        /// <returns>抛物线上的Y高度 / Y height on the parabola</returns>
        public float GetHeight(Vector3 curPoint)
        {
            return GetHeightByX(curPoint.x);
        }

        /// <summary>
        /// 获取抛物线上一段曲线。
        /// Get a segment of the parabola curve.
        /// </summary>
        /// <param name="startPoint">起点 / Start point</param>
        /// <param name="endPoint">终点 / End point</param>
        /// <param name="interval">X轴间隔 / Interval on the X-axis</param>
        /// <returns>抛物线上的坐标列表 / List of coordinates on the parabola</returns>
        public List<Vector2> GetTrail(Vector3 startPoint, Vector3 endPoint, float interval)
        {
            List<Vector2> result = new List<Vector2>();
            var dotNum = (int)(Mathf.Abs(endPoint.x - startPoint.x) / interval);
            var sign = Math.Sign(endPoint.x - startPoint.x);
            for (int i = 0; i <= dotNum; i++)
            {
                var curPosX = startPoint.x + sign * interval * i;
                result.Add(new Vector2(curPosX, GetHeightByX(curPosX)));
            }
            return result;
        }
    }
}