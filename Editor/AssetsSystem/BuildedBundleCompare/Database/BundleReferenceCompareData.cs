using System;
using System.Collections.Generic;

namespace PowerCellStudio.Editor
{
    internal enum BundleCompareStatus
    {
        Same,
        Added,
        Removed,
        Changed
    }

    internal sealed class BuiltBundleData
    {
        public bool exists;
        public long size;
        public readonly HashSet<string> assetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, int> types = new Dictionary<string, int>();

        public List<string> dependentBundles = new List<string>();

        public long loadCost;
    }

    internal sealed class BundleCompareItem
    {
        public readonly string bundleName;
        public readonly BundleCompareStatus status;
        public BundleCompareStatus baselineStatus;
        public readonly long builtSize;
        public readonly HashSet<string> builtAssets;
        public readonly HashSet<string> currentAssets;
        public readonly Dictionary<string, int> builtTypes;
        public readonly List<string> dependentBundles;
        public readonly long loadCost;
        public readonly List<string> allAssets;
        public readonly HashSet<string> addedAssets;
        public readonly HashSet<string> removedAssets;
        public bool hasBaseline;
        public long baselineSize;
        public HashSet<string> baselineAssets;
        public List<string> baselineDependentBundles;

        public BundleCompareItem(
            string name,
            BundleCompareStatus itemStatus,
            long size,
            HashSet<string> built,
            HashSet<string> current,
            Dictionary<string, int> types,
            List<string> dependencies,
            long totalLoadCost)
        {
            bundleName = name;
            status = itemStatus;
            baselineStatus = BundleCompareStatus.Same;
            builtSize = size;
            builtAssets = built;
            currentAssets = current;
            builtTypes = types;
            dependentBundles = dependencies ?? new List<string>();
            loadCost = totalLoadCost;

            addedAssets = new HashSet<string>(current, StringComparer.OrdinalIgnoreCase);
            addedAssets.ExceptWith(built);

            removedAssets = new HashSet<string>(built, StringComparer.OrdinalIgnoreCase);
            removedAssets.ExceptWith(current);

            allAssets = new List<string>(built.Count + current.Count);
            allAssets.AddRange(built);
            foreach (var asset in current)
            {
                if (!built.Contains(asset))
                    allAssets.Add(asset);
            }
            allAssets.Sort(StringComparer.OrdinalIgnoreCase);
        }
    }
}
