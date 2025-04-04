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
        [Range(0f, 360f)]public float rotateClockwise = 0f;
        
        public Oval2D(float widthValue, float heightValue)
        {
            width = Mathf.Max(0.01f,widthValue);
            height = Mathf.Max(0.01f, heightValue);
        }

        public Vector2 GetValueByCentrifugalAngle(float value)
        {
            var x = width * Mathf.Cos(Mathf.Deg2Rad * value);
            var y = height * Mathf.Sin(Mathf.Deg2Rad * value);
            var theta = Mathf.Deg2Rad * rotateClockwise;
            var cosTheta = Mathf.Cos(theta);
            var sinTheta = Mathf.Sin(theta);
            var rotatedX = x * cosTheta + y * sinTheta + offset.x;
            var rotatedY = y * cosTheta - x * sinTheta + offset.y;
            var pos = new Vector2(rotatedX, rotatedY);
            return pos;
        }
        
        public bool GetValueByY(float valueY, out Vector2 posUp, out Vector2 posDown)
        {
            posUp = posDown = Vector2.zero;
            if (height == 0 && width == 0) return false;
            var theta = Mathf.Deg2Rad * rotateClockwise;
            var cosTheta = Mathf.Cos(theta);
            var sinTheta = Mathf.Sin(theta);
            var a = 1f / (height * height * cosTheta * cosTheta + width * width * sinTheta * sinTheta);
            var b = 2f * (height * height - width * width) * (valueY - offset.y) * cosTheta * sinTheta;
            var c = (valueY - offset.y) * (valueY - offset.y) * (height * height * sinTheta * sinTheta + width * width * cosTheta * cosTheta) - height * height *  width * width;
            var delta = b * b - 4f * a * c;
            if (delta < 0) return false;
            posUp.y = valueY;
            posDown.y = valueY;
            posUp.x = (Mathf.Sqrt(delta) - b) / (2f * a) + offset.y;
            posDown.x = (-1f * Mathf.Sqrt(delta) - b) / (2f * a) + offset.y;
            return true;
        }
        
        public bool GetValueByX(float valueX, out Vector2 posRight, out Vector2 posLeft)
        {
            posRight = posLeft = Vector2.zero;
            if (height == 0 && width == 0) return false;
            var theta = Mathf.Deg2Rad * rotateClockwise;
            var cosTheta = Mathf.Cos(theta);
            var sinTheta = Mathf.Sin(theta);
            var a = 1f / (height * height * sinTheta * sinTheta + width * width * cosTheta * cosTheta);
            var b = 2f * (height * height - width * width) * (valueX - offset.x) * cosTheta * sinTheta;
            var c = (valueX - offset.x) * (valueX - offset.x) * (height * height * cosTheta * cosTheta + width * width * sinTheta * sinTheta) - height * height *  width * width;
            var delta = b * b - 4f * a * c;
            if (delta < 0) return false;
            posRight.x = valueX;
            posLeft.x = valueX;
            posRight.y = (Mathf.Sqrt(delta) - b) / (2f * a) + offset.x;
            posLeft.y = (-1f * Mathf.Sqrt(delta) - b) / (2f * a) + offset.x;
            return true;
        }
    }
}