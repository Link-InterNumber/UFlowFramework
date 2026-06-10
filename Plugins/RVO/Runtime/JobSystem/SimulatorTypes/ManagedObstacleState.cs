using Unity.Mathematics;

namespace RVO.JobSystem
{
    internal struct ManagedObstacleState
    {
        public int id;
        public float3 point;
        public float3 direction;
        public int previous;
        public int next;
        public bool convex;
    }
}
