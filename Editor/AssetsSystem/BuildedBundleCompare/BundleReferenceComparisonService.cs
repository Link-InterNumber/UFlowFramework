using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace PowerCellStudio.Editor
{
    /// <summary>
    /// 执行已构建 Bundle、当前配置和历史基准之间的对比。
    /// Compares built Bundles with the current configuration and an optional baseline.
    /// </summary>
    internal static class BundleReferenceComparisonService
    {
        internal static List<BundleCompareItem> Compare(IReadOnlyList<BundleBuildBaselineInfo> baseline, ISet<string> currentBundleNames)
        {
            var items = new List<BundleCompareItem>();
            var baselineData = CreateBaselineMap(baseline);
            var builtNames = BundleReferenceManifest.manifest.GetAllAssetBundles() ?? Array.Empty<string>();
            var currentNames = currentBundleNames ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var allNames = new HashSet<string>(builtNames, StringComparer.OrdinalIgnoreCase);
            allNames.UnionWith(currentNames);
            allNames.UnionWith(baselineData.Keys);
            foreach (var bundleName in allNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
            {
                var builtPath = BundleReferenceManifest.manifest.GetBundlePath(bundleName);
                var isCurrentBundle = currentNames.Contains(bundleName);
                var builtData = BundleReferenceCompareUtility.ReadBuiltMetadata(builtPath);
                var status = builtData.exists && isCurrentBundle
                    ? BundleCompareStatus.Unanalyzed
                    : GetStatus(builtData, new HashSet<string>(StringComparer.OrdinalIgnoreCase), isCurrentBundle);
                var item = new BundleCompareItem(
                    bundleName,
                    status,
                    builtData.size,
                    null,
                    null,
                    null,
                    null,
                    0);

                if (baseline != null)
                    ApplyBaseline(item, builtData, baselineData);
                items.Add(item);
            }

            items.Sort((a, b) => b.builtSize.CompareTo(a.builtSize));
            return items;
        }

        internal static void Analyze(
            BundleCompareItem item,
            IReadOnlyList<BundleBuildBaselineInfo> baseline,
            IDictionary<string, BuiltBundleData> builtData)
        {
            if (item == null || builtData == null)
                return;
            if (item.isAnalyzed)
                return;

            if (!builtData.TryGetValue(item.bundleName, out var rootData) || !rootData.exists)
            {
                var builtPath = BundleReferenceManifest.manifest.GetBundlePath(item.bundleName);
                rootData = BundleReferenceCompareUtility.ReadBuiltAssets(builtPath);
                builtData[item.bundleName] = rootData;
            }

            BundleReferenceCompareUtility.CollectDependencyData(
                item.bundleName,
                BundleReferenceManifest.manifest,
                builtData);
            var currentAssets = BundleReferenceCompareUtility.GetCurrentAssets(item.bundleName);
            var isCurrentBundle = BundleReferenceCompareUtility.HasCurrentBundle(item.bundleName);
            item.status = GetStatus(rootData, currentAssets, isCurrentBundle);
            item.SetAnalysisResult(
                rootData.assetNames,
                currentAssets,
                rootData.types,
                rootData.dependentBundles,
                rootData.loadCost);
            if (baseline != null)
                ApplyBaseline(item, rootData, CreateBaselineMap(baseline));
        }

        internal static string BuildSummary(IReadOnlyList<BundleCompareItem> items)
        {
            if (items == null || items.Count == 0)
                return "没有可比较的 Bundle 数据。";

            var builtCount = items.Count(item => item.status != BundleCompareStatus.Added);
            var currentCount = items.Count(item => item.status != BundleCompareStatus.Removed);
            var added = items.Count(item => item.status == BundleCompareStatus.Added);
            var removed = items.Count(item => item.status == BundleCompareStatus.Removed);
            var size = items.Sum(item => item.builtSize);
            return $"已构建 Bundle：{builtCount}  |  当前配置 Bundle：{currentCount}  |  新增：{added}  |  移除：{removed}  |  已构建总大小：{BundleReferenceCompareUtility.FormatSize(size)}";
        }

        private static Dictionary<string, BundleBuildBaselineInfo> CreateBaselineMap(
            IReadOnlyList<BundleBuildBaselineInfo> baseline)
        {
            var result = new Dictionary<string, BundleBuildBaselineInfo>(StringComparer.OrdinalIgnoreCase);
            if (baseline == null)
                return result;

            foreach (var record in baseline)
            {
                if (!string.IsNullOrEmpty(record.bundleName))
                    result[record.bundleName] = record;
            }

            return result;
        }

        private static BundleCompareStatus GetStatus(
            BuiltBundleData builtAssets,
            ISet<string> currentAssets,
            bool isCurrentBundle)
        {
            if (!builtAssets.exists && isCurrentBundle)
                return BundleCompareStatus.Added;
            if (builtAssets.exists && !isCurrentBundle)
                return BundleCompareStatus.Removed;
            return builtAssets.assetNames.SetEquals(currentAssets)
                ? BundleCompareStatus.Same
                : BundleCompareStatus.Changed;
        }

        private static void ApplyBaseline(
            BundleCompareItem item,
            BuiltBundleData builtAssets,
            IReadOnlyDictionary<string, BundleBuildBaselineInfo> baselineData)
        {
            if (!baselineData.TryGetValue(item.bundleName, out var baselineItem))
            {
                if (builtAssets.exists)
                    item.baselineStatus = BundleCompareStatus.Added;
                return;
            }

            item.hasBaseline = true;
            item.baselineStatus = builtAssets.exists &&
                                   builtAssets.assetNames.SetEquals(baselineItem.assetNames ?? Array.Empty<string>())
                ? BundleCompareStatus.Same
                : builtAssets.exists ? BundleCompareStatus.Changed : BundleCompareStatus.Removed;
            item.baselineSize = baselineItem.size;
            item.baselineAssets = new HashSet<string>(
                baselineItem.assetNames ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            item.baselineDependentBundles = new List<string>(
                baselineItem.dependentBundles ?? Array.Empty<string>());

            foreach (var baselineAsset in item.baselineAssets)
            {
                if (!item.allAssets.Contains(baselineAsset))
                    item.allAssets.Add(baselineAsset);
            }

            item.allAssets.Sort(StringComparer.OrdinalIgnoreCase);
        }
    }
}
