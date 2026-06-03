using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace RVO.JobSystem
{
    [BurstCompile]
    internal struct BuildAgentNeighborsKdTreeJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> Positions;
        [ReadOnly] public NativeArray<int> AgentMaxNeighbors;
        [ReadOnly] public NativeArray<float> AgentNeighborDistances;
        [ReadOnly] public NativeArray<int> Permutation;
        [ReadOnly] public NativeArray<JobKdNode> Nodes;
        [ReadOnly] public NativeArray<int> NodeCount;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> NeighborIndices;
        [NativeDisableParallelForRestriction]
        public NativeArray<float> NeighborDistances;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> NeighborCounts;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> TraversalStack;
        public int MaxNeighborCapacity;
        public int MaxTraversalDepth;

        public void Execute(int index)
        {
            var start = index * MaxNeighborCapacity;
            for (var slot = 0; slot < MaxNeighborCapacity; slot++)
            {
                NeighborIndices[start + slot] = -1;
                NeighborDistances[start + slot] = float.PositiveInfinity;
            }

            var capacity = math.min(AgentMaxNeighbors[index], MaxNeighborCapacity);
            if (capacity <= 0 || NodeCount[0] <= 0)
            {
                NeighborCounts[index] = 0;
                return;
            }

            var selfPos = Positions[index];
            var rangeSq = AgentNeighborDistances[index] * AgentNeighborDistances[index];
            var count = 0;

            var stackStart = index * MaxTraversalDepth;
            var stackTop = 0;
            TraversalStack[stackStart] = 0;

            while (stackTop >= 0)
            {
                var nodeIndex = TraversalStack[stackStart + stackTop--];
                if (nodeIndex < 0 || nodeIndex >= NodeCount[0])
                {
                    continue;
                }

                var node = Nodes[nodeIndex];
                var nodeDistSq = DistSqPointAabb(selfPos, node.min, node.max);
                if (!(nodeDistSq < rangeSq))
                {
                    continue;
                }

                if (node.isLeaf == 1)
                {
                    for (var i = node.begin; i < node.end; i++)
                    {
                        var otherIndex = Permutation[i];
                        if (otherIndex == index)
                        {
                            continue;
                        }

                        var distSq = math.lengthsq(Positions[otherIndex] - selfPos);
                        if (!(distSq < rangeSq))
                        {
                            continue;
                        }

                        if (count < capacity)
                        {
                            NeighborIndices[start + count] = otherIndex;
                            NeighborDistances[start + count] = distSq;
                            count++;
                        }

                        var insert = count - 1;
                        while (insert > 0 && distSq < NeighborDistances[start + insert - 1])
                        {
                            NeighborIndices[start + insert] = NeighborIndices[start + insert - 1];
                            NeighborDistances[start + insert] = NeighborDistances[start + insert - 1];
                            insert--;
                        }

                        NeighborIndices[start + insert] = otherIndex;
                        NeighborDistances[start + insert] = distSq;

                        if (count == capacity)
                        {
                            rangeSq = NeighborDistances[start + count - 1];
                        }
                    }

                    continue;
                }

                var left = node.left;
                var right = node.right;

                if (left < 0 && right < 0)
                {
                    continue;
                }

                if (left < 0)
                {
                    if (stackTop + 1 < MaxTraversalDepth)
                    {
                        TraversalStack[stackStart + (++stackTop)] = right;
                    }
                    continue;
                }

                if (right < 0)
                {
                    if (stackTop + 1 < MaxTraversalDepth)
                    {
                        TraversalStack[stackStart + (++stackTop)] = left;
                    }
                    continue;
                }

                var leftDist = DistSqPointAabb(selfPos, Nodes[left].min, Nodes[left].max);
                var rightDist = DistSqPointAabb(selfPos, Nodes[right].min, Nodes[right].max);

                if (leftDist < rightDist)
                {
                    if (rightDist < rangeSq && stackTop + 1 < MaxTraversalDepth)
                    {
                        TraversalStack[stackStart + (++stackTop)] = right;
                    }

                    if (leftDist < rangeSq && stackTop + 1 < MaxTraversalDepth)
                    {
                        TraversalStack[stackStart + (++stackTop)] = left;
                    }
                }
                else
                {
                    if (leftDist < rangeSq && stackTop + 1 < MaxTraversalDepth)
                    {
                        TraversalStack[stackStart + (++stackTop)] = left;
                    }

                    if (rightDist < rangeSq && stackTop + 1 < MaxTraversalDepth)
                    {
                        TraversalStack[stackStart + (++stackTop)] = right;
                    }
                }
            }

            NeighborCounts[index] = count;
        }

        private static float DistSqPointAabb(float3 point, float3 min, float3 max)
        {
            var dx = math.max(0.0f, min.x - point.x) + math.max(0.0f, point.x - max.x);
            var dy = math.max(0.0f, min.y - point.y) + math.max(0.0f, point.y - max.y);
            var dz = math.max(0.0f, min.z - point.z) + math.max(0.0f, point.z - max.z);
            return dx * dx + dy * dy + dz * dz;
        }
    }
}
