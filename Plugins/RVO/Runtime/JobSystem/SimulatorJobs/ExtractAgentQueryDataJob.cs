using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace RVO.JobSystem
{
    [BurstCompile]
    internal struct ExtractAgentQueryDataJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<JobAgentData> Agents;
        public NativeArray<float3> QueryPositions;
        public NativeArray<int> QueryMaxNeighbors;
        public NativeArray<float> QueryNeighborDistances;

        public void Execute(int index)
        {
            var agent = Agents[index];
            QueryPositions[index] = agent.position;
            QueryMaxNeighbors[index] = agent.maxNeighbors;
            QueryNeighborDistances[index] = agent.neighborDist;
        }
    }
}
