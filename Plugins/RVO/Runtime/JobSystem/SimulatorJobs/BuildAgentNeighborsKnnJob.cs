using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace RVO.JobSystem
{
    [BurstCompile]
    internal struct BuildAgentNeighborsKnnJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> Positions;
        [ReadOnly] public NativeArray<int> AgentMaxNeighbors;
        [ReadOnly] public NativeArray<float> AgentNeighborDistances;
        [ReadOnly] public NativeArray<int> CandidateIndices;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> NeighborIndices;
        [NativeDisableParallelForRestriction]
        public NativeArray<float> NeighborDistances;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> NeighborCounts;
        public int CandidateCapacity;
        public int MaxNeighborCapacity;

        public void Execute(int index)
        {
            var start = index * MaxNeighborCapacity;
            for (var slot = 0; slot < MaxNeighborCapacity; slot++)
            {
                NeighborIndices[start + slot] = -1;
                NeighborDistances[start + slot] = float.PositiveInfinity;
            }

            var capacity = math.min(AgentMaxNeighbors[index], MaxNeighborCapacity);
            if (capacity <= 0 || CandidateCapacity <= 0)
            {
                NeighborCounts[index] = 0;
                return;
            }

            var selfPos = Positions[index];
            var rangeSq = AgentNeighborDistances[index] * AgentNeighborDistances[index];
            var candidateStart = index * CandidateCapacity;
            var count = 0;

            for (var candidateSlot = 0; candidateSlot < CandidateCapacity; candidateSlot++)
            {
                var otherIndex = CandidateIndices[candidateStart + candidateSlot];
                if ((uint)otherIndex >= (uint)Positions.Length || otherIndex == index)
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

            NeighborCounts[index] = count;
        }
    }
}