using System;
using System.Collections.Generic;
using KNN;
using KNN.Jobs;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Pool;

namespace RVO.JobSystem
{
    public sealed partial class JobSimulator
    {
        private void CopyNativeBackToManaged()
        {
            for (var index = 0; index < _agents.Count; index++)
            {
                var native = _nativeAgents[index];
                var agent = _agents[index];
                agent.position = native.position;
                agent.velocity = native.velocity;
            }
            ClearDynamicAgentDirty();
        }

        private void EnsureNativeState()
        {
            if (_nativeStructureDirty)
            {
                if (!_nativeAgents.IsCreated ||
                    _nativeAgents.Length != _agents.Count ||
                    !_nativeObstacles.IsCreated ||
                    _nativeObstacles.Length != _obstacles.Count)
                {
                    ReleaseNativeState();

                    if (_agents.Count == 0)
                    {
                        _nativeStructureDirty = false;
                        _nativeObstacleDirty = false;
                        ClearDynamicAgentDirty();
                        return;
                    }

                    _nativeAgents = new NativeArray<JobAgentData>(_agents.Count, Allocator.Persistent);
                    _neighborIndices = new NativeArray<int>(_agents.Count * MaxNeighborsCapacity, Allocator.Persistent);
                    _neighborDistances = new NativeArray<float>(_agents.Count * MaxNeighborsCapacity, Allocator.Persistent);
                    _neighborCounts = new NativeArray<int>(_agents.Count, Allocator.Persistent);
                    _obstacleNeighborIndices = new NativeArray<int>(_agents.Count * MaxObstacleNeighborsCapacity, Allocator.Persistent);
                    _obstacleNeighborDistances = new NativeArray<float>(_agents.Count * MaxObstacleNeighborsCapacity, Allocator.Persistent);
                    _obstacleNeighborCounts = new NativeArray<int>(_agents.Count, Allocator.Persistent);
                    _queryPositions = new NativeArray<float3>(_agents.Count, Allocator.Persistent);
                    _queryMaxNeighbors = new NativeArray<int>(_agents.Count, Allocator.Persistent);
                    _queryNeighborDistances = new NativeArray<float>(_agents.Count, Allocator.Persistent);
                    _nativeObstacles = new NativeArray<JobObstacleData>(_obstacles.Count, Allocator.Persistent);
                    _orcaLines = new NativeArray<JobLine>(_agents.Count * (MaxNeighborsCapacity + MaxObstacleNeighborsCapacity), Allocator.Persistent);
                    _tempOrcaLines = new NativeArray<JobLine>(_agents.Count * (MaxNeighborsCapacity + MaxObstacleNeighborsCapacity), Allocator.Persistent);
                    _orcaLineCounts = new NativeArray<int>(_agents.Count, Allocator.Persistent);
                    _obstacleOrcaLineCounts = new NativeArray<int>(_agents.Count, Allocator.Persistent);
                    _outputs = new NativeArray<JobAgentOutput>(_agents.Count, Allocator.Persistent);
                    _knnCandidateCapacity = math.min(_agents.Count, MaxNeighborsCapacity + 1);
                    _knnCandidateIndices = new NativeArray<int>(_agents.Count * _knnCandidateCapacity, Allocator.Persistent);
                    _agentKnnContainer = new KnnContainer(_queryPositions, false, Allocator.Persistent);
                }

                EnsureDynamicDirtyCapacity();

                for (var index = 0; index < _agents.Count; index++)
                {
                    SyncNativeAgent(index);
                }
                ClearDynamicAgentDirty();
                _nativeStructureDirty = false;
                _nativeObstacleDirty = true;
            }
            else if (_dirtyDynamicAgentIndices.Count > 0 && _nativeAgents.IsCreated)
            {
                for (var i = 0; i < _dirtyDynamicAgentIndices.Count; i++)
                {
                    var index = _dirtyDynamicAgentIndices[i];
                    if ((uint)index >= (uint)_agents.Count)
                    {
                        continue;
                    }

                    SyncNativeAgent(index);
                }

                ClearDynamicAgentDirty();
            }

            if (_nativeObstacleDirty && _nativeObstacles.IsCreated)
            {
                EnsureNativeObstacleCapacity();

                for (var index = 0; index < _obstacles.Count; index++)
                {
                    var obstacle = _obstacles[index];
                    _nativeObstacles[index] = new JobObstacleData
                    {
                        id = obstacle.id,
                        point = ToFloat3(obstacle.point),
                        direction = ToFloat3(obstacle.direction),
                        previous = obstacle.previous,
                        next = obstacle.next,
                        convex = obstacle.convex ? (byte)1 : (byte)0,
                    };
                }

                RebuildNativeObstacleTree();
                _nativeObstacleDirty = false;
            }
        }

