using System;
using KNN.Jobs;
using Unity.Jobs;
using Unity.Mathematics;

namespace RVO.JobSystem
{
    public sealed partial class JobSimulator
    {
        public float DoStep(float timeStep)
        {
            SetTimeStep(timeStep);
            return DoStepInternal(true);
        }

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

            var knnBuildHandle = new KnnRebuildJob(_agentKnnContainer).Schedule(extractQueryDataHandle);

            var knnQueryHandle = new QueryKNearestBatchJob(_agentKnnContainer, _queryPositions, _knnCandidateIndices)
                .ScheduleBatch(_nativeAgents.Length, math.max(1, _nativeAgents.Length / BatchSize), knnBuildHandle);

            var neighborHandle = new BuildAgentNeighborsKnnJob
            {
                Positions = _queryPositions,
                AgentMaxNeighbors = _queryMaxNeighbors,
                AgentNeighborDistances = _queryNeighborDistances,
                CandidateIndices = _knnCandidateIndices,
                NeighborIndices = _neighborIndices,
                NeighborDistances = _neighborDistances,
                NeighborCounts = _neighborCounts,
                CandidateCapacity = _knnCandidateCapacity,
                MaxNeighborCapacity = MaxNeighborsCapacity,
            }.Schedule(_nativeAgents.Length, BatchSize, knnQueryHandle);

            var obstacleNeighborHandle = _nativeObstacles.IsCreated && _nativeObstacles.Length > 0
                ? new BuildObstacleNeighborsJob
                {
                    Agents = _nativeAgents,
                    Obstacles = _nativeObstacles,
                    ObstacleTreeNodes = _nativeObstacleTreeNodes,
                    ObstacleNeighborIndices = _obstacleNeighborIndices,
                    ObstacleNeighborDistances = _obstacleNeighborDistances,
                    ObstacleNeighborCounts = _obstacleNeighborCounts,
                    MaxObstacleNeighborCapacity = MaxObstacleNeighborsCapacity,
                }.Schedule(_nativeAgents.Length, BatchSize, neighborHandle)
                : new ClearObstacleNeighborsJob
                {
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
                ExtraRadii = ExtraRadii,
                AgentTypeCount = _agentTypeCount,
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

        public void Clear()
        {
            ThrowIfDisposed();
            CompleteAllJobs();
            _agents.Clear();
            _obstacles.Clear();
            _defaultAgent = null;
            _nextAgentId = 0;
            _globalTime = 0.0f;
            _timeStep = 0.1f;
            _nativeStructureDirty = true;
            _nativeObstacleDirty = true;
            _obstacleTreeDirty = true;
            ClearDynamicAgentDirty();
            _obstacleVisibilityTree = null;
            _obstacleVisibilitySegments = null;
            ReleaseNativeState();
        }

        public void CompleteAllJobs()
        {
            if (!_stepped) return;
            _integrateHandle.Complete();
            _integrateHandle = default;
            _stepped = false;
            CopyNativeBackToManaged();
            _globalTime += _timeStep;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            CompleteAllJobs();

            ReleaseNativeState();
            if (ExtraRadii.IsCreated)
            {
                ExtraRadii.Dispose();
            }
            _disposed = true;
            _managedAgentPool.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
