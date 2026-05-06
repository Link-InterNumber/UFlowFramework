using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;

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
            var tempStack = HashSetPool<string>.Get();
            GetBundleDependenciesRecursively(bundleName, ref tempStack);
            var result = tempStack.ToArray();
            dependencies[bundleName] = result;
            HashSetPool<string>.Release(tempStack);
            return result;
        }

        private void GetBundleDependenciesRecursively(string bundleName, ref HashSet<string> dependenciesStack)
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
                dependenciesStack.Add(dependencyName);
                GetBundleDependenciesRecursively(dependencyName, ref dependenciesStack);
            }
        }
    }
}