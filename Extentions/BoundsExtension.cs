using UnityEngine;

namespace PowerCellStudio
{
    public static class BoundsExtension
    {
        public static bool TryGetIntersection(this Bounds self, Bounds other, out Bounds intersection)
        {
            intersection = new Bounds();
            if (!self.Intersects(other)) return false;
            intersection.SetMinMax(
                new Vector3(Mathf.Max(self.min.x, other.min.x), Mathf.Max(self.min.y, other.min.y),Mathf.Max(self.min.z, other.min.z)),
                new Vector3(Mathf.Min(self.max.x, other.max.x), Mathf.Min(self.max.y, other.max.y), Mathf.Min(self.max.z, other.max.z)));
            return true;
        }
    }
}