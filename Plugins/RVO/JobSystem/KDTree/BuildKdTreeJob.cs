using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace RVO.JobSystem
{
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
}
