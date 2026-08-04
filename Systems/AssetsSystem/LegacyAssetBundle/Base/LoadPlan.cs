using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace PowerCellStudio
{
    public class LoadPlan
    {
        private Dictionary<string, HashSet<(string, Type)>> _loadPlan;

        public LoadPlan()
        {
            _loadPlan = new Dictionary<string, HashSet<(string, Type)>>();
        }
        
        public void Clear()
        {
            foreach (var plan in _loadPlan.Values)
            {
                HashSetPool<(string, Type)>.Release(plan);
            }
            _loadPlan.Clear();
        }

        public int GetRefCount(string bundleName)
        {
            return _loadPlan.TryGetValue(bundleName, out var handler) ? handler.Count : 0;
        }

        public void AddPlan(string bundleName, string assetPath, Type assetType)
        {
            if (_loadPlan.TryGetValue(bundleName, out var plan))
            {
                plan.Add((assetPath, assetType));
            }
            else
            {
                plan = HashSetPool<(string, Type)>.Get();
                plan.Add((assetPath, assetType));
                _loadPlan[bundleName] = plan;
            }
        }

        public bool TryGetPlan(string bundleName, out HashSet<(string, Type)> plan)
        {
            return _loadPlan.TryGetValue(bundleName, out plan);
        }

        public void RemovePlan(string bundleName, string assetPath)
        {
            if (_loadPlan.TryGetValue(bundleName, out var plan))
            {
                plan.RemoveWhere(item => item.Item1 == assetPath);
                if (plan.Count == 0)
                {
                    _loadPlan.Remove(bundleName);
                    HashSetPool<(string, Type)>.Release(plan);
                }
            }
        }

        public void ClearPlan(string bundleName)
        {
            if (_loadPlan.TryGetValue(bundleName, out var plan))
            {
                _loadPlan.Remove(bundleName);
                HashSetPool<(string, Type)>.Release(plan);
            }
        }
    }
}