        private void EnsureNativeObstacleCapacity()
        {
            if (_nativeObstacles.IsCreated && _nativeObstacles.Length == _obstacles.Count)
            {
                return;
            }

            if (_nativeObstacles.IsCreated)
            {
                _nativeObstacles.Dispose();
            }

            if (_nativeObstacleTreeNodes.IsCreated)
            {
                _nativeObstacleTreeNodes.Dispose();
            }

            _nativeObstacles = new NativeArray<JobObstacleData>(_obstacles.Count, Allocator.Persistent);
        }

        private void SyncNativeAgent(int index)
        {
            var agent = _agents[index];
            _nativeAgents[index] = new JobAgentData
            {
                id = agent.id,
                agentType = agent.agentType,
                position = agent.position,
                prefVelocity = agent.prefVelocity,
                velocity = agent.velocity,
                radius = agent.radius,
                maxSpeed = agent.maxSpeed,
                neighborDist = agent.neighborDist,
                timeHorizon = agent.timeHorizon,
                timeHorizonObst = agent.timeHorizonObst,
                maxNeighbors = math.min(agent.maxNeighbors, MaxNeighborsCapacity),
            };
        }

        private void RebuildNativeObstacleTree()
        {
            if (_nativeObstacleTreeNodes.IsCreated)
            {
                _nativeObstacleTreeNodes.Dispose();
            }

            if (!_nativeObstacles.IsCreated || _nativeObstacles.Length == 0)
            {
                return;
            }

            var nodes = ListPool<JobObstacleTreeNode>.Get();
            var indices = ListPool<int>.Get();
            for (var i = 0; i < _nativeObstacles.Length; i++)
            {
                if ((uint)_nativeObstacles[i].next < (uint)_nativeObstacles.Length)
                {
                    indices.Add(i);
                }
            }

            if (indices.Count == 0)
            {
                return;
            }

            BuildNativeObstacleTreeNode(indices, nodes);
            _nativeObstacleTreeNodes = new NativeArray<JobObstacleTreeNode>(nodes.Count, Allocator.Persistent);
            for (var index = 0; index < nodes.Count; index++)
            {
                _nativeObstacleTreeNodes[index] = nodes[index];
            }
            ListPool<JobObstacleTreeNode>.Release(nodes);
            ListPool<int>.Release(indices);
        }

        private int BuildNativeObstacleTreeNode(List<int> indices, List<JobObstacleTreeNode> nodes)
        {
            var nodeIndex = nodes.Count;
            var boundsMin = new float3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            var boundsMax = new float3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            for (var i = 0; i < indices.Count; i++)
            {
                GetObstacleSegmentBounds(indices[i], out var segmentMin, out var segmentMax);
                boundsMin = math.min(boundsMin, segmentMin);
                boundsMax = math.max(boundsMax, segmentMax);
            }

            nodes.Add(new JobObstacleTreeNode
            {
                boundsMin = boundsMin,
                boundsMax = boundsMax,
                left = -1,
                right = -1,
                obstacleIndex = indices.Count == 1 ? indices[0] : -1,
            });

            if (indices.Count <= 1)
            {
                return nodeIndex;
            }

            var extents = boundsMax - boundsMin;
            var axis = extents.x >= extents.y
                ? (extents.x >= extents.z ? 0 : 2)
                : (extents.y >= extents.z ? 1 : 2);

            indices.Sort((lhs, rhs) => GetObstacleSegmentCenter(lhs)[axis].CompareTo(GetObstacleSegmentCenter(rhs)[axis]));

            var split = indices.Count / 2;
            var leftIndices = indices.GetRange(0, split);
            var rightIndices = indices.GetRange(split, indices.Count - split);
            var left = BuildNativeObstacleTreeNode(leftIndices, nodes);
            var right = BuildNativeObstacleTreeNode(rightIndices, nodes);

            var node = nodes[nodeIndex];
            node.left = left;
            node.right = right;
            nodes[nodeIndex] = node;
            return nodeIndex;
        }

