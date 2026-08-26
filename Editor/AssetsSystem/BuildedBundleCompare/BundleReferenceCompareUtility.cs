using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

namespace PowerCellStudio.Editor
{
    internal static class BundleReferenceCompareUtility
    {
        // internal static Dictionary<string, string> BuildBundlePathIndex(string directory)
        // {
        //     var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        //     if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        //         return result;
        //
        //     var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
        //     for (var i = 0; i < files.Length; i++)
        //     {
        //         var fileName = Path.GetFileName(files[i]);
        //         var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(files[i]);
        //         if (!result.ContainsKey(fileName))
        //             result.Add(fileName, files[i]);
        //         if (!result.ContainsKey(fileNameWithoutExtension))
        //             result.Add(fileNameWithoutExtension, files[i]);
        //     }
        //
        //     return result;
        // }

        // internal static string FindBundlePath(
        //     string directory,
        //     string bundleName,
        //     string manifestName,
        //     IReadOnlyDictionary<string, string> bundlePathIndex = null)
        // {
        //     if (bundlePathIndex != null && !string.IsNullOrEmpty(bundleName))
        //     {
        //         if (bundlePathIndex.TryGetValue(bundleName, out var indexedPath))
        //             return indexedPath;
        //
        //         var fileName = Path.GetFileName(bundleName);
        //         if (bundlePathIndex.TryGetValue(fileName, out indexedPath))
        //             return indexedPath;
        //     }
        //
        //     return string.Empty;
        //     // return FindBundlePath(directory, bundleName, manifestName);
        // }

        // internal static string FindBundlePath(string directory, string bundleName, string manifestName)
        // {
        //     var directPath = Path.Combine(directory, bundleName.Replace('/', Path.DirectorySeparatorChar));
        //     if (File.Exists(directPath))
        //         return directPath;
        //
        //     var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
        //     for (var i = 0; i < files.Length; i++)
        //     {
        //         var fileName = Path.GetFileName(files[i]);
        //         var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(files[i]);
        //         if (string.Equals(fileName, bundleName, StringComparison.OrdinalIgnoreCase) ||
        //             string.Equals(fileNameWithoutExtension, bundleName, StringComparison.OrdinalIgnoreCase))
        //             return files[i];
        //
        //         if (!string.IsNullOrEmpty(manifestName) &&
        //             string.Equals(fileNameWithoutExtension, manifestName, StringComparison.OrdinalIgnoreCase))
        //             continue;
        //     }
        //
        //     return null;
        // }

        internal static BuiltBundleData ReadBuiltAssets(string path)
        {
            var result = ReadBuiltMetadata(path);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return result;

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

        internal static BuiltBundleData ReadBuiltMetadata(string path)
        {
            var result = new BuiltBundleData();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return result;

            result.exists = true;
            result.size = new FileInfo(path).Length;
            return result;
        }

        internal static void CollectDependencyData(
            string bundleName,
            IBundleReferenceManifest manifest,
            IDictionary<string, BuiltBundleData> builtData)
        {
            if (string.IsNullOrEmpty(bundleName) || manifest == null || builtData == null)
                return;

            if (!builtData.TryGetValue(bundleName, out var rootData))
            {
                var bundlePath = BundleReferenceManifest.manifest.GetBundlePath(bundleName);
                rootData = ReadBuiltAssets(bundlePath);
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
                    var bundlePath = BundleReferenceManifest.manifest.GetBundlePath(dependencyName);
                    dependencyData = ReadBuiltAssets(bundlePath);
                    builtData[dependencyName] = dependencyData;
                }

                rootData.loadCost += dependencyData.size;
            }
            HashSetPool<string>.Release(dependencies);
        }

        private static void CollectDependencyNames(
            string bundleName,
            IBundleReferenceManifest manifest,
            ISet<string> dependencies)
        {
            var directDependencies = manifest.GetDirectDependencies(bundleName) ?? Array.Empty<string>();
            foreach (var dependencyName in directDependencies)
            {
                if (dependencies.Add(dependencyName))
                    CollectDependencyNames(dependencyName, manifest, dependencies);
            }
        }

        internal static HashSet<string> GetCurrentAssets(string bundleName)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(bundleName))
                return result;

            var assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);
            for (var i = 0; i < assetPaths.Length; i++)
            {
                if (!string.IsNullOrEmpty(assetPaths[i]))
                    result.Add(NormalizePath(assetPaths[i]));
            }

            return result;
        }

        internal static bool HasCurrentBundle(string bundleName)
        {
            if (string.IsNullOrEmpty(bundleName))
                return false;
            return AssetDatabase.GetAssetPathsFromAssetBundle(bundleName).Length > 0;
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
