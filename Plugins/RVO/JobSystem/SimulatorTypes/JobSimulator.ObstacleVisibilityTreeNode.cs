using Unity.Mathematics;

namespace RVO.JobSystem
{
    public sealed partial class JobSimulator
    {
        private sealed class ObstacleVisibilityTreeNode
        {
            public float3 boundsMin;
            public float3 boundsMax;
            public ObstacleVisibilityTreeNode left;
            public ObstacleVisibilityTreeNode right;
            public int[] segmentIndices;
        }
    }
}