using System;
using System.Collections.Generic;
using KNN.Jobs;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace RVO.JobSystem
{
    public sealed partial class JobSimulator
    {
        public int AddAgent(Vector3 position)
        {
            ThrowIfDisposed();

            if (_defaultAgent == null)
            {
                return -1;
            }

            return AddAgent(
                position,
                _defaultAgent.neighborDist,
                _defaultAgent.maxNeighbors,
                _defaultAgent.timeHorizon,
                _defaultAgent.timeHorizonObst,
                _defaultAgent.radius,
                _defaultAgent.maxSpeed,
                _defaultAgent.velocity);
        }

        public int AddAgent(Vector3 position, float neighborDist, int maxNeighbors, float timeHorizon, float timeHorizonObst, float radius, float maxSpeed, Vector3 velocity, int agentType = 0)
        {
            if (agentType < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(agentType));
            }
            if (agentType > 0 && !ExtraRadii.IsCreated)
            {
                throw new InvalidOperationException("Call ConfigAgentTypes before adding agents with agent types.");
            }
            if (agentType > 0 && agentType >= _agentTypeCount)
            {
                throw new ArgumentOutOfRangeException(nameof(agentType), $"Agent type must be less than {_agentTypeCount}.");
            }

            ThrowIfDisposed();
            var agent = _managedAgentPool.Get();
            agent.needDelete = false;
            agent.position = position;
            agent.prefVelocity = velocity;
            agent.velocity = velocity;
            agent.neighborDist = neighborDist;
            agent.maxNeighbors = math.max(0, maxNeighbors);
            agent.timeHorizon = math.max(RvoEpsilon, timeHorizon);
            agent.timeHorizonObst = math.max(RvoEpsilon, timeHorizonObst);
            agent.radius = math.max(0.0f, radius);
            agent.maxSpeed = math.max(0.0f, maxSpeed);
            agent.agentType = agentType;

            _agents.Add(agent);
            _nativeStructureDirty = true;
            return agent.id;
        }

        public void DelAgent(int agentNo)
        {
            ThrowIfDisposed();
            var agent = _agents.FindOrDefault(agentNo);
            if (agent != null)
                agent.needDelete = true;
        }

        public int QueryNearAgent(Vector3 point, float radius)
        {
            ThrowIfDisposed();

            if (_agents.Count == 0)
            {
                return -1;
            }

            CompleteAllJobs();

            var queryPoint = ToFloat3(point);
            EnsureAgentQueryTree();

            using (var result = new NativeArray<int>(1, Allocator.TempJob))
            {
                new QueryKNearestJob(_agentKnnContainer, queryPoint, result).Schedule().Complete();
                var bestAgentIndex = result[0];
                if ((uint)bestAgentIndex >= (uint)_agents.Count)
                {
                    return -1;
                }

                var bestDistSq = math.distancesq(queryPoint, _queryPositions[bestAgentIndex]);
                return bestDistSq <= radius * radius ? _agents[bestAgentIndex].id : -1;
            }
        }

        public int GetAgentAgentNeighbor(int agentNo, int neighborNo)
        {
            var index = GetAgentIndex(agentNo);
            if (!_neighborCounts.IsCreated || index >= _neighborCounts.Length)
            {
                throw new InvalidOperationException("Agent neighbors are available after doStep().");
            }

            var count = _neighborCounts[index];
            if (neighborNo < 0 || neighborNo >= count)
            {
                throw new ArgumentOutOfRangeException(nameof(neighborNo));
            }

            var neighborIndex = _neighborIndices[index * MaxNeighborsCapacity + neighborNo];
            return _agents[neighborIndex].id;
        }

        public int GetAgentMaxNeighbors(int agentNo)
        {
            return _agents.FindOrDefault(agentNo)?.maxNeighbors ?? 0;
        }

        public float GetAgentMaxSpeed(int agentNo)
        {
            return _agents.FindOrDefault(agentNo)?.maxSpeed ?? 0f;
        }

        public float GetAgentNeighborDist(int agentNo)
        {
            return _agents.FindOrDefault(agentNo)?.neighborDist ?? 0;
        }

        public int GetAgentNumAgentNeighbors(int agentNo)
        {
            var index = GetAgentIndex(agentNo);
            if (!_neighborCounts.IsCreated || index >= _neighborCounts.Length)
            {
                return 0;
            }

            return _neighborCounts[index];
        }

        public int GetAgentNumObstacleNeighbors(int agentNo)
        {
            var index = GetAgentIndex(agentNo);
            if (!_obstacleNeighborCounts.IsCreated || index >= _obstacleNeighborCounts.Length)
            {
                return 0;
            }

            return _obstacleNeighborCounts[index];
        }

        public int GetAgentObstacleNeighbor(int agentNo, int neighborNo)
        {
            var index = GetAgentIndex(agentNo);
            if (!_obstacleNeighborCounts.IsCreated || index >= _obstacleNeighborCounts.Length)
            {
                throw new InvalidOperationException("Obstacle neighbors are available after doStep().");
            }

            var count = _obstacleNeighborCounts[index];
            if (neighborNo < 0 || neighborNo >= count)
            {
                throw new ArgumentOutOfRangeException(nameof(neighborNo));
            }

            return _obstacleNeighborIndices[index * MaxObstacleNeighborsCapacity + neighborNo];
        }

        public Vector3 GetAgentPosition(int agentNo)
        {
            var agent = _agents.FindOrDefault(agentNo);
            if (agent == null)
            {
                throw new KeyNotFoundException();
            }
            return agent.position;
        }

        public Vector3 GetAgentPrefVelocity(int agentNo)
        {
            return _agents.FindOrDefault(agentNo)?.prefVelocity ?? default;
        }

        public float GetAgentRadius(int agentNo)
        {
            return _agents.FindOrDefault(agentNo)?.radius ?? 0f;
        }

        public float GetAgentTimeHorizon(int agentNo)
        {
            return _agents.FindOrDefault(agentNo)?.timeHorizon ?? 0f;
        }

        public float GetAgentTimeHorizonObst(int agentNo)
        {
            return _agents.FindOrDefault(agentNo)?.timeHorizonObst ?? 0f;
        }

        public Vector3 GetAgentVelocity(int agentNo)
        {
            return _agents.FindOrDefault(agentNo)?.velocity ?? default;
        }

        public int GetNumAgents()
        {
            return _agents.Count;
        }
    }
}
