using System.Collections.Generic;
using UnityEditor;

namespace PowerCellStudio.Editor
{
    public static class BundleReferenceAnalyzer
    {
        public static void DetectorGroupDefect(BundleReferenceQueryer queryer, BundleDefectDetectorBox detectorBox)
        {
            if (queryer == null || detectorBox == null)
                return;
            queryer.EnsureGroups();

            var allGroup = queryer.GetAllGroups();
            var tempAssetList = new List<AssetReferenceData>();
            foreach (var bundleReferenceGroup in allGroup)
            {
                var group = bundleReferenceGroup.Value;

                // 收集数据
                CollectGroupAssetData(queryer, group, ref tempAssetList);

                var allBundle = group.bundleNames;
                // 执行检查
                foreach (var bundleName in allBundle)
                {
                    detectorBox.DetectBundleAndMarkGroup(queryer.GetBundleData(bundleName), queryer);
                }

                // // 释放数据
                // foreach (var bundleName in allBundle)
                // {
                //     queryer.ReleaseBundleAssetsData(bundleName);
                // }
            }
        }

        public static void CollectGroupAssetData(BundleReferenceQueryer queryer,  BundleReferenceGroup group, ref List<AssetReferenceData> tempAssetList)
        {
            var allBundle = group.bundleNames;
            foreach (var bundleName in allBundle)
            {
                if (queryer.GetBundleData(bundleName) == null)
                {
                    var bundleDependents = AssetDatabase.GetAssetBundleDependencies(bundleName, false);
                    queryer.AddBundleData(bundleName, bundleDependents);
                }
                var bundleData = queryer.GetBundleData(bundleName);
                if (bundleData.assets.Count == 0)
                {
                    var assets = AssetDatabase.GetAssetPathsFromAssetBundle(bundleData.bundleName);
                    AssetReferenceCollector.FindDirectReferences(bundleData.bundleName, assets, ref tempAssetList);
                    queryer.SetAssets(bundleData.bundleName, tempAssetList);
                }
            }
        }
        
        public static void DetectorBundlesDefect(BundleReferenceQueryer queryer, BundleDefectDetectorBox detectorBox, BundleReferenceGroup group)
        {
            var allBundle = group.bundleNames;
            foreach (var bundleName in allBundle)
            {
                var bundleData = queryer.GetBundleData(bundleName);
                if (bundleData.tags.Count > 0)
                    continue;
                detectorBox.DetectBundleOnly(queryer.GetBundleData(bundleName), queryer);
            }
        }
    }
}