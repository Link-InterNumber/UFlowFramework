using Unity.Mathematics;

namespace RVO.JobSystem
{
    internal sealed class ObstacleVisibilityTreeNode
    {
        public float3 boundsMin;
        public float3 boundsMax;
        public ObstacleVisibilityTreeNode left;
        public ObstacleVisibilityTreeNode right;
        public int[] segmentIndices;
    }
}