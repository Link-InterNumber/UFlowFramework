using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace RVO.JobSystem
{
    [BurstCompile]
    internal struct ClearObstacleNeighborsJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<int> ObstacleNeighborIndices;
        [NativeDisableParallelForRestriction]
        public NativeArray<float> ObstacleNeighborDistances;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> ObstacleNeighborCounts;
        public int MaxObstacleNeighborCapacity;

        public void Execute(int index)
        {
            var start = index * MaxObstacleNeighborCapacity;
            for (var slot = 0; slot < MaxObstacleNeighborCapacity; slot++)
            {
                ObstacleNeighborIndices[start + slot] = -1;
                ObstacleNeighborDistances[start + slot] = float.PositiveInfinity;
            }

            ObstacleNeighborCounts[index] = 0;
        }
    }
}