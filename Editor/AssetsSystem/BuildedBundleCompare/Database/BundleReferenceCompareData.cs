using System;
using System.Collections.Generic;

namespace PowerCellStudio.Editor
{
    internal enum BundleCompareStatus
    {
        Unanalyzed,
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
        public BundleCompareStatus status;
        public BundleCompareStatus baselineStatus;
        public readonly long builtSize;
        public HashSet<string> builtAssets;
        public HashSet<string> currentAssets;
        public Dictionary<string, int> builtTypes;
        public List<string> dependentBundles;
        public long loadCost;
        public List<string> allAssets;
        public HashSet<string> addedAssets;
        public HashSet<string> removedAssets;
        public bool isAnalyzed;
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
            builtAssets = built ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            currentAssets = current ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            builtTypes = types ?? new Dictionary<string, int>();
            dependentBundles = dependencies ?? new List<string>();
            loadCost = totalLoadCost;
            addedAssets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            removedAssets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            allAssets = new List<string>();
            isAnalyzed = false;
        }

        public void SetAnalysisResult(
            HashSet<string> built,
            HashSet<string> current,
            Dictionary<string, int> types,
            List<string> dependencies,
            long totalLoadCost)
        {
            builtAssets = built ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            currentAssets = current ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            builtTypes = types ?? new Dictionary<string, int>();
            dependentBundles = dependencies ?? new List<string>();
            loadCost = totalLoadCost;
            addedAssets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var asset in currentAssets)
            {
                if (BundleReferenceCompareUtility.FindMatchingAsset(asset, builtAssets) == null)
                    addedAssets.Add(asset);
            }

            removedAssets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var asset in builtAssets)
            {
                if (BundleReferenceCompareUtility.FindMatchingAsset(asset, currentAssets) == null)
                    removedAssets.Add(asset);
            }

            allAssets = new List<string>(builtAssets.Count + currentAssets.Count);
            allAssets.AddRange(builtAssets);
            foreach (var asset in currentAssets)
            {
                if (BundleReferenceCompareUtility.FindMatchingAsset(asset, builtAssets) == null)
                    allAssets.Add(asset);
            }
            allAssets.Sort(StringComparer.OrdinalIgnoreCase);
            isAnalyzed = true;
        }
    }
}
