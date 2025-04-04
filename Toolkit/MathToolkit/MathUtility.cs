using UnityEngine;

namespace PowerCellStudio
{
    public static class MathUtility
    {
        public static float Remap(float val, float start, float end, float toStart, float toEnd)
        {
            if (end.Equals(start)) return toStart;
            return Mathf.Lerp(toStart, toEnd, (val - start) / (end - start));
        }

        public static Oval2D CreateOval2D(float width, float height, Vector2 position, float rotateClockwise = 0)
        {
            width = Mathf.Abs(width);
            height = Mathf.Abs(height);
            var oval = new Oval2D(width, height);
            oval.offset = position;
            oval.rotateClockwise = rotateClockwise;
            return oval;
        }

        public static ProceduralCurve CreateProceduralCurve(float initValue, float frequency, float damping = 1f, float response = 0f)
        {
            var curve = new ProceduralCurve
            {
                frequency = Mathf.Max(0.1f, frequency),
                damping = damping,
                response = response
            };
            curve.InitCal(initValue);
            return curve;
        }

        public static Parabola2D CreateParabola2D(Vector2 startPos, Vector2 endPos, float heightRelateTo2Point)
        {
            if (startPos.Equals(endPos))
            {
                LinkLog.LogError("Parabola2D start position can not be same to end position!");
                return null;
            }
            return new Parabola2D(startPos, endPos, heightRelateTo2Point);
        }
    }
}