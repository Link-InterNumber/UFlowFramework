using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public class BundleDependenceMap
    {
        private AssetBundleManifest manifest;
        
        private Dictionary<string, string[]> dependencies;

        public BundleDependenceMap(AssetBundleManifest manifest)
        {
            this.manifest = manifest;
            dependencies =  new Dictionary<string, string[]>();
        }

        public string[] GetBundleDependencies(string bundleName)
        {
            if (dependencies.TryGetValue(bundleName, out var dependenciesArray))
            {
                return dependenciesArray;
            }
            var tempStack = HashStackPool<string>.Get();
            GetBundleDependenciesRecursively(bundleName, ref tempStack);
            var result = new string[tempStack.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = tempStack.Pop();
            }
            dependencies[bundleName] = result;
            if (!HashStackPool<string>.Release(tempStack)) 
                tempStack.Clear();
            return result;
        }

        private void GetBundleDependenciesRecursively(string bundleName, ref HashStack<string> dependenciesStack)
        {
            var bundles = manifest.GetAllDependencies(bundleName);
            if (bundles == null || bundles.Length == 0)
            {
                return;
            }
            for (var i = 0; i < bundles.Length; i++)
            {
                var dependencyName = bundles[i];
                if (dependenciesStack.Contains(dependencyName)) continue;
                if (dependencyName == bundleName) continue;
                dependenciesStack.Push(dependencyName);
                GetBundleDependenciesRecursively(dependencyName, ref dependenciesStack);
            }
        }
    }
}