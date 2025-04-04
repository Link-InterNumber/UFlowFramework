using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PowerCellStudio
{
    /// <summary>
    /// y = -k(x-a)^2 +b
    /// </summary>
    [Serializable]
    public class Parabola2D
    {
        public Vector3 StartPos;
        public Vector3 EndPos;
        
        private float a;
        private float b;
        private float k;

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
        /// 根据X轴位置获得抛物线上坐标
        /// </summary>
        /// <param name="PosX"></param>
        /// <returns></returns>
        public Vector2 GetDotByX(float PosX)
        {
            return new Vector2(PosX, getHeightByX(PosX));
        }

        public List<Vector2> GetDotByY(float PosY)
        {
            if (PosY >= b)
                return new List<Vector2>(){new Vector2(a,b)};

            List<Vector2> result = new List<Vector2> { };
            
            result.Add(new Vector2(a - Mathf.Sqrt((b - PosY) / k), PosY));
            result.Add(new Vector2(Mathf.Sqrt((b - PosY) / k) + a, PosY));

            return result;
        }

        public Vector2 GetNearDot(Vector2 pos)
        {
            var dot = GetDotByY(pos.y).OrderBy(o=>o.x);
            return pos.x + 0.1f >= a ? dot.LastOrDefault() : dot.FirstOrDefault();
        }

        /// <summary>
        /// 根据X轴位置获得抛物线上Y轴高度
        /// </summary>
        /// <param name="PosX"></param>
        /// <returns></returns>
        public float getHeightByX(float posX)
        {
            var result = -k * (posX - a) * (posX - a) + b;
            return result;
        }
        // y = -k(x-a)^2 +b

        /// <summary>
        /// 根据X轴位置获得抛物线上Y轴高度
        /// </summary>
        /// <param name="PosX"></param>
        /// <returns></returns>
        public float getHeight(Vector3 curPoint)
        {
            return getHeightByX(curPoint.x);
        }

        /// <summary>
        /// 获取抛物线上一段曲线
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        public List<Vector2> getTrail(Vector3 startPoint, Vector3 endPoint, float interval)
        {
            List<Vector2> result = new List<Vector2>();
            var dotNum = (int)(Mathf.Abs(endPoint.x - startPoint.x) / interval);
            var sign = Math.Sign(endPoint.x - startPoint.x);
            for (int i = 0; i < dotNum; i++)
            {
                var curPosX = startPoint.x + sign * interval * i;
                result.Add(new Vector2(curPosX, getHeightByX(curPosX)));
            }
            return result;
        }
    }
}