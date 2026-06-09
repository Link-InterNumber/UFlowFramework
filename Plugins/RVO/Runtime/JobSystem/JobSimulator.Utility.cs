using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;

namespace RVO.JobSystem
{
    public sealed partial class JobSimulator
    {
        private int GetAgentIndex(int agentNo)
        {
            ThrowIfDisposed();

            var index = _agents.DenseIndexOf(agentNo);
            if (index >= 0)
            {
                return index;
            }

            throw new KeyNotFoundException($"Agent {agentNo} was not found.");
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
            var removeNoId = ListPool<ManagedAgentState>.Get();
            foreach (var managedAgentState in _agents)
            {
                if (managedAgentState.needDelete)
                {
                    removeNoId.Add(managedAgentState);
                }
            }

            for (var i = 0; i < removeNoId.Count; i++)
            {
                var state = removeNoId[i];
                _agents.Remove(state.id);
                _managedAgentPool.Release(state);
            }

            if (removeNoId.Count > 0)
            {
                _nativeStructureDirty = true;
                ClearDynamicAgentDirty();
            }
            ListPool<ManagedAgentState>.Release(removeNoId);
        }

        private static float3 ToFloat3(Vector3 value)
        {
            return new float3(value.x, value.y, value.z);
        }
    }
}
