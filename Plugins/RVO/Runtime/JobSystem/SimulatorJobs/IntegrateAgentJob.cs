using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace RVO.JobSystem
{
    [BurstCompile]
    internal struct IntegrateAgentJob : IJobParallelFor
    {
        public NativeArray<JobAgentData> Agents;
        [ReadOnly] public NativeArray<JobAgentOutput> Outputs;
        public float TimeStep;

        public void Execute(int index)
        {
            var agent = Agents[index];
            var output = Outputs[index];
            agent.velocity = output.newVelocity;
            agent.position += output.newVelocity * TimeStep;
            Agents[index] = agent;
        }
    }
}
