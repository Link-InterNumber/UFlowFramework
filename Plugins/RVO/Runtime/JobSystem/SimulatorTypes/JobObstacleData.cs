using Unity.Mathematics;

namespace RVO.JobSystem
{
    internal struct JobObstacleData
    {
        public int id;
        public float3 point;
        public float3 direction;
        public int previous;
        public int next;
        public byte convex;
    }
}
