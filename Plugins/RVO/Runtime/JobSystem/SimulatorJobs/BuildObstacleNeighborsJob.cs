using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace RVO.JobSystem
{
    [BurstCompile]
    internal struct BuildObstacleNeighborsJob : IJobParallelFor
    {
        private const int MaxStackSize = 64;

        [ReadOnly] public NativeArray<JobAgentData> Agents;
        [ReadOnly] public NativeArray<JobObstacleData> Obstacles;
        [ReadOnly] public NativeArray<JobObstacleTreeNode> ObstacleTreeNodes;
        [NativeDisableParallelForRestriction] public NativeArray<int> ObstacleNeighborIndices;
        [NativeDisableParallelForRestriction] public NativeArray<float> ObstacleNeighborDistances;
        [NativeDisableParallelForRestriction] public NativeArray<int> ObstacleNeighborCounts;
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

            if (ObstacleTreeNodes.IsCreated && ObstacleTreeNodes.Length > 0)
            {
                count = QueryObstacleTree(agent, rangeSq, start);
            }
            else
            {
                count = QueryAllObstacles(agent, rangeSq, start);
            }

            ObstacleNeighborCounts[index] = count;
        }

        private int QueryObstacleTree(JobAgentData agent, float rangeSq, int start)
        {
            var count = 0;
            var stack = new FixedList512Bytes<int>();
            stack.Add(0);

            while (stack.Length > 0)
            {
                var stackIndex = stack.Length - 1;
                var nodeIndex = stack[stackIndex];
                stack.RemoveAt(stackIndex);

                var node = ObstacleTreeNodes[nodeIndex];
                if (!CircleBoundsOverlap(agent.position, rangeSq, node.boundsMin, node.boundsMax))
                {
                    continue;
                }

                if (node.obstacleIndex >= 0)
                {
                    TryAddObstacle(agent, node.obstacleIndex, rangeSq, start, ref count);
                    continue;
                }

                if (node.left >= 0)
                {
                    if (stack.Length >= MaxStackSize)
                    {
                        return QueryAllObstacles(agent, rangeSq, start);
                    }

                    stack.Add(node.left);
                }

                if (node.right >= 0)
                {
                    if (stack.Length >= MaxStackSize)
                    {
                        return QueryAllObstacles(agent, rangeSq, start);
                    }

                    stack.Add(node.right);
                }
            }

            return count;
        }

        private int QueryAllObstacles(JobAgentData agent, float rangeSq, int start)
        {
            var count = 0;
            for (var obstacleIndex = 0; obstacleIndex < Obstacles.Length; obstacleIndex++)
            {
                TryAddObstacle(agent, obstacleIndex, rangeSq, start, ref count);
            }

            return count;
        }

        private void TryAddObstacle(JobAgentData agent, int obstacleIndex, float rangeSq, int start, ref int count)
        {
            var obstacle = Obstacles[obstacleIndex];
            if ((uint)obstacle.next >= (uint)Obstacles.Length)
            {
                return;
            }

            var next = Obstacles[obstacle.next];
            var leftOf = MathUtil.LeftOf(obstacle.point, next.point, agent.position);
            if (!(leftOf < 0.0f))
            {
                return;
            }

            var distSq = MathUtil.DistSqPointLineSegment(obstacle.point, next.point, agent.position);
            if (!(distSq < rangeSq))
            {
                return;
            }

            if (count < MaxObstacleNeighborCapacity)
            {
                ObstacleNeighborIndices[start + count] = obstacleIndex;
                ObstacleNeighborDistances[start + count] = distSq;
                count++;
            }
            else if (distSq >= ObstacleNeighborDistances[start + count - 1])
            {
                return;
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

        private static bool CircleBoundsOverlap(float3 point, float radiusSq, float3 boundsMin, float3 boundsMax)
        {
            var closest = math.clamp(point, boundsMin, boundsMax);
            return math.distancesq(point, closest) < radiusSq;
        }
    }
}