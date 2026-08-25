using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Pool;

namespace PowerCellStudio.Editor
{
    internal static class BundleReferenceCompareUtility
    {
        internal static string FindBundlePath(string directory, string bundleName, string manifestName)
        {
            var directPath = Path.Combine(directory, bundleName.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(directPath))
                return directPath;

            var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
            for (var i = 0; i < files.Length; i++)
            {
                var fileName = Path.GetFileName(files[i]);
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(files[i]);
                if (string.Equals(fileName, bundleName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(fileNameWithoutExtension, bundleName, StringComparison.OrdinalIgnoreCase))
                    return files[i];

                if (!string.IsNullOrEmpty(manifestName) &&
                    string.Equals(fileNameWithoutExtension, manifestName, StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            return null;
        }

        internal static BuiltBundleData ReadBuiltAssets(string path)
        {
            var result = new BuiltBundleData();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return result;

            result.exists = true;
            result.size = new FileInfo(path).Length;
            var bundle = AssetBundle.LoadFromFile(path);
            if (bundle == null)
                return result;

            try
            {
                foreach (var assetName in bundle.GetAllAssetNames())
                {
                    var normalized = NormalizePath(assetName);
                    result.assetNames.Add(normalized);

                    var typeName = GetTypeFromExtension(normalized);
                    result.types.TryGetValue(typeName, out var count);
                    result.types[typeName] = count + 1;

                    // var asset = bundle.LoadAsset<UnityEngine.Object>(assetName);
                    // // var runtimeSize = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(asset);
                    // var typeName = asset != null ? asset.GetType().Name : GetTypeFromExtension(normalized);
                    // result.types.TryGetValue(typeName, out var count);
                    // result.types[typeName] = count + 1;
                    // Resources.UnloadAsset(asset);
                }
            }
            finally
            {
                bundle.Unload(false);
            }

            return result;
        }

        internal static void CollectDependencyData(
            string bundleName,
            string directory,
            string manifestName,
            AssetBundleManifest manifest,
            IDictionary<string, BuiltBundleData> builtData)
        {
            if (string.IsNullOrEmpty(bundleName) || manifest == null || builtData == null)
                return;

            if (!builtData.TryGetValue(bundleName, out var rootData))
            {
                rootData = ReadBuiltAssets(FindBundlePath(directory, bundleName, manifestName));
                builtData[bundleName] = rootData;
            }

            var dependencies = HashSetPool<string>.Get();
            CollectDependencyNames(bundleName, manifest, dependencies);
            rootData.dependentBundles.Clear();
            rootData.dependentBundles.AddRange(dependencies);

            rootData.loadCost = rootData.size;
            foreach (var dependencyName in dependencies)
            {
                if (!builtData.TryGetValue(dependencyName, out var dependencyData))
                {
                    dependencyData = ReadBuiltAssets(FindBundlePath(directory, dependencyName, manifestName));
                    builtData[dependencyName] = dependencyData;
                }

                rootData.loadCost += dependencyData.size;
            }
            HashSetPool<string>.Release(dependencies);
        }

        private static void CollectDependencyNames(
            string bundleName,
            AssetBundleManifest manifest,
            ISet<string> dependencies)
        {
            var directDependencies = manifest.GetDirectDependencies(bundleName) ?? Array.Empty<string>();
            foreach (var dependencyName in directDependencies)
            {
                if (dependencies.Add(dependencyName))
                    CollectDependencyNames(dependencyName, manifest, dependencies);
            }
        }

        internal static HashSet<string> GetCurrentAssets(BundleReferenceQueryer queryer, string bundleName)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!queryer.GetAllBundleData().TryGetValue(bundleName, out var data) || data.assets == null)
                return result;

            foreach (var asset in data.assets)
            {
                if (asset != null && !string.IsNullOrEmpty(asset.assetPath))
                    result.Add(NormalizePath(asset.assetPath));
            }

            return result;
        }

        internal static string FormatTypes(Dictionary<string, int> types)
        {
            if (types == null || types.Count == 0)
                return "无";

            var result = new List<string>(types.Count);
            foreach (var pair in types)
                result.Add($"{pair.Key}({pair.Value})");
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return string.Join("、", result);
        }

        internal static string FormatSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024f:0.##} KB";
            if (bytes < 1024L * 1024L * 1024L)
                return $"{bytes / (1024f * 1024f):0.##} MB";
            return $"{bytes / (1024f * 1024f * 1024f):0.##} GB";
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }

        private static string GetTypeFromExtension(string assetName)
        {
            var extension = Path.GetExtension(assetName);
            if (string.IsNullOrEmpty(extension))
                return "Unknown";

            switch (extension.ToLowerInvariant())
            {
                case ".prefab":
                    return "Prefab";
                case ".mat":
                    return "Material";
                case ".shader":
                case ".shadergraph":
                    return "Shader";
                case ".controller":
                    return "AnimatorController";
                case ".anim":
                    return "AnimationClip";
                case ".unity":
                    return "Scene";
                case ".asset":
                    return "ScriptableObject";
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".tga":
                case ".psd":
                case ".exr":
                    return "Texture";
                case ".wav":
                case ".mp3":
                case ".ogg":
                case ".aiff":
                    return "AudioClip";
                case ".fbx":
                case ".obj":
                case ".dae":
                case ".gltf":
                case ".glb":
                    return "Model";
                case ".ttf":
                case ".otf":
                case ".font":
                    return "Font";
                case ".mp4":
                case ".mov":
                case ".avi":
                case ".wmv":
                    return "VideoClip";
                default:
                    return extension.TrimStart('.').ToUpperInvariant();
            }
        }
    }
}
