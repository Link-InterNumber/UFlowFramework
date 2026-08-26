using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace PowerCellStudio.Editor
{
    /// <summary>
    /// Bundle 对比窗口使用的固定配置、历史键和界面文案。
    /// Fixed settings, editor preference keys, and UI text for Bundle comparison.
    /// </summary>
    internal static class BundleReferenceCompareSettings
    {
        private const string HistoryKeyPrefix = "BundleReferenceCompare";
        private const string BuildDirectorySuffix = "BuildDirectory";
        private const string ManifestNameSuffix = "ManifestName";
        private const string BaselineFileSuffix = "BaselineFile";

        private static readonly string ProjectKeyPrefix = CreateProjectKeyPrefix();

        internal static readonly string HistoryBuildDirectoryKey = CreateHistoryKey(BuildDirectorySuffix);
        internal static readonly string HistoryManifestNameKey = CreateHistoryKey(ManifestNameSuffix);
        internal static readonly string HistoryBaselineFileKey = CreateHistoryKey(BaselineFileSuffix);
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

        private static string _cachedProjectKeyPrefix;

        private static string CreateHistoryKey(string suffix)
        {
            return $"{ProjectKeyPrefix}_{HistoryKeyPrefix}_{suffix}";
        }

        private static string CreateProjectKeyPrefix()
        {
            if (!string.IsNullOrEmpty(_cachedProjectKeyPrefix))
                return _cachedProjectKeyPrefix;
            var projectPath = GetProjectPath();
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(projectPath));
                _cachedProjectKeyPrefix = BitConverter.ToString(bytes, 0, 8).Replace("-", string.Empty);
                return _cachedProjectKeyPrefix;
            }
        }

        private static string GetProjectPath()
        {
            var assetsPath = Application.dataPath;
            var projectPath = Directory.GetParent(assetsPath)?.FullName;
            return string.IsNullOrEmpty(projectPath) ? assetsPath : projectPath;
        }
    }
}