        private void GetObstacleSegmentBounds(int obstacleIndex, out float3 boundsMin, out float3 boundsMax)
        {
            var obstacle = _nativeObstacles[obstacleIndex];
            var next = _nativeObstacles[obstacle.next];
            boundsMin = math.min(obstacle.point, next.point);
            boundsMax = math.max(obstacle.point, next.point);
        }

        private float3 GetObstacleSegmentCenter(int obstacleIndex)
        {
            var obstacle = _nativeObstacles[obstacleIndex];
            var next = _nativeObstacles[obstacle.next];
            return (obstacle.point + next.point) * 0.5f;
        }

        private void MarkDynamicAgentDirty(int index)
        {
            if (_nativeStructureDirty)
            {
                return;
            }

            EnsureDynamicDirtyCapacity();
            if (index < 0 || index >= _dynamicAgentDirtyMarks.Length)
            {
                _nativeStructureDirty = true;
                return;
            }

            if (_dynamicAgentDirtyMarks[index])
            {
                return;
            }

            _dynamicAgentDirtyMarks[index] = true;
            _dirtyDynamicAgentIndices.Add(index);
        }

        private void EnsureDynamicDirtyCapacity()
        {
            if (_dynamicAgentDirtyMarks.Length >= _agents.Count)
            {
                return;
            }

            var newCapacity = math.max(_agents.Count, math.max(4, _dynamicAgentDirtyMarks.Length * 2));
            Array.Resize(ref _dynamicAgentDirtyMarks, newCapacity);
        }

        private void ClearDynamicAgentDirty()
        {
            for (var i = 0; i < _dirtyDynamicAgentIndices.Count; i++)
            {
                var index = _dirtyDynamicAgentIndices[i];
                if (index >= 0 && index < _dynamicAgentDirtyMarks.Length)
                {
                    _dynamicAgentDirtyMarks[index] = false;
                }
            }

            _dirtyDynamicAgentIndices.Clear();
        }

        private void EnsureAgentQueryTree()
        {
            EnsureNativeState();

            if (!_nativeAgents.IsCreated || _nativeAgents.Length == 0)
            {
                return;
            }

            var extractQueryDataHandle = new ExtractAgentQueryDataJob
            {
                Agents = _nativeAgents,
                QueryPositions = _queryPositions,
                QueryMaxNeighbors = _queryMaxNeighbors,
                QueryNeighborDistances = _queryNeighborDistances,
            }.Schedule(_nativeAgents.Length, BatchSize);

            extractQueryDataHandle.Complete();
            new KnnRebuildJob(_agentKnnContainer).Schedule().Complete();
        }

        private void ReleaseNativeState()
        {
            if (_nativeAgents.IsCreated) _nativeAgents.Dispose();
            if (_neighborIndices.IsCreated) _neighborIndices.Dispose();
            if (_neighborDistances.IsCreated) _neighborDistances.Dispose();
            if (_neighborCounts.IsCreated) _neighborCounts.Dispose();
            if (_obstacleNeighborIndices.IsCreated) _obstacleNeighborIndices.Dispose();
            if (_obstacleNeighborDistances.IsCreated) _obstacleNeighborDistances.Dispose();
            if (_obstacleNeighborCounts.IsCreated) _obstacleNeighborCounts.Dispose();
            if (_queryPositions.IsCreated)
            {
                _queryPositions.Dispose();
                _agentKnnContainer.Dispose();
            }
            if (_queryMaxNeighbors.IsCreated) _queryMaxNeighbors.Dispose();
            if (_queryNeighborDistances.IsCreated) _queryNeighborDistances.Dispose();
            if (_nativeObstacles.IsCreated) _nativeObstacles.Dispose();
            if (_nativeObstacleTreeNodes.IsCreated) _nativeObstacleTreeNodes.Dispose();
            if (_outputs.IsCreated) _outputs.Dispose();
            if (_orcaLines.IsCreated) _orcaLines.Dispose();
            if (_tempOrcaLines.IsCreated) _tempOrcaLines.Dispose();
            if (_orcaLineCounts.IsCreated) _orcaLineCounts.Dispose();
            if (_obstacleOrcaLineCounts.IsCreated) _obstacleOrcaLineCounts.Dispose();
            if (_knnCandidateIndices.IsCreated) _knnCandidateIndices.Dispose();

            _knnCandidateCapacity = 0;
        }
    }
}
