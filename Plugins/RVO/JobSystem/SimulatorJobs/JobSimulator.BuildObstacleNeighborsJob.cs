using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace RVO.JobSystem
{
    public sealed partial class JobSimulator
    {
        [BurstCompile]
        private struct BuildObstacleNeighborsJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<JobAgentData> Agents;
            [ReadOnly] public NativeArray<JobObstacleData> Obstacles;
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

                if (Obstacles.Length == 0)
                {
                    ObstacleNeighborCounts[index] = 0;
                    return;
                }

                var agent = Agents[index];
                var range = agent.timeHorizonObst * agent.maxSpeed + agent.radius;
                var rangeSq = range * range;
                var count = 0;

                for (var obstacleIndex = 0; obstacleIndex < Obstacles.Length; obstacleIndex++)
                {
                    var obstacle = Obstacles[obstacleIndex];
                    var next = Obstacles[obstacle.next];
                    var leftOf = MathUtil.LeftOf(obstacle.point, next.point, agent.position);
                    if (!(leftOf < 0.0f))
                    {
                        continue;
                    }

                    var distSq = MathUtil.DistSqPointLineSegment(obstacle.point, next.point, agent.position);
                    if (!(distSq < rangeSq))
                    {
                        continue;
                    }

                    if (count < MaxObstacleNeighborCapacity)
                    {
                        ObstacleNeighborIndices[start + count] = obstacleIndex;
                        ObstacleNeighborDistances[start + count] = distSq;
                        count++;
                    }

                    var insert = count - 1;
                    while (insert > 0 && distSq < ObstacleNeighborDistances[start + insert - 1])
                    {
                        ObstacleNeighborIndices[start + insert] = ObstacleNeighborIndices[start + insert - 1];
                        ObstacleNeighborDistances[start + insert] = ObstacleNeighborDistances[start + insert - 1];
                        insert--;
                    }

                    ObstacleNeighborIndices[start + insert] = obstacleIndex;
                    ObstacleNeighborDistances[start + insert] = distSq;
                }

                ObstacleNeighborCounts[index] = count;
            }
        }
    }
}
