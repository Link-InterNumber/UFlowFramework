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
        public string toolTips => "Bundle 内资源被外部 Bundle 直接引用，同时又被本 Bundle 的其他资源使用，可能导致加载无关资源并拉长加载链。";
        public string tag => "引用分散或冗余";
        public DefectLevel defectLevel => DefectLevel.Medium;

        public bool Detect(BundleReferenceQueryer queryer, BundleReferenceData bundleData)
        {
            if (queryer == null || bundleData == null || string.IsNullOrEmpty(bundleData.bundleName))
                return false;
            if (bundleData.bundleDependent == null || bundleData.bundleDependent.Count == 0)
                return false;
            if (bundleData.assets == null || bundleData.assets.Count <= 1)
                return false;

            var bundleDependentDict = DictionaryPool<string, int>.Get();
            try
            {
                foreach (var se in bundleData.bundleDependent)
                {
                    bundleDependentDict[se] = 0;
                }

                foreach (var assetReferenceData in bundleData.assets)
                {
                    if (assetReferenceData?.assetDependent == null)
                        continue;
                    foreach (var se in assetReferenceData.assetDependent)
                    {
                        var ae = queryer.GetAsset(se);
                        if (ae == null || string.IsNullOrEmpty(ae.bundleName) ||
                            !bundleDependentDict.TryGetValue(ae.bundleName, out var referenceCount))
                            continue;

                        bundleDependentDict[ae.bundleName] = referenceCount + 1;
                    }
                }

                foreach (var referenceCount in bundleDependentDict.Values)
                {
                    if (referenceCount > 0)
                        return true;
                }

                return false;
            }
            finally
            {
                DictionaryPool<string, int>.Release(bundleDependentDict);
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