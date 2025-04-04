using UnityEngine;

namespace PowerCellStudio
{
    public static class Vector3Extension
    {
        public static Vector3 NormalizedOrDefault(this Vector3 v3, Vector3 defaultValue)
        {
            return v3.Equals(Vector3.zero) ? defaultValue : v3.normalized;
        }

        public static Vector3 RotationAngle2D(this Vector3 v3, float angle)
        {
            return Quaternion.AngleAxis(angle, Vector3.forward) * v3;
        }
        
        public static float ManhattanDistance(this Vector3 v3, Vector3 target)
        {
            return Mathf.Abs(v3.x - target.x) + Mathf.Abs(v3.y - target.y) + Mathf.Abs(v3.z - target.z);
        }
        
        public static bool IsInRange(this Vector3 v3, Vector3 target, float range)
        {
            var manhattanDistance = v3.ManhattanDistance(target);
            if(manhattanDistance / 3f > range) return false;
            var distance = Vector3.SqrMagnitude(-v3 + target);
            return distance <= (range * range);
        }
        
        public static bool IsInRange2D(this Vector3 v3, Vector3 target, float range)
        {
            var manhattanDistance = Vector2Extension.ManhattanDistance(v3, target);
            if(manhattanDistance * 0.5f > range) return false;
            var distance = Vector2.SqrMagnitude(-v3 + target);
            return distance <= (range * range);
        }
    }
    
    public static class Vector2Extension
    {
        public static Vector2 NormalizedOrDefault(this Vector2 v3, Vector2 defaultValue)
        {
            return v3.Equals(Vector2.zero) ? defaultValue : v3.normalized;
        }

        public static Vector2 RotationAngle2D(this Vector2 v3, float angle)
        {
            return Quaternion.AngleAxis(angle, Vector3.forward) * v3;
        }
        
        public static float ManhattanDistance(this Vector2 v3, Vector2 target)
        {
            return Mathf.Abs(v3.x - target.x) + Mathf.Abs(v3.y - target.y);
        }
        
        public static bool IsInRange(this Vector2 v2, Vector2 target, float range)
        {
            var manhattanDistance = v2.ManhattanDistance(target);
            if(manhattanDistance > range) return false;
            var distance = Vector2.SqrMagnitude(target - v2);
            return distance <= (range * range);
        }
    }
}