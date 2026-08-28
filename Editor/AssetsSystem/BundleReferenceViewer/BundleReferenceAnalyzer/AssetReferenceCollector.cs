using System;
using System.Collections.Generic;
using UnityEditor;

namespace PowerCellStudio.Editor
{
    public static class AssetReferenceCollector
    {
        internal static void FindDirectReferences(string bundleName, string[] assetPaths, ref List<AssetReferenceData> result)
        {
            result.Clear();
            if (assetPaths == null || assetPaths.Length == 0)
                return;
            for (var i = 0; i < assetPaths.Length; i++)
            {
                var data = FindDirectReferences(bundleName, assetPaths[i]);
                if (data != null)
                    result.Add(data);
            }
        }
        
        /// <summary>
        /// 查找目标资源直接使用的依赖资源路径。
        /// Finds the asset paths directly used by the target asset.
        /// </summary>
        /// <param name="bundleName">资源所属的 AssetBundle 名称。 AssetBundle name containing the target asset.</param>
        /// <param name="assetPath">目标资源的 Unity 资源路径。 Unity asset path of the target asset.</param>
        /// <returns>目标资源直接依赖的路径数组。 Asset paths directly depended on by the target.</returns>
        private static AssetReferenceData FindDirectReferences(string bundleName, string assetPath)
        {
            if (string.IsNullOrEmpty(bundleName) || string.IsNullOrEmpty(assetPath))
                return null;

            var targetPath = NormalizeAssetPath(assetPath);
            var dependencies = AssetDatabase.GetDependencies(targetPath, false);
            var data = new AssetReferenceData(targetPath, bundleName);
            for (var i = 0; i < dependencies.Length; i++)
            {
                var dependency = NormalizeAssetPath(dependencies[i]);
                var bundleBelongsTo = AssetDatabase.GetImplicitAssetBundleName(dependency);
                if (string.Equals(bundleBelongsTo, bundleName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(dependency, targetPath, StringComparison.OrdinalIgnoreCase))
                    continue;
                // if (!string.Equals(dependency, targetPath, StringComparison.OrdinalIgnoreCase))
                data.assetDependent.Add(dependency);
            }
            return data;
        }

        private static string NormalizeAssetPath(string assetPath)
        {
            return assetPath.Replace('\\', '/');
        }
    }
}