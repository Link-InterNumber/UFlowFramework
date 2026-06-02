using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace RVO.JobSystem
{
    /// <summary>
    /// A Burst/JobSystem RVO simulator that keeps the legacy RVO implementation intact.
    ///
    /// Current scope:
    /// - Agent-agent ORCA avoidance. 
    /// - Burst parallel neighbor search using an all-pairs pass.
    /// - Burst parallel velocity solve and integration.
    /// - Kd-tree construction and traversal for neighbor search.
    ///
    /// Current limitations:
    /// - Static obstacle ORCA is not ported yet.
    /// </summary>
    public sealed class JobSimulator : IDisposable
    {
        private const int DefaultBatchSize = 32;
        private const int DefaultMaxNeighborsCapacity = 16;
        private const int DefaultMaxPointsPerKdLeaf = 32;
        private const int DefaultMaxKdTraversalDepth = 64;
        private const float RvoEpsilon = 0.00001f;

        private readonly List<ManagedAgentState> _agents = new List<ManagedAgentState>();
        private readonly Dictionary<int, int> _agentNo2Index = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _index2AgentNo = new Dictionary<int, int>();

        private NativeArray<JobAgentData> _nativeAgents;
        private NativeArray<int> _neighborIndices;
        private NativeArray<float> _neighborDistances;
        private NativeArray<int> _neighborCounts;
        private NativeArray<float3> _queryPositions;
        private NativeArray<int> _queryMaxNeighbors;
        private NativeArray<float> _queryNeighborDistances;
        private NativeArray<JobLine> _orcaLines;
        private NativeArray<JobLine> _tempOrcaLines;
        private NativeArray<int> _orcaLineCounts;
        private NativeArray<JobAgentOutput> _outputs;
        private NativeArray<int> _kdPermutation;
        private NativeArray<JobKdNode> _kdNodes;
        private NativeArray<int> _kdNodeCount;
        private NativeArray<int> _kdBuildStackNode;
        private NativeArray<int> _kdBuildStackBegin;
        private NativeArray<int> _kdBuildStackEnd;
        private NativeArray<int> _kdTraversalStack;

        private ManagedAgentState _defaultAgent;
        private bool _disposed;
        private bool _nativeDirty = true;
        private int _nextAgentId;
        private float _globalTime;
        private float _timeStep = 0.1f;

        public JobSimulator(int maxNeighborsCapacity = DefaultMaxNeighborsCapacity, int batchSize = DefaultBatchSize)
        {
            MaxNeighborsCapacity = math.max(1, maxNeighborsCapacity);
            BatchSize = math.max(1, batchSize);
        }

        public int MaxNeighborsCapacity { get; }

        public int BatchSize { get; }

        public float doStep()
        {
            ThrowIfDisposed();

            UpdateDeletedAgents();
            EnsureNativeState();

            if (_nativeAgents.Length == 0)
            {
                _globalTime += _timeStep;
                return _globalTime;
            }

            var extractQueryDataHandle = new ExtractAgentQueryDataJob
            {
                Agents = _nativeAgents,
                QueryPositions = _queryPositions,
                QueryMaxNeighbors = _queryMaxNeighbors,
                QueryNeighborDistances = _queryNeighborDistances,
            }.Schedule(_nativeAgents.Length, BatchSize);

            var kdBuildHandle = new BuildKdTreeJob
            {
                Positions = _queryPositions,
                Permutation = _kdPermutation,
                Nodes = _kdNodes,
                NodeCount = _kdNodeCount,
                StackNode = _kdBuildStackNode,
                StackBegin = _kdBuildStackBegin,
                StackEnd = _kdBuildStackEnd,
                MaxPointsPerLeaf = DefaultMaxPointsPerKdLeaf,
            }.Schedule(extractQueryDataHandle);

            var neighborHandle = new BuildAgentNeighborsKdTreeJob
            {
                Positions = _queryPositions,
                AgentMaxNeighbors = _queryMaxNeighbors,
                AgentNeighborDistances = _queryNeighborDistances,
                Permutation = _kdPermutation,
                Nodes = _kdNodes,
                NodeCount = _kdNodeCount,
                NeighborIndices = _neighborIndices,
                NeighborDistances = _neighborDistances,
                NeighborCounts = _neighborCounts,
                TraversalStack = _kdTraversalStack,
                MaxNeighborCapacity = MaxNeighborsCapacity,
                MaxTraversalDepth = DefaultMaxKdTraversalDepth,
            }.Schedule(_nativeAgents.Length, BatchSize, kdBuildHandle);

            var velocityHandle = new ComputeAgentVelocityJob
            {
                Agents = _nativeAgents,
                NeighborIndices = _neighborIndices,
                NeighborCounts = _neighborCounts,
                OrcaLines = _orcaLines,
                TempOrcaLines = _tempOrcaLines,
                OrcaLineCounts = _orcaLineCounts,
                Outputs = _outputs,
                MaxNeighborCapacity = MaxNeighborsCapacity,
                TimeStep = _timeStep,
            }.Schedule(_nativeAgents.Length, BatchSize, neighborHandle);

            var integrateHandle = new IntegrateAgentJob
            {
                Agents = _nativeAgents,
                Outputs = _outputs,
                TimeStep = _timeStep,
            }.Schedule(_nativeAgents.Length, BatchSize, velocityHandle);

            integrateHandle.Complete();

            CopyNativeBackToManaged();
            _globalTime += _timeStep;
            return _globalTime;
        }

        public void Clear()
        {
            ThrowIfDisposed();
            _agents.Clear();
            _agentNo2Index.Clear();
            _index2AgentNo.Clear();
            _defaultAgent = null;
            _nextAgentId = 0;
            _globalTime = 0.0f;
            _timeStep = 0.1f;
            _nativeDirty = true;
            ReleaseNativeState();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            ReleaseNativeState();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        public int addAgent(Vector3 position)
        {
            ThrowIfDisposed();

            if (_defaultAgent == null)
            {
                return -1;
            }

            return addAgent(
                position,
                _defaultAgent.neighborDist,
                _defaultAgent.maxNeighbors,
                _defaultAgent.timeHorizon,
                _defaultAgent.timeHorizonObst,
                _defaultAgent.radius,
                _defaultAgent.maxSpeed,
                _defaultAgent.velocity);
        }

        public int addAgent(Vector3 position, float neighborDist, int maxNeighbors, float timeHorizon, float timeHorizonObst, float radius, float maxSpeed, Vector3 velocity)
        {
            ThrowIfDisposed();

            var agent = new ManagedAgentState
            {
                id = _nextAgentId++,
                position = position,
                prefVelocity = velocity,
                velocity = velocity,
                neighborDist = neighborDist,
                maxNeighbors = math.max(0, maxNeighbors),
                timeHorizon = math.max(RvoEpsilon, timeHorizon),
                timeHorizonObst = math.max(RvoEpsilon, timeHorizonObst),
                radius = math.max(0.0f, radius),
                maxSpeed = math.max(0.0f, maxSpeed),
            };

            _agents.Add(agent);
            RegisterAgentAtIndex(_agents.Count - 1, agent.id);
            _nativeDirty = true;
            return agent.id;
        }

        public void delAgent(int agentNo)
        {
            ThrowIfDisposed();

            if (_agentNo2Index.TryGetValue(agentNo, out var index))
            {
                _agents[index].needDelete = true;
            }
        }

        public int addObstacle(IList<Vector3> vertices)
        {
            throw new NotSupportedException("JobSimulator does not implement static obstacle support yet.");
        }

        public void processObstacles()
        {
        }

        public bool queryVisibility(Vector3 point1, Vector3 point2, float radius)
        {
            throw new NotSupportedException("JobSimulator does not implement obstacle visibility queries yet.");
        }

        public int queryNearAgent(Vector3 point, float radius)
        {
            ThrowIfDisposed();

            var bestDistSq = radius * radius;
            var bestAgent = -1;
            for (var index = 0; index < _agents.Count; index++)
            {
                var distSq = (point - _agents[index].position).sqrMagnitude;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestAgent = _agents[index].id;
                }
            }

            return bestAgent;
        }

        public int getAgentAgentNeighbor(int agentNo, int neighborNo)
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
            return _index2AgentNo[neighborIndex];
        }

        public int getAgentMaxNeighbors(int agentNo)
        {
            return _agents[GetAgentIndex(agentNo)].maxNeighbors;
        }

        public float getAgentMaxSpeed(int agentNo)
        {
            return _agents[GetAgentIndex(agentNo)].maxSpeed;
        }

        public float getAgentNeighborDist(int agentNo)
        {
            return _agents[GetAgentIndex(agentNo)].neighborDist;
        }

        public int getAgentNumAgentNeighbors(int agentNo)
        {
            var index = GetAgentIndex(agentNo);
            if (!_neighborCounts.IsCreated || index >= _neighborCounts.Length)
            {
                return 0;
            }

            return _neighborCounts[index];
        }

        public int getAgentNumObstacleNeighbors(int agentNo)
        {
            return 0;
        }

        public Vector3 getAgentPosition(int agentNo)
        {
            return _agents[GetAgentIndex(agentNo)].position;
        }

        public Vector3 getAgentPrefVelocity(int agentNo)
        {
            return _agents[GetAgentIndex(agentNo)].prefVelocity;
        }

        public float getAgentRadius(int agentNo)
        {
            return _agents[GetAgentIndex(agentNo)].radius;
        }

        public float getAgentTimeHorizon(int agentNo)
        {
            return _agents[GetAgentIndex(agentNo)].timeHorizon;
        }

        public float getAgentTimeHorizonObst(int agentNo)
        {
            return _agents[GetAgentIndex(agentNo)].timeHorizonObst;
        }

        public Vector3 getAgentVelocity(int agentNo)
        {
            return _agents[GetAgentIndex(agentNo)].velocity;
        }

        public float getGlobalTime()
        {
            return _globalTime;
        }

        public int getNumAgents()
        {
            return _agents.Count;
        }

        public int getNumObstacleVertices()
        {
            return 0;
        }

        public Vector3 getObstacleVertex(int vertexNo)
        {
            throw new NotSupportedException("JobSimulator does not implement static obstacle support yet.");
        }

        public int getNextObstacleVertexNo(int vertexNo)
        {
            throw new NotSupportedException("JobSimulator does not implement static obstacle support yet.");
        }

        public int getPrevObstacleVertexNo(int vertexNo)
        {
            throw new NotSupportedException("JobSimulator does not implement static obstacle support yet.");
        }

        public float getTimeStep()
        {
            return _timeStep;
        }

        public void setAgentDefaults(float neighborDist, int maxNeighbors, float timeHorizon, float timeHorizonObst, float radius, float maxSpeed, Vector3 velocity)
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

        public void setAgentMaxNeighbors(int agentNo, int maxNeighbors)
        {
            var agent = _agents[GetAgentIndex(agentNo)];
            agent.maxNeighbors = math.max(0, maxNeighbors);
            _nativeDirty = true;
        }

        public void setAgentMaxSpeed(int agentNo, float maxSpeed)
        {
            var agent = _agents[GetAgentIndex(agentNo)];
            agent.maxSpeed = math.max(0.0f, maxSpeed);
            _nativeDirty = true;
        }

        public void setAgentNeighborDist(int agentNo, float neighborDist)
        {
            var agent = _agents[GetAgentIndex(agentNo)];
            agent.neighborDist = neighborDist;
            _nativeDirty = true;
        }

        public void setAgentPosition(int agentNo, Vector3 position)
        {
            var agent = _agents[GetAgentIndex(agentNo)];
            agent.position = position;
            _nativeDirty = true;
        }

        public void setAgentPrefVelocity(int agentNo, Vector3 prefVelocity)
        {
            var agent = _agents[GetAgentIndex(agentNo)];
            agent.prefVelocity = prefVelocity;
            _nativeDirty = true;
        }

        public void setAgentRadius(int agentNo, float radius)
        {
            var agent = _agents[GetAgentIndex(agentNo)];
            agent.radius = math.max(0.0f, radius);
            _nativeDirty = true;
        }

        public void setAgentTimeHorizon(int agentNo, float timeHorizon)
        {
            var agent = _agents[GetAgentIndex(agentNo)];
            agent.timeHorizon = math.max(RvoEpsilon, timeHorizon);
            _nativeDirty = true;
        }

        public void setAgentTimeHorizonObst(int agentNo, float timeHorizonObst)
        {
            var agent = _agents[GetAgentIndex(agentNo)];
            agent.timeHorizonObst = math.max(RvoEpsilon, timeHorizonObst);
            _nativeDirty = true;
        }

        public void setAgentVelocity(int agentNo, Vector3 velocity)
        {
            var agent = _agents[GetAgentIndex(agentNo)];
            agent.velocity = velocity;
            _nativeDirty = true;
        }

        public void setGlobalTime(float globalTime)
        {
            _globalTime = globalTime;
        }

        public void setTimeStep(float timeStep)
        {
            _timeStep = math.max(RvoEpsilon, timeStep);
        }

        private void CopyNativeBackToManaged()
        {
            for (var index = 0; index < _agents.Count; index++)
            {
                var native = _nativeAgents[index];
                var agent = _agents[index];
                agent.position = ToVector3(native.position);
                agent.velocity = ToVector3(native.velocity);
            }

            _nativeDirty = false;
        }

        private void EnsureNativeState()
        {
            if (_nativeDirty)
            {
                if (!_nativeAgents.IsCreated || _nativeAgents.Length != _agents.Count)
                {
                    ReleaseNativeState();

                    if (_agents.Count == 0)
                    {
                        _nativeDirty = false;
                        return;
                    }

                    _nativeAgents = new NativeArray<JobAgentData>(_agents.Count, Allocator.Persistent);
                    _neighborIndices = new NativeArray<int>(_agents.Count * MaxNeighborsCapacity, Allocator.Persistent);
                    _neighborDistances = new NativeArray<float>(_agents.Count * MaxNeighborsCapacity, Allocator.Persistent);
                    _neighborCounts = new NativeArray<int>(_agents.Count, Allocator.Persistent);
                    _queryPositions = new NativeArray<float3>(_agents.Count, Allocator.Persistent);
                    _queryMaxNeighbors = new NativeArray<int>(_agents.Count, Allocator.Persistent);
                    _queryNeighborDistances = new NativeArray<float>(_agents.Count, Allocator.Persistent);
                    _orcaLines = new NativeArray<JobLine>(_agents.Count * MaxNeighborsCapacity, Allocator.Persistent);
                    _tempOrcaLines = new NativeArray<JobLine>(_agents.Count * MaxNeighborsCapacity, Allocator.Persistent);
                    _orcaLineCounts = new NativeArray<int>(_agents.Count, Allocator.Persistent);
                    _outputs = new NativeArray<JobAgentOutput>(_agents.Count, Allocator.Persistent);
                    _kdPermutation = new NativeArray<int>(_agents.Count, Allocator.Persistent);
                    _kdNodes = new NativeArray<JobKdNode>(_agents.Count * 2, Allocator.Persistent);
                    _kdNodeCount = new NativeArray<int>(1, Allocator.Persistent);
                    _kdBuildStackNode = new NativeArray<int>(_agents.Count * 2, Allocator.Persistent);
                    _kdBuildStackBegin = new NativeArray<int>(_agents.Count * 2, Allocator.Persistent);
                    _kdBuildStackEnd = new NativeArray<int>(_agents.Count * 2, Allocator.Persistent);
                    _kdTraversalStack = new NativeArray<int>(_agents.Count * DefaultMaxKdTraversalDepth, Allocator.Persistent);
                }

                for (var index = 0; index < _agents.Count; index++)
                {
                    var agent = _agents[index];
                    _nativeAgents[index] = new JobAgentData
                    {
                        id = agent.id,
                        position = ToFloat3(agent.position),
                        prefVelocity = ToFloat3(agent.prefVelocity),
                        velocity = ToFloat3(agent.velocity),
                        radius = agent.radius,
                        maxSpeed = agent.maxSpeed,
                        neighborDist = agent.neighborDist,
                        timeHorizon = agent.timeHorizon,
                        timeHorizonObst = agent.timeHorizonObst,
                        maxNeighbors = math.min(agent.maxNeighbors, MaxNeighborsCapacity),
                    };
                }

                _nativeDirty = false;
            }
        }

        private int GetAgentIndex(int agentNo)
        {
            ThrowIfDisposed();

            if (_agentNo2Index.TryGetValue(agentNo, out var index))
            {
                return index;
            }

            throw new KeyNotFoundException($"Agent {agentNo} was not found.");
        }

        private void RebuildAgentLookup()
        {
            _agentNo2Index.Clear();
            _index2AgentNo.Clear();

            for (var index = 0; index < _agents.Count; index++)
            {
                RegisterAgentAtIndex(index, _agents[index].id);
            }
        }

        private void RegisterAgentAtIndex(int index, int agentNo)
        {
            _agentNo2Index[agentNo] = index;
            _index2AgentNo[index] = agentNo;
        }

        private void ReleaseNativeState()
        {
            if (_nativeAgents.IsCreated)
            {
                _nativeAgents.Dispose();
            }

            if (_neighborIndices.IsCreated)
            {
                _neighborIndices.Dispose();
            }

            if (_neighborDistances.IsCreated)
            {
                _neighborDistances.Dispose();
            }

            if (_neighborCounts.IsCreated)
            {
                _neighborCounts.Dispose();
            }

            if (_queryPositions.IsCreated)
            {
                _queryPositions.Dispose();
            }

            if (_queryMaxNeighbors.IsCreated)
            {
                _queryMaxNeighbors.Dispose();
            }

            if (_queryNeighborDistances.IsCreated)
            {
                _queryNeighborDistances.Dispose();
            }

            if (_outputs.IsCreated)
            {
                _outputs.Dispose();
            }

            if (_orcaLines.IsCreated)
            {
                _orcaLines.Dispose();
            }

            if (_tempOrcaLines.IsCreated)
            {
                _tempOrcaLines.Dispose();
            }

            if (_orcaLineCounts.IsCreated)
            {
                _orcaLineCounts.Dispose();
            }

            if (_kdPermutation.IsCreated)
            {
                _kdPermutation.Dispose();
            }

            if (_kdNodes.IsCreated)
            {
                _kdNodes.Dispose();
            }

            if (_kdNodeCount.IsCreated)
            {
                _kdNodeCount.Dispose();
            }

            if (_kdBuildStackNode.IsCreated)
            {
                _kdBuildStackNode.Dispose();
            }

            if (_kdBuildStackBegin.IsCreated)
            {
                _kdBuildStackBegin.Dispose();
            }

            if (_kdBuildStackEnd.IsCreated)
            {
                _kdBuildStackEnd.Dispose();
            }

            if (_kdTraversalStack.IsCreated)
            {
                _kdTraversalStack.Dispose();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(JobSimulator));
            }
        }

        private void UpdateDeletedAgents()
        {
            var removed = false;
            for (var index = _agents.Count - 1; index >= 0; index--)
            {
                if (_agents[index].needDelete)
                {
                    _agents.RemoveAt(index);
                    removed = true;
                }
            }

            if (removed)
            {
                RebuildAgentLookup();
                _nativeDirty = true;
            }
        }

        private static float3 ToFloat3(Vector3 value)
        {
            return new float3(value.x, value.y, value.z);
        }

        private static Vector3 ToVector3(float3 value)
        {
            return new Vector3(value.x, value.y, value.z);
        }

        private sealed class ManagedAgentState
        {
            public int id;
            public int maxNeighbors;
            public float maxSpeed;
            public float neighborDist;
            public float radius;
            public float timeHorizon;
            public float timeHorizonObst;
            public bool needDelete;
            public Vector3 position;
            public Vector3 prefVelocity;
            public Vector3 velocity;
        }

        private struct JobAgentData
        {
            public int id;
            public int maxNeighbors;
            public float maxSpeed;
            public float neighborDist;
            public float radius;
            public float timeHorizon;
            public float timeHorizonObst;
            public float3 position;
            public float3 prefVelocity;
            public float3 velocity;
        }

        private struct JobAgentOutput
        {
            public float3 newVelocity;
        }

        private struct JobLine
        {
            public float3 point;
            public float3 direction;
        }

        [BurstCompile]
        private struct ExtractAgentQueryDataJob : IJobParallelFor
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

        [BurstCompile]
        private struct ComputeAgentVelocityJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<JobAgentData> Agents;
            [ReadOnly] public NativeArray<int> NeighborIndices;
            [ReadOnly] public NativeArray<int> NeighborCounts;
            [NativeDisableParallelForRestriction]
            public NativeArray<JobLine> OrcaLines;
            [NativeDisableParallelForRestriction]
            public NativeArray<JobLine> TempOrcaLines;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> OrcaLineCounts;
            [NativeDisableParallelForRestriction]
            public NativeArray<JobAgentOutput> Outputs;
            public int MaxNeighborCapacity;
            public float TimeStep;

            public void Execute(int index)
            {
                var agent = Agents[index];
                var start = index * MaxNeighborCapacity;
                var neighborCount = NeighborCounts[index];
                var invTimeHorizon = 1.0f / math.max(agent.timeHorizon, RvoEpsilon);
                var lineCount = 0;

                for (var slot = 0; slot < neighborCount; slot++)
                {
                    var otherIndex = NeighborIndices[start + slot];
                    if (otherIndex < 0)
                    {
                        continue;
                    }

                    var other = Agents[otherIndex];
                    var relativePosition = other.position - agent.position;
                    var relativeVelocity = agent.velocity - other.velocity;
                    var distSq = MathUtil.AbsSq(relativePosition);
                    var combinedRadius = agent.radius + other.radius;
                    var combinedRadiusSq = combinedRadius * combinedRadius;

                    JobLine line;
                    float3 u;

                    if (distSq > combinedRadiusSq)
                    {
                        var w = relativeVelocity - invTimeHorizon * relativePosition;
                        var wLengthSq = MathUtil.AbsSq(w);
                        var dotProduct1 = math.dot(w, relativePosition);

                        if (dotProduct1 < 0.0f && dotProduct1 * dotProduct1 > combinedRadiusSq * wLengthSq)
                        {
                            var wLength = math.sqrt(wLengthSq);
                            var unitW = wLength > RvoEpsilon ? w / wLength : float3.zero;

                            line.direction = new float3(unitW.y, -unitW.x, 0.0f);
                            u = (combinedRadius * invTimeHorizon - wLength) * unitW;
                        }
                        else
                        {
                            var leg = math.sqrt(math.max(0.0f, distSq - combinedRadiusSq));

                            if (MathUtil.Det(relativePosition, w) > 0.0f)
                            {
                                line.direction = new float3(
                                    relativePosition.x * leg - relativePosition.y * combinedRadius,
                                    relativePosition.x * combinedRadius + relativePosition.y * leg,
                                    0.0f) / math.max(distSq, RvoEpsilon);
                            }
                            else
                            {
                                line.direction = -new float3(
                                    relativePosition.x * leg + relativePosition.y * combinedRadius,
                                    -relativePosition.x * combinedRadius + relativePosition.y * leg,
                                    0.0f) / math.max(distSq, RvoEpsilon);
                            }

                            var dotProduct2 = math.dot(relativeVelocity, line.direction);
                            u = dotProduct2 * line.direction - relativeVelocity;
                        }
                    }
                    else
                    {
                        var invTimeStep = 1.0f / math.max(TimeStep, RvoEpsilon);
                        var w = relativeVelocity - invTimeStep * relativePosition;
                        var wLength = math.length(w);
                        var unitW = wLength > RvoEpsilon ? w / wLength : float3.zero;

                        line.direction = new float3(unitW.y, -unitW.x, 0.0f);
                        u = (combinedRadius * invTimeStep - wLength) * unitW;
                    }

                    line.point = agent.velocity + 0.5f * u;
                    if (lineCount < MaxNeighborCapacity)
                    {
                        OrcaLines[start + lineCount] = line;
                        lineCount++;
                    }
                }

                OrcaLineCounts[index] = lineCount;

                var newVelocity = agent.prefVelocity;
                var lineFail = LinearProgram2(OrcaLines, start, lineCount, agent.maxSpeed, agent.prefVelocity, false, ref newVelocity);
                if (lineFail < lineCount)
                {
                    LinearProgram3(OrcaLines, TempOrcaLines, start, start, lineCount, 0, lineFail, agent.maxSpeed, ref newVelocity);
                }

                Outputs[index] = new JobAgentOutput
                {
                    newVelocity = newVelocity,
                };
            }

            private static bool LinearProgram1(NativeArray<JobLine> lines, int lineStart, int lineNo, float radius, float3 optVelocity, bool directionOpt, ref float3 result)
            {
                var line = lines[lineStart + lineNo];
                var dotProduct = math.dot(line.point, line.direction);
                var discriminant = dotProduct * dotProduct + radius * radius - MathUtil.AbsSq(line.point);

                if (discriminant < 0.0f)
                {
                    return false;
                }

                var sqrtDiscriminant = math.sqrt(discriminant);
                var tLeft = -dotProduct - sqrtDiscriminant;
                var tRight = -dotProduct + sqrtDiscriminant;

                for (var index = 0; index < lineNo; index++)
                {
                    var otherLine = lines[lineStart + index];
                    var denominator = MathUtil.Det(line.direction, otherLine.direction);
                    var numerator = MathUtil.Det(otherLine.direction, line.point - otherLine.point);

                    if (math.abs(denominator) <= RvoEpsilon)
                    {
                        if (numerator < 0.0f)
                        {
                            return false;
                        }

                        continue;
                    }

                    var t = numerator / denominator;
                    if (denominator >= 0.0f)
                    {
                        tRight = math.min(tRight, t);
                    }
                    else
                    {
                        tLeft = math.max(tLeft, t);
                    }

                    if (tLeft > tRight)
                    {
                        return false;
                    }
                }

                if (directionOpt)
                {
                    result = math.dot(optVelocity, line.direction) > 0.0f
                        ? line.point + tRight * line.direction
                        : line.point + tLeft * line.direction;
                    return true;
                }

                var projectedT = math.dot(line.direction, optVelocity - line.point);
                if (projectedT < tLeft)
                {
                    result = line.point + tLeft * line.direction;
                }
                else if (projectedT > tRight)
                {
                    result = line.point + tRight * line.direction;
                }
                else
                {
                    result = line.point + projectedT * line.direction;
                }

                return true;
            }

            private static int LinearProgram2(NativeArray<JobLine> lines, int lineStart, int lineCount, float radius, float3 optVelocity, bool directionOpt, ref float3 result)
            {
                if (directionOpt)
                {
                    result = optVelocity * radius;
                }
                else if (MathUtil.AbsSq(optVelocity) > radius * radius)
                {
                    result = MathUtil.NormalizeSafe(optVelocity) * radius;
                }
                else
                {
                    result = optVelocity;
                }

                for (var index = 0; index < lineCount; index++)
                {
                    var line = lines[lineStart + index];
                    if (!(MathUtil.Det(line.direction, line.point - result) > 0.0f))
                    {
                        continue;
                    }

                    var tempResult = result;
                    if (!LinearProgram1(lines, lineStart, index, radius, optVelocity, directionOpt, ref result))
                    {
                        result = tempResult;
                        return index;
                    }
                }

                return lineCount;
            }

            private static void LinearProgram3(NativeArray<JobLine> lines, NativeArray<JobLine> tempLines, int lineStart, int tempStart, int lineCount, int numObstacleLines, int beginLine, float radius, ref float3 result)
            {
                var distance = 0.0f;

                for (var index = beginLine; index < lineCount; index++)
                {
                    var line = lines[lineStart + index];
                    if (!(MathUtil.Det(line.direction, line.point - result) > distance))
                    {
                        continue;
                    }

                    var projectionLineCount = 0;
                    for (var obstacleIndex = 0; obstacleIndex < numObstacleLines; obstacleIndex++)
                    {
                        tempLines[tempStart + projectionLineCount] = lines[lineStart + obstacleIndex];
                        projectionLineCount++;
                    }

                    for (var previous = numObstacleLines; previous < index; previous++)
                    {
                        var previousLine = lines[lineStart + previous];
                        JobLine projectedLine;
                        var determinant = MathUtil.Det(line.direction, previousLine.direction);

                        if (math.abs(determinant) <= RvoEpsilon)
                        {
                            if (math.dot(line.direction, previousLine.direction) > 0.0f)
                            {
                                continue;
                            }

                            projectedLine.point = 0.5f * (line.point + previousLine.point);
                        }
                        else
                        {
                            projectedLine.point = line.point + (MathUtil.Det(previousLine.direction, line.point - previousLine.point) / determinant) * line.direction;
                        }

                        projectedLine.direction = MathUtil.NormalizeSafe(previousLine.direction - line.direction);
                        tempLines[tempStart + projectionLineCount] = projectedLine;
                        projectionLineCount++;
                    }

                    var tempResult = result;
                    var direction = new float3(-line.direction.y, line.direction.x, 0.0f);
                    if (LinearProgram2(tempLines, tempStart, projectionLineCount, radius, direction, true, ref result) < projectionLineCount)
                    {
                        result = tempResult;
                    }

                    distance = MathUtil.Det(line.direction, line.point - result);
                }
            }
        }

        [BurstCompile]
        private struct IntegrateAgentJob : IJobParallelFor
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

        private static class MathUtil
        {
            public static float AbsSq(float3 value)
            {
                return math.dot(value, value);
            }

            public static float Det(float3 lhs, float3 rhs)
            {
                return lhs.x * rhs.y - lhs.y * rhs.x;
            }

            public static float3 NormalizeSafe(float3 value)
            {
                var lengthSq = AbsSq(value);
                if (lengthSq <= RvoEpsilon * RvoEpsilon)
                {
                    return float3.zero;
                }

                return value * math.rsqrt(lengthSq);
            }
        }
    }
}