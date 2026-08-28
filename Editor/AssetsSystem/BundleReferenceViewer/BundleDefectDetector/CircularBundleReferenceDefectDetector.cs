// ==================== 3. 循环依赖检测器 ====================

using System.Collections.Generic;
using UnityEngine.Pool;

namespace PowerCellStudio.Editor
{
    /// <summary>
    /// 检测Bundle依赖图中是否存在循环引用（环）。
    /// 循环依赖会导致加载死锁、冗余加载和难以维护的依赖关系。
    /// </summary>
    public sealed class CircularBundleReferenceDefectDetector : IBundleDefectDetector
    {
        public string title => "循环依赖";
        public string toolTips => "Bundle之间存在循环引用（A→B→A），可能导致加载异常或资源冗余。";
        public string tag => "循环依赖";
        public DefectLevel defectLevel => DefectLevel.High;

        private int _maxRecursionDepth = 6;

        public bool Detect(BundleReferenceQueryer queryer, BundleReferenceData bundleData, out string defectDetail)
        {
            defectDetail = null;
            if (queryer == null || bundleData == null || string.IsNullOrEmpty(bundleData.bundleName))
                return false;

            // 使用DFS检测从当前节点出发是否存在环
            var state = DictionaryPool<string, int>.Get(); // 0-未访问，1-访问中，2-已访问
            var path = new List<string>(_maxRecursionDepth + 2);
            var result = HasCycle(queryer, bundleData.bundleName, state, path, 0, out var cyclePath);
            DictionaryPool<string, int>.Release(state);
            if (result)
            {
                defectDetail = $"Bundle '{bundleData.bundleName}' 存在循环依赖，环路: {string.Join(" -> ", cyclePath)}。";
            }
            return result;
        }

        private bool HasCycle(BundleReferenceQueryer queryer, string node,
            Dictionary<string, int> state, List<string> path, int depth, out List<string> cyclePath)
        {
            cyclePath = null;
            if (state.TryGetValue(node, out int status))
            {
                if (status != 1)
                    return false;

                var cycleStartIndex = path.IndexOf(node);
                if (cycleStartIndex < 0)
                    return false;

                cyclePath = path.GetRange(cycleStartIndex, path.Count - cycleStartIndex);
                cyclePath.Add(node);
                return true;
            }

            if (depth > _maxRecursionDepth)
            {
                state[node] = 2; // 标记已访问
                return false; // 超过最大递归深度，认为没有环
            }

            state[node] = 1; // 标记访问中
            path.Add(node);

            var data = queryer.GetBundleData(node);
            if (data?.bundleDependent != null)
            {
                foreach (var dep in data.bundleDependent)
                {
                    if (HasCycle(queryer, dep, state, path, depth + 1, out cyclePath))
                        return true;
                }
            }

            path.RemoveAt(path.Count - 1);
            state[node] = 2; // 标记已访问
            return false;
        }

        public bool HasDefect(BundleReferenceQueryer queryer, BundleReferenceGroup group)
        {
            if (queryer == null || group?.bundleNames == null)
                return false;

            // 可以逐Bundle检测，但为了效率，整个图检测一次即可，但现有接口只能返回bool，
            // 这里简单遍历，但会重复计算，可优化但忽略。
            foreach (var bundleName in group.bundleNames)
            {
                if (Detect(queryer, queryer.GetBundleData(bundleName), out _))
                    return true;
            }
            return false;
        }
    }
}