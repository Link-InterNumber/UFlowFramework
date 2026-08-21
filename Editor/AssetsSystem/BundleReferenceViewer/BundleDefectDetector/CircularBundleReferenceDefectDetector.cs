using System.Collections.Generic;

namespace PowerCellStudio.Editor
{
    /// <summary>
    /// 检测 Bundle 是否处于循环依赖链中。
    /// Detects whether a bundle is part of a circular dependency chain.
    /// </summary>
    public sealed class CircularBundleReferenceDefectDetector : IBundleDefectDetector, System.IDisposable
    {
        private BundleReferenceQueryer _cachedQueryer;
        private HashSet<string> _cyclicBundles;

        public string title => "循环引用";

        public string toolTips => "Bundle 通过依赖链直接或间接依赖自身。";

        public string tag => "循环引用";

        public DefectLevel defectLevel => DefectLevel.High;

        public void Dispose()
        {
            _cachedQueryer = null;
            _cyclicBundles?.Clear();
            _cyclicBundles = null;
        }

        public bool Detect(BundleReferenceQueryer queryer, BundleReferenceData bundleData)
        {
            if (queryer == null || bundleData == null || string.IsNullOrEmpty(bundleData.bundleName))
                return false;

            EnsureCache(queryer);
            return _cyclicBundles.Contains(bundleData.bundleName);
        }

        public bool HasDefect(BundleReferenceQueryer queryer, BundleReferenceGroup group)
        {
            if (queryer == null || group?.bundleNames == null)
                return false;

            EnsureCache(queryer);
            foreach (var bundleName in group.bundleNames)
            {
                if (_cyclicBundles.Contains(bundleName))
                    return true;
            }

            return false;
        }

        private void EnsureCache(BundleReferenceQueryer queryer)
        {
            if (_cachedQueryer == queryer && _cyclicBundles != null)
                return;

            _cachedQueryer = queryer;
            _cyclicBundles = FindCyclicBundles(queryer.GetAllBundleData());
        }

        private static HashSet<string> FindCyclicBundles(
            IReadOnlyDictionary<string, BundleReferenceData> allBundles)
        {
            var cyclicBundles = new HashSet<string>();
            if (allBundles == null || allBundles.Count == 0)
                return cyclicBundles;

            var dependencies = CreateDependencySnapshot(allBundles);
            var referenced = CreateReferencedSnapshot(allBundles);
            var visited = new HashSet<string>();
            var orderedBundles = new List<string>(allBundles.Keys);
            var finishOrder = new List<string>(orderedBundles.Count);
            for (var i = 0; i < orderedBundles.Count; i++)
            {
                var start = orderedBundles[i];
                if (visited.Contains(start))
                    continue;
                BuildFinishOrder(start, dependencies, allBundles, visited, finishOrder);
            }

            visited.Clear();
            for (var i = finishOrder.Count - 1; i >= 0; i--)
            {
                var start = finishOrder[i];
                if (visited.Contains(start))
                    continue;

                var component = CollectComponent(start, referenced, allBundles, visited);
                if (component.Count > 1 || HasSelfReference(start, dependencies))
                    cyclicBundles.UnionWith(component);
            }

            return cyclicBundles;
        }

        private static void BuildFinishOrder(
            string start,
            IReadOnlyDictionary<string, string[]> dependencies,
            IReadOnlyDictionary<string, BundleReferenceData> allBundles,
            HashSet<string> visited,
            List<string> finishOrder)
        {
            var stack = new Stack<TraversalFrame>();
            visited.Add(start);
            stack.Push(new TraversalFrame(start));
            while (stack.Count > 0)
            {
                var frame = stack.Pop();
                var currentDependencies = dependencies[frame.bundleName];
                if (frame.nextIndex < currentDependencies.Length)
                {
                    stack.Push(new TraversalFrame(frame.bundleName, frame.nextIndex + 1));
                    var dependency = currentDependencies[frame.nextIndex];
                    if (!string.IsNullOrEmpty(dependency) && allBundles.ContainsKey(dependency) && visited.Add(dependency))
                        stack.Push(new TraversalFrame(dependency));
                    continue;
                }
                finishOrder.Add(frame.bundleName);
            }
        }

        private static HashSet<string> CollectComponent(
            string start,
            IReadOnlyDictionary<string, string[]> referenced,
            IReadOnlyDictionary<string, BundleReferenceData> allBundles,
            HashSet<string> visited)
        {
            var component = new HashSet<string>();
            var stack = new Stack<string>();
            stack.Push(start);
            visited.Add(start);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                component.Add(current);
                foreach (var source in referenced[current])
                {
                    if (!string.IsNullOrEmpty(source) && allBundles.ContainsKey(source) && visited.Add(source))
                        stack.Push(source);
                }
            }
            return component;
        }

        private static bool HasSelfReference(
            string bundleName,
            IReadOnlyDictionary<string, string[]> dependencies)
        {
            var bundleDependencies = dependencies[bundleName];
            for (var i = 0; i < bundleDependencies.Length; i++)
            {
                if (bundleDependencies[i] == bundleName)
                    return true;
            }
            return false;
        }

        private static Dictionary<string, string[]> CreateDependencySnapshot(
            IReadOnlyDictionary<string, BundleReferenceData> allBundles,
            bool referenced = false)
        {
            var snapshot = new Dictionary<string, string[]>(allBundles.Count);
            foreach (var pair in allBundles)
            {
                var source = referenced ? pair.Value.bundleReferenced : pair.Value.bundleDependent;
                snapshot[pair.Key] = source == null ? System.Array.Empty<string>() : new List<string>(source).ToArray();
            }
            return snapshot;
        }

        private static Dictionary<string, string[]> CreateReferencedSnapshot(
            IReadOnlyDictionary<string, BundleReferenceData> allBundles)
        {
            return CreateDependencySnapshot(allBundles, true);
        }

        private readonly struct TraversalFrame
        {
            public readonly string bundleName;
            public readonly int nextIndex;

            public TraversalFrame(string bundleName, int nextIndex = 0)
            {
                this.bundleName = bundleName;
                this.nextIndex = nextIndex;
            }
        }
    }
}