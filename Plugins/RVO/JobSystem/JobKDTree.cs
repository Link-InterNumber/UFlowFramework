using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace RVO.JobSystem
{
    internal struct JobKdNode
    {
        public int begin;
        public int end;
        public int left;
        public int right;
        public int axis;
        public float split;
        public float3 min;
        public float3 max;
        public byte isLeaf;
    }

    [BurstCompile]
    internal struct BuildKdTreeJob : IJob
    {
        [ReadOnly] public NativeArray<float3> Positions;
        public NativeArray<int> Permutation;
        public NativeArray<JobKdNode> Nodes;
        public NativeArray<int> NodeCount;
        public NativeArray<int> StackNode;
        public NativeArray<int> StackBegin;
        public NativeArray<int> StackEnd;
        public int MaxPointsPerLeaf;

        public void Execute()
        {
            var count = Positions.Length;
            if (count == 0)
            {
                NodeCount[0] = 0;
                return;
            }

            for (var index = 0; index < count; index++)
            {
                Permutation[index] = index;
            }

            var nodeCount = 1;
            var stackTop = 0;
            StackNode[0] = 0;
            StackBegin[0] = 0;
            StackEnd[0] = count;

            while (stackTop >= 0)
            {
                var nodeIndex = StackNode[stackTop];
                var begin = StackBegin[stackTop];
                var end = StackEnd[stackTop];
                stackTop--;

                var boundsMin = new float3(float.MaxValue, float.MaxValue, float.MaxValue);
                var boundsMax = new float3(float.MinValue, float.MinValue, float.MinValue);
                for (var scan = begin; scan < end; scan++)
                {
                    var position = Positions[Permutation[scan]];
                    boundsMin = math.min(boundsMin, position);
                    boundsMax = math.max(boundsMax, position);
                }

                var node = new JobKdNode
                {
                    begin = begin,
                    end = end,
                    left = -1,
                    right = -1,
                    axis = 0,
                    split = 0.0f,
                    min = boundsMin,
                    max = boundsMax,
                    isLeaf = 1,
                };

                if (end - begin > MaxPointsPerLeaf)
                {
                    var size = boundsMax - boundsMin;
                    var splitAxis = 0;
                    var axisSize = size.x;
                    if (axisSize < size.y)
                    {
                        splitAxis = 1;
                        axisSize = size.y;
                    }

                    if (axisSize < size.z)
                    {
                        splitAxis = 2;
                    }

                    var splitPivot = 0.5f * (boundsMin[splitAxis] + boundsMax[splitAxis]);
                    var splitIndex = Partition(begin, end, splitAxis, splitPivot);

                    if (splitIndex == begin)
                    {
                        splitIndex++;
                    }
                    else if (splitIndex == end)
                    {
                        splitIndex--;
                    }

                    if (splitIndex > begin && splitIndex < end)
                    {
                        var leftNode = nodeCount++;
                        var rightNode = nodeCount++;

                        node.axis = splitAxis;
                        node.split = splitPivot;
                        node.left = leftNode;
                        node.right = rightNode;
                        node.isLeaf = 0;

                        stackTop++;
                        StackNode[stackTop] = rightNode;
                        StackBegin[stackTop] = splitIndex;
                        StackEnd[stackTop] = end;

                        stackTop++;
                        StackNode[stackTop] = leftNode;
                        StackBegin[stackTop] = begin;
                        StackEnd[stackTop] = splitIndex;
                    }
                }

                Nodes[nodeIndex] = node;
            }

            NodeCount[0] = nodeCount;
        }

        private int Partition(int begin, int end, int axis, float pivot)
        {
            var left = begin;
            var right = end;

            while (true)
            {
                while (left < right && Positions[Permutation[left]][axis] < pivot)
                {
                    left++;
                }

                while (right > left && Positions[Permutation[right - 1]][axis] >= pivot)
                {
                    right--;
                }

                if (left >= right)
                {
                    return left;
                }

                var temp = Permutation[left];
                Permutation[left] = Permutation[right - 1];
                Permutation[right - 1] = temp;
                left++;
                right--;
            }
        }
    }

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