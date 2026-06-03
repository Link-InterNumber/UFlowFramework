/*
 * Simulator.cs
 * RVO2 Library C#
 *
 * Copyright 2008 University of North Carolina at Chapel Hill
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 * Please send all bug reports to <geom@cs.unc.edu>.
 *
 * The authors may be contacted via:
 *
 * Jur van den Berg, Stephen J. Guy, Jamie Snape, Ming C. Lin, Dinesh Manocha
 * Dept. of Computer Science
 * 201 S. Columbia St.
 * Frederick P. Brooks, Jr. Computer Science Bldg.
 * Chapel Hill, N.C. 27599-3175
 * United States of America
 *
 * <http://gamma.cs.unc.edu/RVO2/>
 */

using System;
using System.Collections.Generic;
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
    /// - Static obstacle ORCA avoidance.
    /// - Burst parallel neighbor search using an all-pairs pass.
    /// - Burst parallel velocity solve and integration.
    /// - Kd-tree construction and traversal for neighbor search.
    /// </summary>
    public sealed partial class JobSimulator : IDisposable
    {
        private const int DefaultBatchSize = 32;
        private const int DefaultMaxNeighborsCapacity = 16;
        private const int DefaultMaxObstacleNeighborsCapacity = 16;
        private const int DefaultMaxPointsPerKdLeaf = 32;
        private const int DefaultMaxKdTraversalDepth = 64;
        private const float RvoEpsilon = 0.00001f;

        private readonly List<ManagedAgentState> _agents = new List<ManagedAgentState>();
        private readonly List<ManagedObstacleState> _obstacles = new List<ManagedObstacleState>();
        private readonly Dictionary<int, int> _agentNo2Index = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _index2AgentNo = new Dictionary<int, int>();

        private NativeArray<JobAgentData> _nativeAgents;
        private NativeArray<int> _neighborIndices;
        private NativeArray<float> _neighborDistances;
        private NativeArray<int> _neighborCounts;
        private NativeArray<int> _obstacleNeighborIndices;
        private NativeArray<float> _obstacleNeighborDistances;
        private NativeArray<int> _obstacleNeighborCounts;
        private NativeArray<float3> _queryPositions;
        private NativeArray<int> _queryMaxNeighbors;
        private NativeArray<float> _queryNeighborDistances;
        private NativeArray<JobObstacleData> _nativeObstacles;
        private NativeArray<JobLine> _orcaLines;
        private NativeArray<JobLine> _tempOrcaLines;
        private NativeArray<int> _orcaLineCounts;
        private NativeArray<int> _obstacleOrcaLineCounts;
        private NativeArray<JobAgentOutput> _outputs;
        private NativeArray<int> _kdPermutation;
        private NativeArray<JobKdNode> _kdNodes;
        private NativeArray<int> _kdNodeCount;
        private NativeArray<int> _kdBuildStackNode;
        private NativeArray<int> _kdBuildStackBegin;
        private NativeArray<int> _kdBuildStackEnd;
        private NativeArray<int> _kdTraversalStack;

        private ManagedAgentState _defaultAgent;
        private readonly List<int> _dirtyDynamicAgentIndices = new List<int>();
        private bool[] _dynamicAgentDirtyMarks = Array.Empty<bool>();
        private ObstacleVisibilityTreeNode _obstacleVisibilityTree;
        private List<ObstacleSegment> _obstacleVisibilitySegments;
        private bool _disposed;
        private bool _nativeStructureDirty = true;
        private bool _nativeObstacleDirty = true;
        private bool _obstacleTreeDirty = true;
        private int _nextAgentId;
        private float _globalTime;
        private float _timeStep = 0.1f;
        private JobHandle _integrateHandle;
        private bool _stepped;

        // private bool _obstaclesProcessed;

        public JobSimulator(int maxNeighborsCapacity = DefaultMaxNeighborsCapacity, int batchSize = DefaultBatchSize)
            : this(maxNeighborsCapacity, DefaultMaxObstacleNeighborsCapacity, batchSize)
        {
        }

        public JobSimulator(int maxNeighborsCapacity, int maxObstacleNeighborsCapacity, int batchSize)
        {
            MaxNeighborsCapacity = math.max(1, maxNeighborsCapacity);
            MaxObstacleNeighborsCapacity = math.max(1, maxObstacleNeighborsCapacity);
            BatchSize = math.max(1, batchSize);
        }

        public int MaxNeighborsCapacity { get; }

        public int MaxObstacleNeighborsCapacity { get; }

        public int BatchSize { get; }

        /// <summary>
        /// Performs a simulation step and updates the two-dimensional position and two-dimensional velocity of each agent.
        /// </summary>
        /// <param name="timeStep"></param>
        /// <returns>The global time after the simulation step.</returns>
        public float DoStep(float timeStep)
        {
            SetTimeStep(timeStep);
            return DoStepInternal(true);
        }

        /// <summary>
        /// Performs a simulation step and updates the two-dimensional position and two-dimensional velocity of each agent.
        /// </summary>
        /// <returns>The global time after the simulation step.</returns>
        public float DoStep()
        {
            return DoStepInternal(true);
        }
        
        public float DoStepAsync(float timeStep)
        {
            SetTimeStep(timeStep);
            return DoStepInternal(false);
        }

        public float DoStepAsync()
        {
            return DoStepInternal(false);
        }

        internal float DoStepInternal(bool completeRightNow)
        {
            if (_stepped)
            {
                if (completeRightNow)
                {
                    _integrateHandle.Complete();
                    _integrateHandle = default;
                    _stepped = false;
                    CopyNativeBackToManaged();
                    _globalTime += _timeStep;
                }
                return _globalTime;
            }
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

            var obstacleNeighborHandle = new BuildObstacleNeighborsJob
            {
                Agents = _nativeAgents,
                Obstacles = _nativeObstacles,
                ObstacleNeighborIndices = _obstacleNeighborIndices,
                ObstacleNeighborDistances = _obstacleNeighborDistances,
                ObstacleNeighborCounts = _obstacleNeighborCounts,
                MaxObstacleNeighborCapacity = MaxObstacleNeighborsCapacity,
            }.Schedule(_nativeAgents.Length, BatchSize, neighborHandle);

            var velocityHandle = new ComputeAgentVelocityJob
            {
                Agents = _nativeAgents,
                Obstacles = _nativeObstacles,
                NeighborIndices = _neighborIndices,
                NeighborCounts = _neighborCounts,
                ObstacleNeighborIndices = _obstacleNeighborIndices,
                ObstacleNeighborCounts = _obstacleNeighborCounts,
                OrcaLines = _orcaLines,
                TempOrcaLines = _tempOrcaLines,
                OrcaLineCounts = _orcaLineCounts,
                ObstacleOrcaLineCounts = _obstacleOrcaLineCounts,
                Outputs = _outputs,
                MaxNeighborCapacity = MaxNeighborsCapacity,
                MaxObstacleNeighborCapacity = MaxObstacleNeighborsCapacity,
                MaxOrcaLineCapacity = MaxNeighborsCapacity + MaxObstacleNeighborsCapacity,
                TimeStep = _timeStep,
            }.Schedule(_nativeAgents.Length, BatchSize, obstacleNeighborHandle);

            var integrateHandle = new IntegrateAgentJob
            {
                Agents = _nativeAgents,
                Outputs = _outputs,
                TimeStep = _timeStep,
            }.Schedule(_nativeAgents.Length, BatchSize, velocityHandle);
            if (completeRightNow)
            {
                integrateHandle.Complete();
                CopyNativeBackToManaged();
                _globalTime += _timeStep;
                return _globalTime;
            }
            _integrateHandle = integrateHandle;
            _stepped = true;
            return _globalTime;
        }
        
        public bool IsJobRunning()
        {
            return _stepped;
        }

        /// <summary>
        /// wait until job is completed, return false while job is running.
        /// </summary>
        public bool CheckJobCompletion()
        {
            if (!_stepped) return true;
            if (!_integrateHandle.IsCompleted) return false;
            _integrateHandle.Complete();
            _integrateHandle = default;
            _stepped = false;
            _globalTime += _timeStep;
            CopyNativeBackToManaged();
            return true;
        }

        /**
         * <summary>Clears the simulation.</summary>
         */
        public void Clear()
        {
            ThrowIfDisposed();
            _agents.Clear();
            _agentNo2Index.Clear();
            _index2AgentNo.Clear();
            _obstacles.Clear();
            _defaultAgent = null;
            _nextAgentId = 0;
            _globalTime = 0.0f;
            _timeStep = 0.1f;
            // _obstaclesProcessed = false;
            _nativeStructureDirty = true;
            _nativeObstacleDirty = true;
            _obstacleTreeDirty = true;
            ClearDynamicAgentDirty();
            _obstacleVisibilityTree = null;
            _obstacleVisibilitySegments = null;
            ReleaseNativeState();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_stepped)
            {
                _integrateHandle.Complete();
                _integrateHandle = default;
                _stepped = false;
            }

            ReleaseNativeState();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        /**
         * <summary>Adds a new agent with default properties to the simulation.
         * </summary>
         *
         * <returns>The number of the agent, or -1 when the agent defaults have
         * not been set.</returns>
         *
         * <param name="position">The two-dimensional starting position of this
         * agent.</param>
         */
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

        /**
         * <summary>Adds a new agent to the simulation.</summary>
         *
         * <returns>The number of the agent.</returns>
         *
         * <param name="position">The two-dimensional starting position of this
         * agent.</param>
         * <param name="neighborDist">The maximum distance (center point to
         * center point) to other agents this agent takes into account in the
         * navigation. The larger this number, the longer the running time of
         * the simulation. If the number is too low, the simulation will not be
         * safe. Must be non-negative.</param>
         * <param name="maxNeighbors">The maximum number of other agents this
         * agent takes into account in the navigation. The larger this number,
         * the longer the running time of the simulation. If the number is too
         * low, the simulation will not be safe.</param>
         * <param name="timeHorizon">The minimal amount of time for which this
         * agent's velocities that are computed by the simulation are safe with
         * respect to other agents. The larger this number, the sooner this
         * agent will respond to the presence of other agents, but the less
         * freedom this agent has in choosing its velocities. Must be positive.
         * </param>
         * <param name="timeHorizonObst">The minimal amount of time for which
         * this agent's velocities that are computed by the simulation are safe
         * with respect to obstacles. The larger this number, the sooner this
         * agent will respond to the presence of obstacles, but the less freedom
         * this agent has in choosing its velocities. Must be positive.</param>
         * <param name="radius">The radius of this agent. Must be non-negative.
         * </param>
         * <param name="maxSpeed">The maximum speed of this agent. Must be
         * non-negative.</param>
         * <param name="velocity">The initial two-dimensional linear velocity of
         * this agent.</param>
         */
        public int AddAgent(Vector3 position, float neighborDist, int maxNeighbors, float timeHorizon, float timeHorizonObst, float radius, float maxSpeed, Vector3 velocity)
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
            _nativeStructureDirty = true;
            return agent.id;
        }

        public void DelAgent(int agentNo)
        {
            ThrowIfDisposed();

            if (_agentNo2Index.TryGetValue(agentNo, out var index))
            {
                _agents[index].needDelete = true;
            }
        }

        /**
         * <summary>Adds a new obstacle to the simulation.</summary>
         *
         * <returns>The number of the first vertex of the obstacle, or -1 when
         * the number of vertices is less than two.</returns>
         *
         * <param name="vertices">List of the vertices of the polygonal obstacle
         * in counterclockwise order.</param>
         *
         * <remarks>To add a "negative" obstacle, e.g. a bounding polygon around
         * the environment, the vertices should be listed in clockwise order.
         * </remarks>
         */
        public int AddObstacle(IList<Vector3> vertices)
        {
            ThrowIfDisposed();

            if (vertices == null || vertices.Count < 2)
            {
                return -1;
            }

            var obstacleNo = _obstacles.Count;

            for (var i = 0; i < vertices.Count; ++i)
            {
                var next = i == vertices.Count - 1 ? 0 : i + 1;
                var previous = i == 0 ? vertices.Count - 1 : i - 1;
                var direction = MathUtil.NormalizeSafe(ToFloat3(vertices[next] - vertices[i]));

                var obstacle = new ManagedObstacleState
                {
                    id = _obstacles.Count,
                    point = vertices[i],
                    direction = direction,
                    previous = obstacleNo + previous,
                    next = obstacleNo + next,
                    convex = vertices.Count == 2 || MathUtil.LeftOf(ToFloat3(vertices[previous]), ToFloat3(vertices[i]), ToFloat3(vertices[next])) >= 0.0f,
                };

                _obstacles.Add(obstacle);
            }

            // _obstaclesProcessed = false;
            _nativeObstacleDirty = true;
            _obstacleTreeDirty = true;
            _obstacleVisibilityTree = null;
            return obstacleNo;
        }

        /**
         * <summary>Processes the obstacles that have been added so that they
         * are accounted for in the simulation.</summary>
         *
         * <remarks>Obstacles added to the simulation after this function has
         * been called are not accounted for in the simulation.</remarks>
         */
        public void ProcessObstacles()
        {
            ThrowIfDisposed();
            EnsureObstacleVisibilityTree();
            // _obstaclesProcessed = true;
        }

        /**
         * <summary>Performs a visibility query between the two specified points
         * with respect to the obstacles.</summary>
         *
         * <returns>A boolean specifying whether the two points are mutually
         * visible. Returns true when the obstacles have not been processed.
         * </returns>
         *
         * <param name="point1">The first point of the query.</param>
         * <param name="point2">The second point of the query.</param>
         * <param name="radius">The minimal distance between the line connecting
         * the two points and the obstacles in order for the points to be
         * mutually visible (optional). Must be non-negative.</param>
         */
        public bool QueryVisibility(Vector3 point1, Vector3 point2, float radius)
        {
            ThrowIfDisposed();
            if (_obstacles == null || _obstacles.Count == 0)
            {
                return true;
            }

            // if (!_obstaclesProcessed)
            // {
            //     return true;
            // }

            var q1 = ToFloat3(point1);
            var q2 = ToFloat3(point2);
            var radiusSq = radius * radius;
            EnsureObstacleVisibilityTree();

            return QueryVisibilityTree(q1, q2, radiusSq, _obstacleVisibilityTree);
        }

        public int QueryNearAgent(Vector3 point, float radius)
        {
            ThrowIfDisposed();

            var bestDistSq = radius * radius;
            var bestAgent = -1;
            float3 pointF3 = ToFloat3(point);
            for (var index = 0; index < _agents.Count; index++)
            {
                var distSq = math.distancesq(pointF3, _agents[index].position);
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestAgent = _agents[index].id;
                }
            }

            return bestAgent;
        }

        /**
         * <summary>Returns the specified agent neighbor of the specified agent.
         * </summary>
         *
         * <returns>The number of the neighboring agent.</returns>
         *
         * <param name="agentNo">The number of the agent whose agent neighbor is
         * to be retrieved.</param>
         * <param name="neighborNo">The number of the agent neighbor to be
         * retrieved.</param>
         */
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
            return _index2AgentNo[neighborIndex];
        }

        /**
         * <summary>Returns the maximum neighbor count of a specified agent.
         * </summary>
         *
         * <returns>The present maximum neighbor count of the agent.</returns>
         *
         * <param name="agentNo">The number of the agent whose maximum neighbor
         * count is to be retrieved.</param>
         */
        public int GetAgentMaxNeighbors(int agentNo)
        {
            return _agents[GetAgentIndex(agentNo)].maxNeighbors;
        }

        /**
         * <summary>Returns the maximum speed of a specified agent.</summary>
         *
         * <returns>The present maximum speed of the agent.</returns>
         *
         * <param name="agentNo">The number of the agent whose maximum speed is
         * to be retrieved.</param>
         */
        public float GetAgentMaxSpeed(int agentNo)
        {
            return _agents[GetAgentIndex(agentNo)].maxSpeed;
        }

        /**
         * <summary>Returns the maximum neighbor distance of a specified agent.
         * </summary>
         *
         * <returns>The present maximum neighbor distance of the agent.
         * </returns>
         *
         * <param name="agentNo">The number of the agent whose maximum neighbor
         * distance is to be retrieved.</param>
         */
        public float GetAgentNeighborDist(int agentNo)
        {
            return _agents[GetAgentIndex(agentNo)].neighborDist;
        }

        /**
         * <summary>Returns the count of agent neighbors taken into account to
         * compute the current velocity for the specified agent.</summary>
         *
         * <returns>The count of agent neighbors taken into account to compute
         * the current velocity for the specified agent.</returns>
         *
         * <param name="agentNo">The number of the agent whose count of agent
         * neighbors is to be retrieved.</param>
         */
        public int GetAgentNumAgentNeighbors(int agentNo)
        {
            var index = GetAgentIndex(agentNo);
            if (!_neighborCounts.IsCreated || index >= _neighborCounts.Length)
            {
                return 0;
            }

            return _neighborCounts[index];
        }

        /**
         * <summary>Returns the count of obstacle neighbors taken into account
         * to compute the current velocity for the specified agent.</summary>
         *
         * <returns>The count of obstacle neighbors taken into account to
         * compute the current velocity for the specified agent.</returns>
         *
         * <param name="agentNo">The number of the agent whose count of obstacle
         * neighbors is to be retrieved.</param>
         */
        public int GetAgentNumObstacleNeighbors(int agentNo)
        {
            var index = GetAgentIndex(agentNo);
            if (!_obstacleNeighborCounts.IsCreated || index >= _obstacleNeighborCounts.Length)
            {
                return 0;
            }

            return _obstacleNeighborCounts[index];
        }

        /**
         * <summary>Returns the specified obstacle neighbor of the specified
         * agent.</summary>
         *
         * <returns>The number of the first vertex of the neighboring obstacle
         * edge.</returns>
         *
         * <param name="agentNo">The number of the agent whose obstacle neighbor
         * is to be retrieved.</param>
         * <param name="neighborNo">The number of the obstacle neighbor to be
         * retrieved.</param>
         */
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

        /**
         * <summary>Returns the two-dimensional position of a specified agent.
         * </summary>
         *
         * <returns>The present two-dimensional position of the (center of the)
         * agent.</returns>
         *
         * <param name="agentNo">The number of the agent whose two-dimensional
         * position is to be retrieved.</param>
         */
        public Vector3 GetAgentPosition(int agentNo)
        {
            return _agents[GetAgentIndex(agentNo)].position;
        }

        /**
         * <summary>Returns the two-dimensional preferred velocity of a
         * specified agent.</summary>
         *
         * <returns>The present two-dimensional preferred velocity of the agent.
         * </returns>
         *
         * <param name="agentNo">The number of the agent whose two-dimensional
         * preferred velocity is to be retrieved.</param>
         */
        public Vector3 GetAgentPrefVelocity(int agentNo)
        {
            return _agents[GetAgentIndex(agentNo)].prefVelocity;
        }

        /**
         * <summary>Returns the radius of a specified agent.</summary>
         *
         * <returns>The present radius of the agent.</returns>
         *
         * <param name="agentNo">The number of the agent whose radius is to be
         * retrieved.</param>
         */
        public float GetAgentRadius(int agentNo)
        {
            return _agents[GetAgentIndex(agentNo)].radius;
        }

        /**
         * <summary>Returns the time horizon of a specified agent.</summary>
         *
         * <returns>The present time horizon of the agent.</returns>
         *
         * <param name="agentNo">The number of the agent whose time horizon is
         * to be retrieved.</param>
         */
        public float GetAgentTimeHorizon(int agentNo)
        {
            return _agents[GetAgentIndex(agentNo)].timeHorizon;
        }

        /**
         * <summary>Returns the time horizon with respect to obstacles of a
         * specified agent.</summary>
         *
         * <returns>The present time horizon with respect to obstacles of the
         * agent.</returns>
         *
         * <param name="agentNo">The number of the agent whose time horizon with
         * respect to obstacles is to be retrieved.</param>
         */
        public float GetAgentTimeHorizonObst(int agentNo)
        {
            return _agents[GetAgentIndex(agentNo)].timeHorizonObst;
        }

        /**
         * <summary>Returns the two-dimensional linear velocity of a specified
         * agent.</summary>
         *
         * <returns>The present two-dimensional linear velocity of the agent.
         * </returns>
         *
         * <param name="agentNo">The number of the agent whose two-dimensional
         * linear velocity is to be retrieved.</param>
         */
        public Vector3 GetAgentVelocity(int agentNo)
        {
            return _agents[GetAgentIndex(agentNo)].velocity;
        }

        /**
         * <summary>Returns the global time of the simulation.</summary>
         *
         * <returns>The present global time of the simulation (zero initially).
         * </returns>
         */
        public float GetGlobalTime()
        {
            return _globalTime;
        }

        /**
         * <summary>Returns the count of agents in the simulation.</summary>
         *
         * <returns>The count of agents in the simulation.</returns>
         */
        public int GetNumAgents()
        {
            return _agents.Count;
        }

        /**
         * <summary>Returns the count of obstacle vertices in the simulation.
         * </summary>
         *
         * <returns>The count of obstacle vertices in the simulation.</returns>
         */
        public int GetNumObstacleVertices()
        {
            return _obstacles.Count;
        }

        /**
         * <summary>Returns the two-dimensional position of a specified obstacle
         * vertex.</summary>
         *
         * <returns>The two-dimensional position of the specified obstacle
         * vertex.</returns>
         *
         * <param name="vertexNo">The number of the obstacle vertex to be
         * retrieved.</param>
         */
        public Vector3 GetObstacleVertex(int vertexNo)
        {
            ThrowIfDisposed();

            if (vertexNo < 0 || vertexNo >= _obstacles.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(vertexNo));
            }

            return _obstacles[vertexNo].point;
        }

        /**
         * <summary>Returns the number of the obstacle vertex succeeding the
         * specified obstacle vertex in its polygon.</summary>
         *
         * <returns>The number of the obstacle vertex succeeding the specified
         * obstacle vertex in its polygon.</returns>
         *
         * <param name="vertexNo">The number of the obstacle vertex whose
         * successor is to be retrieved.</param>
         */
        public int GetNextObstacleVertexNo(int vertexNo)
        {
            ThrowIfDisposed();

            if (vertexNo < 0 || vertexNo >= _obstacles.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(vertexNo));
            }

            return _obstacles[vertexNo].next;
        }

        /**
         * <summary>Returns the number of the obstacle vertex preceding the
         * specified obstacle vertex in its polygon.</summary>
         *
         * <returns>The number of the obstacle vertex preceding the specified
         * obstacle vertex in its polygon.</returns>
         *
         * <param name="vertexNo">The number of the obstacle vertex whose
         * predecessor is to be retrieved.</param>
         */
        public int GetPrevObstacleVertexNo(int vertexNo)
        {
            ThrowIfDisposed();

            if (vertexNo < 0 || vertexNo >= _obstacles.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(vertexNo));
            }

            return _obstacles[vertexNo].previous;
        }

        /**
         * <summary>Returns the time step of the simulation.</summary>
         *
         * <returns>The present time step of the simulation.</returns>
         */
        public float GetTimeStep()
        {
            return _timeStep;
        }

        /**
         * <summary>Sets the default properties for any new agent that is added.
         * </summary>
         *
         * <param name="neighborDist">The default maximum distance (center point
         * to center point) to other agents a new agent takes into account in
         * the navigation. The larger this number, the longer he running time of
         * the simulation. If the number is too low, the simulation will not be
         * safe. Must be non-negative.</param>
         * <param name="maxNeighbors">The default maximum number of other agents
         * a new agent takes into account in the navigation. The larger this
         * number, the longer the running time of the simulation. If the number
         * is too low, the simulation will not be safe.</param>
         * <param name="timeHorizon">The default minimal amount of time for
         * which a new agent's velocities that are computed by the simulation
         * are safe with respect to other agents. The larger this number, the
         * sooner an agent will respond to the presence of other agents, but the
         * less freedom the agent has in choosing its velocities. Must be
         * positive.</param>
         * <param name="timeHorizonObst">The default minimal amount of time for
         * which a new agent's velocities that are computed by the simulation
         * are safe with respect to obstacles. The larger this number, the
         * sooner an agent will respond to the presence of obstacles, but the
         * less freedom the agent has in choosing its velocities. Must be
         * positive.</param>
         * <param name="radius">The default radius of a new agent. Must be
         * non-negative.</param>
         * <param name="maxSpeed">The default maximum speed of a new agent. Must
         * be non-negative.</param>
         * <param name="velocity">The default initial two-dimensional linear
         * velocity of a new agent.</param>
         */
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

        /**
         * <summary>Sets the maximum neighbor count of a specified agent.
         * </summary>
         *
         * <param name="agentNo">The number of the agent whose maximum neighbor
         * count is to be modified.</param>
         * <param name="maxNeighbors">The replacement maximum neighbor count.
         * </param>
         */
        public void SetAgentMaxNeighbors(int agentNo, int maxNeighbors)
        {
            var agent = _agents[GetAgentIndex(agentNo)];
            agent.maxNeighbors = math.max(0, maxNeighbors);
            _nativeStructureDirty = true;
        }

        /**
         * <summary>Sets the maximum speed of a specified agent.</summary>
         *
         * <param name="agentNo">The number of the agent whose maximum speed is
         * to be modified.</param>
         * <param name="maxSpeed">The replacement maximum speed. Must be
         * non-negative.</param>
         */
        public void SetAgentMaxSpeed(int agentNo, float maxSpeed)
        {
            var agent = _agents[GetAgentIndex(agentNo)];
            agent.maxSpeed = math.max(0.0f, maxSpeed);
            _nativeStructureDirty = true;
        }

        /**
         * <summary>Sets the maximum neighbor distance of a specified agent.
         * </summary>
         *
         * <param name="agentNo">The number of the agent whose maximum neighbor
         * distance is to be modified.</param>
         * <param name="neighborDist">The replacement maximum neighbor distance.
         * Must be non-negative.</param>
         */
        public void SetAgentNeighborDist(int agentNo, float neighborDist)
        {
            var agent = _agents[GetAgentIndex(agentNo)];
            agent.neighborDist = neighborDist;
            _nativeStructureDirty = true;
        }

        /**
         * <summary>Sets the two-dimensional position of a specified agent.
         * </summary>
         *
         * <param name="agentNo">The number of the agent whose two-dimensional
         * position is to be modified.</param>
         * <param name="position">The replacement of the two-dimensional
         * position.</param>
         */
        public void SetAgentPosition(int agentNo, Vector3 position)
        {
            var index = GetAgentIndex(agentNo);
            var agent = _agents[index];
            agent.position = position;
            MarkDynamicAgentDirty(index);
        }

        /**
         * <summary>Sets the two-dimensional preferred velocity of a specified
         * agent.</summary>
         *
         * <param name="agentNo">The number of the agent whose two-dimensional
         * preferred velocity is to be modified.</param>
         * <param name="prefVelocity">The replacement of the two-dimensional
         * preferred velocity.</param>
         */
        public void SetAgentPrefVelocity(int agentNo, Vector3 prefVelocity)
        {
            var index = GetAgentIndex(agentNo);
            var agent = _agents[index];
            agent.prefVelocity = prefVelocity;
            MarkDynamicAgentDirty(index);
        }

        /**
         * <summary>Sets the radius of a specified agent.</summary>
         *
         * <param name="agentNo">The number of the agent whose radius is to be
         * modified.</param>
         * <param name="radius">The replacement radius. Must be non-negative.
         * </param>
         */
        public void SetAgentRadius(int agentNo, float radius)
        {
            var agent = _agents[GetAgentIndex(agentNo)];
            agent.radius = math.max(0.0f, radius);
            _nativeStructureDirty = true;
        }

        /**
         * <summary>Sets the time horizon of a specified agent with respect to
         * other agents.</summary>
         *
         * <param name="agentNo">The number of the agent whose time horizon is
         * to be modified.</param>
         * <param name="timeHorizon">The replacement time horizon with respect
         * to other agents. Must be positive.</param>
         */
        public void SetAgentTimeHorizon(int agentNo, float timeHorizon)
        {
            var agent = _agents[GetAgentIndex(agentNo)];
            agent.timeHorizon = math.max(RvoEpsilon, timeHorizon);
            _nativeStructureDirty = true;
        }

        /**
         * <summary>Sets the time horizon of a specified agent with respect to
         * obstacles.</summary>
         *
         * <param name="agentNo">The number of the agent whose time horizon with
         * respect to obstacles is to be modified.</param>
         * <param name="timeHorizonObst">The replacement time horizon with
         * respect to obstacles. Must be positive.</param>
         */
        public void SetAgentTimeHorizonObst(int agentNo, float timeHorizonObst)
        {
            var agent = _agents[GetAgentIndex(agentNo)];
            agent.timeHorizonObst = math.max(RvoEpsilon, timeHorizonObst);
            _nativeStructureDirty = true;
        }

        /**
         * <summary>Sets the two-dimensional linear velocity of a specified
         * agent.</summary>
         *
         * <param name="agentNo">The number of the agent whose two-dimensional
         * linear velocity is to be modified.</param>
         * <param name="velocity">The replacement two-dimensional linear
         * velocity.</param>
         */
        public void SetAgentVelocity(int agentNo, Vector3 velocity)
        {
            var index = GetAgentIndex(agentNo);
            var agent = _agents[index];
            agent.velocity = velocity;
            MarkDynamicAgentDirty(index);
        }

        /**
         * <summary>Sets the global time of the simulation.</summary>
         *
         * <param name="globalTime_">The global time of the simulation.</param>
         */
        public void SetGlobalTime(float globalTime)
        {
            _globalTime = globalTime;
        }

        /**
         * <summary>Sets the time step of the simulation.</summary>
         *
         * <param name="timeStep">The time step of the simulation. Must be
         * positive.</param>
         */
        public void SetTimeStep(float timeStep)
        {
            _timeStep = math.max(RvoEpsilon, timeStep);
        }

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
                    _kdPermutation = new NativeArray<int>(_agents.Count, Allocator.Persistent);
                    _kdNodes = new NativeArray<JobKdNode>(_agents.Count * 2, Allocator.Persistent);
                    _kdNodeCount = new NativeArray<int>(1, Allocator.Persistent);
                    _kdBuildStackNode = new NativeArray<int>(_agents.Count * 2, Allocator.Persistent);
                    _kdBuildStackBegin = new NativeArray<int>(_agents.Count * 2, Allocator.Persistent);
                    _kdBuildStackEnd = new NativeArray<int>(_agents.Count * 2, Allocator.Persistent);
                    _kdTraversalStack = new NativeArray<int>(_agents.Count * DefaultMaxKdTraversalDepth, Allocator.Persistent);
                }

                EnsureDynamicDirtyCapacity();

                for (var index = 0; index < _agents.Count; index++)
                {
                    var agent = _agents[index];
                    _nativeAgents[index] = new JobAgentData
                    {
                        id = agent.id,
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

                    var agent = _agents[index];
                    var native = _nativeAgents[index];
                    native.position = agent.position;
                    native.prefVelocity = agent.prefVelocity;
                    native.velocity = agent.velocity;
                    _nativeAgents[index] = native;
                }

                ClearDynamicAgentDirty();
            }

            if (_nativeObstacleDirty && _nativeObstacles.IsCreated)
            {
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

                _nativeObstacleDirty = false;
            }
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
            if (_dynamicAgentDirtyMarks.Length == _agents.Count)
            {
                return;
            }

            _dynamicAgentDirtyMarks = new bool[_agents.Count];
            _dirtyDynamicAgentIndices.Clear();
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

        private sealed class ObstacleSegment
        {
            public float3 start;
            public float3 end;
            public float3 boundsMin;
            public float3 boundsMax;
            public float3 center;
        }

        private void EnsureObstacleVisibilityTree()
        {
            if (!_obstacleTreeDirty)
            {
                return;
            }

            if (_obstacles.Count == 0)
            {
                _obstacleVisibilityTree = null;
                _obstacleVisibilitySegments = null;
                _obstacleTreeDirty = false;
                return;
            }

            if (_obstacleVisibilitySegments == null)
            {
                _obstacleVisibilitySegments = new List<ObstacleSegment>(_obstacles.Count);
            }
            else
            {
                _obstacleVisibilitySegments.Clear();
            }

            for (var index = 0; index < _obstacles.Count; index++)
            {
                var obstacle = _obstacles[index];
                if ((uint)obstacle.next >= (uint)_obstacles.Count)
                {
                    continue;
                }

                var start = ToFloat3(obstacle.point);
                var end = ToFloat3(_obstacles[obstacle.next].point);
                _obstacleVisibilitySegments.Add(new ObstacleSegment
                {
                    start = start,
                    end = end,
                    boundsMin = math.min(start, end),
                    boundsMax = math.max(start, end),
                    center = (start + end) * 0.5f,
                });
            }

            if (_obstacleVisibilitySegments.Count == 0)
            {
                _obstacleVisibilityTree = null;
                _obstacleTreeDirty = false;
                return;
            }

            var indices = new List<int>(_obstacleVisibilitySegments.Count);
            for (var index = 0; index < _obstacleVisibilitySegments.Count; index++)
            {
                indices.Add(index);
            }

            _obstacleVisibilityTree = BuildObstacleVisibilityTree(indices);
            _obstacleTreeDirty = false;
        }

        private ObstacleVisibilityTreeNode BuildObstacleVisibilityTree(List<int> indices)
        {
            if (indices == null || indices.Count == 0)
            {
                return null;
            }

            var node = new ObstacleVisibilityTreeNode
            {
                boundsMin = new float3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity),
                boundsMax = new float3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity),
            };

            for (var i = 0; i < indices.Count; i++)
            {
                var segment = _obstacleVisibilitySegments[indices[i]];
                node.boundsMin = math.min(node.boundsMin, segment.boundsMin);
                node.boundsMax = math.max(node.boundsMax, segment.boundsMax);
            }

            if (indices.Count <= 8)
            {
                node.segmentIndices = indices.ToArray();
                return node;
            }

            var extents = node.boundsMax - node.boundsMin;
            var axis = extents.x >= extents.y
                ? (extents.x >= extents.z ? 0 : 2)
                : (extents.y >= extents.z ? 1 : 2);

            indices.Sort((lhs, rhs) =>
            {
                var lhsCenter = _obstacleVisibilitySegments[lhs].center;
                var rhsCenter = _obstacleVisibilitySegments[rhs].center;
                switch (axis)
                {
                    case 0:
                        return lhsCenter.x.CompareTo(rhsCenter.x);
                    case 1:
                        return lhsCenter.y.CompareTo(rhsCenter.y);
                    default:
                        return lhsCenter.z.CompareTo(rhsCenter.z);
                }
            });

            var split = indices.Count / 2;
            if (split <= 0 || split >= indices.Count)
            {
                node.segmentIndices = indices.ToArray();
                return node;
            }

            var leftIndices = indices.GetRange(0, split);
            var rightIndices = indices.GetRange(split, indices.Count - split);

            node.left = BuildObstacleVisibilityTree(leftIndices);
            node.right = BuildObstacleVisibilityTree(rightIndices);
            return node;
        }

        private bool QueryVisibilityTree(float3 q1, float3 q2, float radiusSq, ObstacleVisibilityTreeNode node)
        {
            if (node == null)
            {
                return true;
            }

            if (!CapsuleBoundsOverlap(q1, q2, radiusSq, node.boundsMin, node.boundsMax))
            {
                return true;
            }

            if (node.segmentIndices != null)
            {
                for (var i = 0; i < node.segmentIndices.Length; i++)
                {
                    var segment = _obstacleVisibilitySegments[node.segmentIndices[i]];
                    if (MathUtil.DistSqSegmentSegment(q1, q2, segment.start, segment.end) <= radiusSq)
                    {
                        return false;
                    }
                }

                return true;
            }

            if (!QueryVisibilityTree(q1, q2, radiusSq, node.left))
            {
                return false;
            }

            return QueryVisibilityTree(q1, q2, radiusSq, node.right);
        }

        private static bool CapsuleBoundsOverlap(float3 q1, float3 q2, float radiusSq, float3 boundsMin, float3 boundsMax)
        {
            var radius = math.sqrt(math.max(radiusSq, 0.0f));
            var radius3 = new float3(radius, radius, radius);
            var capsuleMin = math.min(q1, q2) - radius3;
            var capsuleMax = math.max(q1, q2) + radius3;

            return capsuleMin.x <= boundsMax.x &&
                   capsuleMax.x >= boundsMin.x &&
                   capsuleMin.y <= boundsMax.y &&
                   capsuleMax.y >= boundsMin.y &&
                   capsuleMin.z <= boundsMax.z &&
                   capsuleMax.z >= boundsMin.z;
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

            if (_obstacleNeighborIndices.IsCreated)
            {
                _obstacleNeighborIndices.Dispose();
            }

            if (_obstacleNeighborDistances.IsCreated)
            {
                _obstacleNeighborDistances.Dispose();
            }

            if (_obstacleNeighborCounts.IsCreated)
            {
                _obstacleNeighborCounts.Dispose();
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

            if (_nativeObstacles.IsCreated)
            {
                _nativeObstacles.Dispose();
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

            if (_obstacleOrcaLineCounts.IsCreated)
            {
                _obstacleOrcaLineCounts.Dispose();
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
                _nativeStructureDirty = true;
                ClearDynamicAgentDirty();
            }
        }

        private static float3 ToFloat3(Vector3 value)
        {
            return new float3(value.x, value.y, value.z);
        }
    }
}