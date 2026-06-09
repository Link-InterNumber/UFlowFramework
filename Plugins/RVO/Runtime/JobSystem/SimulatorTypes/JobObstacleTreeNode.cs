using Unity.Mathematics;

namespace RVO.JobSystem
{
    internal struct JobObstacleTreeNode
    {
        public float3 boundsMin;
        public float3 boundsMax;
        public int left;
        public int right;
        public int obstacleIndex;
    }
}