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
using KNN;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Pool;

namespace RVO.JobSystem
{
    /// <summary>
    /// A Burst/JobSystem RVO simulator that keeps the legacy RVO implementation intact.
    /// </summary>
    public sealed partial class JobSimulator : IDisposable
    {
        private const int DefaultBatchSize = 32;
        private const int DefaultMaxNeighborsCapacity = 16;
        private const int DefaultMaxObstacleNeighborsCapacity = 16;
        private const float RvoEpsilon = 0.00001f;

        private readonly SparseSet _agents = new SparseSet(128);
        private readonly List<ManagedObstacleState> _obstacles = new List<ManagedObstacleState>();

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
        private NativeArray<int> _knnCandidateIndices;
        private NativeArray<JobObstacleData> _nativeObstacles;
        private NativeArray<JobObstacleTreeNode> _nativeObstacleTreeNodes;
        private NativeArray<JobLine> _orcaLines;
        private NativeArray<JobLine> _tempOrcaLines;
        private NativeArray<int> _orcaLineCounts;
        private NativeArray<int> _obstacleOrcaLineCounts;
        private NativeArray<JobAgentOutput> _outputs;
        private KnnContainer _agentKnnContainer;
        private int _knnCandidateCapacity;

        // 一个通过 agentType - agentType 查找额外半径的矩阵，长度为 agentTypeCount * agentTypeCount
        // 索引方式 agentTypeA * agentTypeCount + agentTypeB
        public NativeArray<float> ExtraRadii;

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
        private ObjectPool<ManagedAgentState> _managedAgentPool;

        public JobSimulator(int maxNeighborsCapacity = DefaultMaxNeighborsCapacity, int batchSize = DefaultBatchSize)
            : this(maxNeighborsCapacity, DefaultMaxObstacleNeighborsCapacity, batchSize)
        {
        }

        public JobSimulator(int maxNeighborsCapacity, int maxObstacleNeighborsCapacity, int batchSize)
        {
            MaxNeighborsCapacity = math.max(1, maxNeighborsCapacity);
            MaxObstacleNeighborsCapacity = math.max(1, maxObstacleNeighborsCapacity);
            BatchSize = math.max(1, batchSize);
            ExtraRadii = new NativeArray<float>(1, Allocator.Persistent);
            _managedAgentPool = new ObjectPool<ManagedAgentState>(() => new ManagedAgentState
            {
                id = _nextAgentId++
            }, null, null, null, false);
        }

        public int MaxNeighborsCapacity { get; }

        public int MaxObstacleNeighborsCapacity { get; }

        public int BatchSize { get; }

        private int _agentTypeCount = 1;

        public void ConfigAgentTypes(int agentTypeCount)
        {
            if (agentTypeCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(agentTypeCount));
            }

            CompleteAllJobs();

            if (ExtraRadii.IsCreated)
            {
                ExtraRadii.Dispose();
            }
            ExtraRadii = new NativeArray<float>(agentTypeCount * agentTypeCount, Allocator.Persistent);
            _agentTypeCount = agentTypeCount;
        }

        public void ConfigAgentExtraRadii(int agentTypeA, int agentTypeB, float extraRadius)
        {
            if (!ExtraRadii.IsCreated)
            {
                throw new InvalidOperationException("Call ConfigAgentTypes before configuring extra radii.");
            }

            if (agentTypeA < 0 || agentTypeA >= _agentTypeCount)
            {
                throw new ArgumentOutOfRangeException(nameof(agentTypeA));
            }
            if (agentTypeB < 0 || agentTypeB >= _agentTypeCount)
            {
                throw new ArgumentOutOfRangeException(nameof(agentTypeB));
            }
            if (extraRadius < 0.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(extraRadius));
            }
            CompleteAllJobs();
            ExtraRadii[agentTypeA * _agentTypeCount + agentTypeB] = extraRadius;
        }
    }
}
