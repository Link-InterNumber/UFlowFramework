using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace PowerCellStudio.Editor
{
    /// <summary>
    /// 检测仅被一个 Bundle 引用且只包含一个资源的 Bundle。
    /// Detects bundles referenced by only one bundle and containing only one asset.
    /// </summary>
    public sealed class SingleReferenceSingleAssetDefectDetector : IBundleDefectDetector
    {
        public string title => "单引用单资源";

        public string toolTips => "Bundle 仅被一个 Bundle 引用，并且自身只包含一个资源，可能存在过度拆包。";

        public string tag => "单引用单资源";

        public DefectLevel defectLevel => DefectLevel.Low;

        public bool Detect(BundleReferenceQueryer queryer, BundleReferenceData bundleData)
        {
            if (queryer == null || bundleData == null || string.IsNullOrEmpty(bundleData.bundleName))
                return false;
            if (bundleData.bundleReferenced == null || bundleData.bundleReferenced.Count != 1)
                return false;

            if (bundleData.assets == null)
            {
                var assets = AssetDatabase.GetAssetPathsFromAssetBundle(bundleData.bundleName);
                var assetData = new List<AssetReferenceData>();
                for (var i = 0; i < assets.Length; i++)
                {
                    var d = AssetReferenceCollector.FindDirectReferences(bundleData.bundleName, assets[i]);
                    if (d == null) continue;
                    assetData.Add(d);
                }
                bundleData.assets = assetData;
                queryer.SetAssets(bundleData.bundleName, bundleData.assets);
            }

            return bundleData.assets?.Count == 1;
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
    }
}