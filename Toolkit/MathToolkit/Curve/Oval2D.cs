using System;
using UnityEngine;

namespace PowerCellStudio
{
    [Serializable]
    public class Oval2D
    {
        [Tooltip("the width of Oval")] public float width = 2f;
        [Tooltip("the height of Oval")] public float height = 1f;
        public Vector2 offset = Vector2.zero;
        [Range(0f, 360f)] public float rotateClockwise = 0f;

        /// <summary>
        /// 构造一个新的Oval2D实例。
        /// Construct a new instance of Oval2D.
        /// </summary>
        /// <param name="widthValue">椭圆的宽度 / Width of the oval</param>
        /// <param name="heightValue">椭圆的高度 / Height of the oval</param>
        public Oval2D(float widthValue, float heightValue)
        {
            width = Mathf.Max(0.01f, widthValue);
            height = Mathf.Max(0.01f, heightValue);
        }

        /// <summary>
        /// 根据离心角度获取椭圆上的位置。
        /// Get a point on the oval given a centrifugal angle.
        /// </summary>
        /// <param name="angleValue">角度值 / Angle value</param>
        /// <returns>二维空间的位置 / Position in 2D space</returns>
        public Vector2 GetValueByCentrifugalAngle(float angleValue)
        {
            var x = width * Mathf.Cos(Mathf.Deg2Rad * angleValue);
            var y = height * Mathf.Sin(Mathf.Deg2Rad * angleValue);
            var theta = Mathf.Deg2Rad * rotateClockwise;
            var cosTheta = Mathf.Cos(theta);
            var sinTheta = Mathf.Sin(theta);
            var rotatedX = x * cosTheta + y * sinTheta + offset.x;
            var rotatedY = y * cosTheta - x * sinTheta + offset.y;
            return new Vector2(rotatedX, rotatedY);
        }

        /// <summary>
        /// 根据给定的Y值获取椭圆上的交点。
        /// Get intersection points on the oval given a Y value.
        /// </summary>
        /// <param name="valueY">Y坐标 / Y coordinate</param>
        /// <param name="posUp">上方的交点位置 / Position of the upper intersection point</param>
        /// <param name="posDown">下方的交点位置 / Position of the lower intersection point</param>
        /// <returns>是否存在交点 / Whether intersections exist</returns>
        public bool GetValueByY(float valueY, out Vector2 posUp, out Vector2 posDown)
        {
            posUp = posDown = Vector2.zero;
            if (height <= 0 || width <= 0) return false;

            var theta = Mathf.Deg2Rad * rotateClockwise;
            var cosTheta = Mathf.Cos(theta);
            var sinTheta = Mathf.Sin(theta);

            var a = 1f / (height * height * cosTheta * cosTheta + width * width * sinTheta * sinTheta);
            var b = 2f * (height * height - width * width) * (valueY - offset.y) * cosTheta * sinTheta;
            var c = (valueY - offset.y) * (valueY - offset.y) * 
                    (height * height * sinTheta * sinTheta + width * width * cosTheta * cosTheta) - 
                    height * height * width * width;

            var delta = b * b - 4f * a * c;
            if (delta < 0) return false;

            posUp.y = valueY;
            posDown.y = valueY;
            posUp.x = (Mathf.Sqrt(delta) - b) / (2f * a) + offset.x;
            posDown.x = (-Mathf.Sqrt(delta) - b) / (2f * a) + offset.x;
            return true;
        }

        /// <summary>
        /// 根据给定的X值获取椭圆上的交点。
        /// Get intersection points on the oval given an X value.
        /// </summary>
        /// <param name="valueX">X坐标 / X coordinate</param>
        /// <param name="posRight">右侧的交点位置 / Position of the right intersection point</param>
        /// <param name="posLeft">左侧的交点位置 / Position of the left intersection point</param>
        /// <returns>是否存在交点 / Whether intersections exist</returns>
        public bool GetValueByX(float valueX, out Vector2 posRight, out Vector2 posLeft)
        {
            posRight = posLeft = Vector2.zero;
            if (height <= 0 || width <= 0) return false;

            var theta = Mathf.Deg2Rad * rotateClockwise;
            var cosTheta = Mathf.Cos(theta);
            var sinTheta = Mathf.Sin(theta);

            var a = 1f / (height * height * sinTheta * sinTheta + width * width * cosTheta * cosTheta);
            var b = 2f * (height * height - width * width) * (valueX - offset.x) * cosTheta * sinTheta;
            var c = (valueX - offset.x) * (valueX - offset.x) * 
                    (height * height * cosTheta * cosTheta + width * width * sinTheta * sinTheta) - 
                    height * height * width * width;

            var delta = b * b - 4f * a * c;
            if (delta < 0) return false;

            posRight.x = valueX;
            posLeft.x = valueX;
            posRight.y = (Mathf.Sqrt(delta) - b) / (2f * a) + offset.y;
            posLeft.y = (-Mathf.Sqrt(delta) - b) / (2f * a) + offset.y;
            return true;
        }
    }
}