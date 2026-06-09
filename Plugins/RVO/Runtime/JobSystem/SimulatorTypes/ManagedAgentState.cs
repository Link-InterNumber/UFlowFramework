using Unity.Mathematics;
using UnityEngine.Pool;

namespace RVO.JobSystem
{
    internal sealed class ManagedAgentState
    {
        public int id;
        public int agentType;
        public int maxNeighbors;
        public float maxSpeed;
        public float neighborDist;
        public float radius;
        public float timeHorizon;
        public float timeHorizonObst;
        public bool needDelete;
        public float3 position;
        public float3 prefVelocity;
        public float3 velocity;
    }
}
