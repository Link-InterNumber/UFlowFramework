using UnityEngine;

namespace PowerCellStudio.Editor
{
    /// <summary>
    /// Bundle 对比窗口使用的固定配置、历史键和界面文案。
    /// Fixed settings, editor preference keys, and UI text for Bundle comparison.
    /// </summary>
    internal static class BundleReferenceCompareSettings
    {
        internal const string HistoryBuildDirectoryKey = "BundleReferenceCompare_BuildDirectory";
        internal const string HistoryManifestNameKey = "BundleReferenceCompare_ManifestName";
        internal const string HistoryBaselineFileKey = "BundleReferenceCompare_BaselineFile";
        internal const string HeaderTitle = "Bundle 构建对比";
        internal const string HeaderSubtitle = "比较已构建 Bundle 与当前项目配置，快速定位资源变化。";
        internal const string InitialSummary = "请选择已构建 Bundle 目录并开始对比。";

        internal static readonly Color RootBackground = new Color(0.12f, 0.12f, 0.12f, 1f);
        internal static readonly Color PanelBackground = new Color(0.16f, 0.17f, 0.2f, 0.95f);
        internal static readonly Color SummaryBackground = new Color(0.18f, 0.22f, 0.28f, 0.9f);
        internal static readonly Color TitleColor = new Color(0.88f, 0.92f, 0.98f, 1f);
        internal static readonly Color MutedTextColor = new Color(0.62f, 0.66f, 0.72f, 1f);
        internal static readonly Color SummaryTextColor = new Color(0.82f, 0.88f, 0.96f, 1f);
        internal static readonly Color DetailTextColor = new Color(0.78f, 0.82f, 0.9f, 1f);
        internal static readonly Color AssetTextColor = new Color(0.82f, 0.86f, 0.94f, 1f);
        internal static readonly Color BaselineAddedColor = new Color(0.35f, 0.72f, 1f, 1f);
        internal static readonly Color BaselineRemovedColor = new Color(0.82f, 0.48f, 1f, 1f);
    }
}
