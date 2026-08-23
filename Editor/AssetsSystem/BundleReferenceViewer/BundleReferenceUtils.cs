using UnityEditor;
using UnityEngine;

namespace PowerCellStudio.Editor
{
    public static class BundleReferenceUtils
    {
        public static void PingAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return;

            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (asset == null)
                return;

            // Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        public static Color GetDefectColor(DefectLevel level)
        {
            if ((level & DefectLevel.High) != 0)
                return new Color(0.75f, 0.18f, 0.18f, 1f);
            if ((level & DefectLevel.Medium) != 0)
                return new Color(0.85f, 0.52f, 0.12f, 1f);
            if ((level & DefectLevel.Low) != 0)
                return new Color(0.72f, 0.65f, 0.12f, 1f);
            return Color.clear;
        }

        public static string GetDefectLevelText(DefectLevel level)
        {
            if ((level & DefectLevel.High) != 0)
                return "高";
            if ((level & DefectLevel.Medium) != 0)
                return "中";
            if ((level & DefectLevel.Low) != 0)
                return "低";
            return "无";
        }
    }
}