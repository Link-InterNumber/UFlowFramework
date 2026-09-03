#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

namespace PowerCellStudio
{
    public sealed class EditorLoadDependencyProvider : ILoadDependencyProvider
    {
        private Dictionary<string, string[]> _assetDependenciesCache = new Dictionary<string, string[]>();
        private Dictionary<string, string[]> _assetBundleDependenciesCache = new Dictionary<string, string[]>();
        
        public string[] GetAssetDependencies(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;

            if (_assetDependenciesCache.TryGetValue(assetPath, out var dependencies))
                return dependencies;

            dependencies = AssetDatabase.GetDependencies(assetPath, true);
            _assetDependenciesCache[assetPath] = dependencies;
            return dependencies;
        }

        public string[] GetAssetBundleDependencies(string assetBundleName)
        {
            if (string.IsNullOrEmpty(assetBundleName))
                return null;

            if (_assetBundleDependenciesCache.TryGetValue(assetBundleName, out var dependencies))
                return dependencies;

            dependencies = AssetDatabase.GetAssetBundleDependencies(assetBundleName, true);
            _assetBundleDependenciesCache[assetBundleName] = dependencies;
            return dependencies;
        }

        public void Dispose()
        {
            _assetDependenciesCache.Clear();
            _assetBundleDependenciesCache.Clear();
        }
    }
}
#endif