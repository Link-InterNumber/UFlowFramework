// ==================== 1. 依赖链路过长检测器 ====================
using System.Collections.Generic;
using System.Linq;

namespace PowerCellStudio.Editor
{
    /// <summary>
    /// 检测Bundle的依赖深度是否超过阈值（默认5层）。
    /// 深层依赖会导致加载时串联大量AssetBundle，增加加载耗时和内存占用。
    /// </summary>
    public sealed class DeepDependencyDefectDetector : IBundleDefectDetector
    {
        private const int MAX_DEPTH = 5;

        public string title => "依赖链路过长";
        public string toolTips => $"Bundle的依赖深度超过 {MAX_DEPTH} 层，可能导致加载性能问题。";
        public string tag => "依赖链路过长";
        public DefectLevel defectLevel => DefectLevel.Medium;

        public bool Detect(BundleReferenceQueryer queryer, BundleReferenceData bundleData, out string defectDetail)
        {
            defectDetail = null;
            if (queryer == null || bundleData == null || string.IsNullOrEmpty(bundleData.bundleName))
                return false;

            // 计算从当前Bundle出发的最长依赖深度（BFS）
            var visited = new HashSet<string>();
            var queue = new Queue<(string name, int depth)>();
            var parent = new Dictionary<string, string>();
            queue.Enqueue((bundleData.bundleName, 0));
            visited.Add(bundleData.bundleName);

            int maxDepth = 0;
            while (queue.Count > 0)
            {
                var (current, depth) = queue.Dequeue();
                if (depth > maxDepth) maxDepth = depth;
                if (depth >= MAX_DEPTH)
                {
                    var chain = BuildChain(current, parent);
                    defectDetail = $"Bundle '{bundleData.bundleName}' 的依赖链路超过 {MAX_DEPTH} 层，最长链路: {string.Join(" -> ", chain)}（共 {depth} 层）。";
                    return true;
                }

                var data = queryer.GetBundleData(current);
                if (data?.bundleDependent == null) continue;

                foreach (var dep in data.bundleDependent)
                {
                    if (!visited.Contains(dep))
                    {
                        visited.Add(dep);
                        parent[dep] = current;
                        queue.Enqueue((dep, depth + 1));
                    }
                }
            }

            return false;
        }

        private static List<string> BuildChain(string node, Dictionary<string, string> parent)
        {
            var chain = new List<string>();
            while (!string.IsNullOrEmpty(node))
            {
                chain.Add(node);
                if (!parent.TryGetValue(node, out node))
                    break;
            }
            chain.Reverse();
            return chain;
        }

        public bool HasDefect(BundleReferenceQueryer queryer, BundleReferenceGroup group)
        {
            if (queryer == null || group?.bundleNames == null)
                return false;

            foreach (var bundleName in group.bundleNames)
            {
                if (Detect(queryer, queryer.GetBundleData(bundleName), out _))
                    return true;
            }
            return false;
        }
    }
}