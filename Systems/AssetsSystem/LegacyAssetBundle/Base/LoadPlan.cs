using System.Collections.Generic;

namespace PowerCellStudio
{
    public class LoadPlan
    {
        private Dictionary<string, HashSet<string>> _loadPlan;

        public LoadPlan()
        {
            _loadPlan = new Dictionary<string, HashSet<string>>();
        }
        
        public int GetRefCount(string bundleName)
        {
            return _loadPlan.TryGetValue(bundleName, out var handler) ? handler.Count : 0;
        }

        public void AddPlan(string bundleName, string assetPath)
        {
            if (_loadPlan.TryGetValue(bundleName, out var plan))
            {
                plan.Add(assetPath);
            }
            else
            {
                plan = new HashSet<string>();
                plan.Add(assetPath);
                _loadPlan[bundleName] = plan;
            }
        }

        public bool TryPopPlan(string bundleName, out HashSet<string> plan)
        {
            return _loadPlan.Remove(bundleName, out plan);
        }
    }
}