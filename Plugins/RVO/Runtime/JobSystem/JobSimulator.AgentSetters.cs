using Unity.Mathematics;
using UnityEngine;

namespace RVO.JobSystem
{
    public sealed partial class JobSimulator
    {
        public float GetGlobalTime()
        {
            return _globalTime;
        }

        public float GetTimeStep()
        {
            return _timeStep;
        }

        public void SetAgentDefaults(float neighborDist, int maxNeighbors, float timeHorizon, float timeHorizonObst, float radius, float maxSpeed, Vector3 velocity)
        {
            ThrowIfDisposed();

            _defaultAgent = new ManagedAgentState
            {
                neighborDist = neighborDist,
                maxNeighbors = math.max(0, maxNeighbors),
                timeHorizon = math.max(RvoEpsilon, timeHorizon),
                timeHorizonObst = math.max(RvoEpsilon, timeHorizonObst),
                radius = math.max(0.0f, radius),
                maxSpeed = math.max(0.0f, maxSpeed),
                velocity = velocity,
                prefVelocity = velocity,
            };
        }

        public void SetAgentMaxNeighbors(int agentNo, int maxNeighbors)
        {
            var index = GetAgentIndex(agentNo);
            var agent = _agents[index];
            agent.maxNeighbors = math.max(0, maxNeighbors);
            MarkDynamicAgentDirty(index);
        }

        public void SetAgentMaxSpeed(int agentNo, float maxSpeed)
        {
            var index = GetAgentIndex(agentNo);
            var agent = _agents[index];
            agent.maxSpeed = math.max(0.0f, maxSpeed);
            MarkDynamicAgentDirty(index);
        }

        public void SetAgentNeighborDist(int agentNo, float neighborDist)
        {
            var index = GetAgentIndex(agentNo);
            var agent = _agents[index];
            agent.neighborDist = neighborDist;
            MarkDynamicAgentDirty(index);
        }

        public void SetAgentPosition(int agentNo, Vector3 position)
        {
            var index = GetAgentIndex(agentNo);
            var agent = _agents[index];
            agent.position = position;
            MarkDynamicAgentDirty(index);
        }

        public void SetAgentPrefVelocity(int agentNo, Vector3 prefVelocity)
        {
            var index = GetAgentIndex(agentNo);
            var agent = _agents[index];
            agent.prefVelocity = prefVelocity;
            MarkDynamicAgentDirty(index);
        }

        public void SetAgentRadius(int agentNo, float radius)
        {
            var index = GetAgentIndex(agentNo);
            var agent = _agents[index];
            agent.radius = math.max(0.0f, radius);
            MarkDynamicAgentDirty(index);
        }

        public void SetAgentTimeHorizon(int agentNo, float timeHorizon)
        {
            var index = GetAgentIndex(agentNo);
            var agent = _agents[index];
            agent.timeHorizon = math.max(RvoEpsilon, timeHorizon);
            MarkDynamicAgentDirty(index);
        }

        public void SetAgentTimeHorizonObst(int agentNo, float timeHorizonObst)
        {
            var index = GetAgentIndex(agentNo);
            var agent = _agents[index];
            agent.timeHorizonObst = math.max(RvoEpsilon, timeHorizonObst);
            MarkDynamicAgentDirty(index);
        }

        public void SetAgentVelocity(int agentNo, Vector3 velocity)
        {
            var index = GetAgentIndex(agentNo);
            var agent = _agents[index];
            agent.velocity = velocity;
            MarkDynamicAgentDirty(index);
        }

        public void SetGlobalTime(float globalTime)
        {
            _globalTime = globalTime;
        }

        public void SetTimeStep(float timeStep)
        {
            _timeStep = math.max(RvoEpsilon, timeStep);
        }
    }
}
