// ==================== 4. （可选）孤立Bundle检测器 ====================
namespace PowerCellStudio.Editor
{
    /// <summary>
    /// 检测没有任何其他Bundle引用，且自身也不依赖任何其他Bundle的孤立包。
    /// 这类Bundle可能是废弃资源或未被正确引用，建议清理。
    /// </summary>
    public sealed class OrphanBundleDefectDetector : IBundleDefectDetector
    {
        public string title => "孤立Bundle";
        public string toolTips => "Bundle既没有被任何其他Bundle引用，也不依赖其他Bundle，可能是废弃资源。";
        public string tag => "孤立";
        public DefectLevel defectLevel => DefectLevel.Low;

        public bool Detect(BundleReferenceQueryer queryer, BundleReferenceData bundleData)
        {
            if (queryer == null || bundleData == null || string.IsNullOrEmpty(bundleData.bundleName))
                return false;

            bool noReferenced = bundleData.bundleReferenced == null || bundleData.bundleReferenced.Count == 0;
            bool noDependent = bundleData.bundleDependent == null || bundleData.bundleDependent.Count == 0;
            bool hasAssets = (bundleData.assets?.Count ?? 0) > 0;
            return noReferenced && noDependent && hasAssets;
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