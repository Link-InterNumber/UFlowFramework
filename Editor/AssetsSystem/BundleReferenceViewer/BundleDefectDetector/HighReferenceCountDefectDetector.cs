// ==================== 2. 引用次数过多导致资源冗余加载检测器 ====================
namespace PowerCellStudio.Editor
{
    /// <summary>
    /// 检测Bundle被大量其他Bundle直接引用，且自身资源数量较多。
    /// 这种情况下，每个引用者加载时都可能拉取该Bundle，而可能只用到其中少数资源，造成浪费。
    /// </summary>
    public sealed class HighReferenceCountDefectDetector : IBundleDefectDetector
    {
        private const int REFERENCE_THRESHOLD = 10;
        private const int ASSET_THRESHOLD = 5;

        public string title => "引用过于分散";
        public string toolTips => $"Bundle被 {REFERENCE_THRESHOLD} 个以上其他Bundle引用，且自身包含 {ASSET_THRESHOLD} 个以上资源，可能加载大量无关资源。";
        public string tag => "引用分散";
        public DefectLevel defectLevel => DefectLevel.Medium;

        public bool Detect(BundleReferenceQueryer queryer, BundleReferenceData bundleData, out string defectDetail)
        {
            defectDetail = null;
            if (queryer == null || bundleData == null || string.IsNullOrEmpty(bundleData.bundleName))
                return false;

            if (bundleData.bundleReferenced == null || bundleData.bundleReferenced.Count < REFERENCE_THRESHOLD)
                return false;

            int assetCount = bundleData.assets?.Count ?? 0;
            if (assetCount >= ASSET_THRESHOLD)
            {
                defectDetail = $"Bundle '{bundleData.bundleName}' 被 {bundleData.bundleReferenced.Count} 个其他 Bundle 引用，且包含 {assetCount} 个资源；引用方: {string.Join(", ", bundleData.bundleReferenced)}，可能加载大量无关资源。";
                return true;
            }
            return false;
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