using System.Collections.Generic;

namespace PowerCellStudio.Editor
{
    /// <summary>
    /// Bundle 对比窗口使用的展示格式化方法。
    /// Presentation formatters used by the Bundle comparison window.
    /// </summary>
    internal static class BundleReferenceCompareFormatter
    {
        internal static string FormatSignedSize(long bytes)
        {
            return bytes > 0
                ? "+" + BundleReferenceCompareUtility.FormatSize(bytes)
                : bytes < 0
                    ? "-" + BundleReferenceCompareUtility.FormatSize(-bytes)
                    : "0 B";
        }

        internal static string FormatTypes(Dictionary<string, int> types)
        {
            return BundleReferenceCompareUtility.FormatTypes(types);
        }
    }
}
