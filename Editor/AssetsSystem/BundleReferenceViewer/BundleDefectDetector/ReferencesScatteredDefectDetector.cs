using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace PowerCellStudio.Editor
{
    /// <summary>
    /// 检测 Bundle 内部资源被外部 Bundle 使用，同时又被本 Bundle 的其他资源使用的情况。
    /// Detects assets shared externally while also being used by other assets in the same Bundle.
    /// </summary>
    public sealed class ReferencesScatteredDefectDetector : IBundleDefectDetector, IDisposable
    {
        public string title => "内部资源被外部多个分包引用";
        public string toolTips => "Bundle 内资源被外部多个 Bundle 直接引用，同时每个分包使用的资源不相同，可能导致加载无关资源并拉长加载链。";
        public string tag => "引用分散或冗余";
        public DefectLevel defectLevel => DefectLevel.Medium;

        public bool Detect(BundleReferenceQueryer queryer, BundleReferenceData bundleData)
        {
            if (queryer == null || bundleData == null || string.IsNullOrEmpty(bundleData.bundleName))
                return false;
            if (bundleData.bundleReferenced == null || bundleData.bundleReferenced.Count == 0)
                return false;
            if (bundleData.assets == null || bundleData.assets.Count <= 1)
                return false;

            var bundleReferencedDict = DictionaryPool<string, HashSet<string>>.Get();
            try
            {
                foreach (var assetReferenceData in bundleData.assets)
                {
                    if (assetReferenceData?.bundleReferenced == null)
                        continue;

                    foreach (var se in assetReferenceData.bundleReferenced)
                    {
                        var referencingAsset = queryer.GetAsset(se);
                        if (referencingAsset == null ||
                            string.IsNullOrEmpty(referencingAsset.bundleName) ||
                            string.Equals(referencingAsset.bundleName, bundleData.bundleName,
                                StringComparison.OrdinalIgnoreCase) ||
                            !bundleData.bundleReferenced.Contains(referencingAsset.bundleName))
                            continue;

                        if (!bundleReferencedDict.TryGetValue(referencingAsset.bundleName,
                                out var referencedAssets))
                        {
                            referencedAssets = HashSetPool<string>.Get();
                            bundleReferencedDict.Add(referencingAsset.bundleName, referencedAssets);
                        }

                        referencedAssets.Add(assetReferenceData.assetPath);
                    }
                }

                if (bundleReferencedDict.Count < 2)
                    return false;

                HashSet<string> firstReferencedAssets = null;
                foreach (var referencedAssets in bundleReferencedDict.Values)
                {
                    if (firstReferencedAssets == null)
                    {
                        firstReferencedAssets = referencedAssets;
                        continue;
                    }

                    if (!firstReferencedAssets.SetEquals(referencedAssets))
                        return true;
                }

                return false;
            }
            finally
            {
                foreach (var referencedAssets in bundleReferencedDict.Values)
                    HashSetPool<string>.Release(referencedAssets);
                DictionaryPool<string, HashSet<string>>.Release(bundleReferencedDict);
            }
        }

        public bool HasDefect(BundleReferenceQueryer queryer, BundleReferenceGroup group)
        {
            if (queryer == null || group?.bundleNames == null)
                return false;

            foreach (var bundleName in group.bundleNames)
            {
                if (Detect(queryer, queryer.GetBundleData(bundleName)))
                    return true;
            }

            return false;
        }

        public void Dispose()
        {
        }
    }
}