using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PowerCellStudio.Editor
{
    /// <summary>
    /// Bundle 构建基准文件的保存、读取和创建工具。
    /// Creates, saves, and loads Bundle build baseline files.
    /// </summary>
    internal static class BundleBuildBaselineUtility
    {
        internal const int CurrentVersion = 1;
        internal const string FileExtension = ".bundlebaseline";

        internal static void Save(string filePath, IEnumerable<BundleBuildBaselineInfo> records)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("基准文件路径不能为空。", nameof(filePath));
            if (records == null)
                throw new ArgumentNullException(nameof(records));

            var recordList = records is ICollection<BundleBuildBaselineInfo> collection
                ? new List<BundleBuildBaselineInfo>(collection)
                : new List<BundleBuildBaselineInfo>(records);
            var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var temporaryPath = filePath + ".tmp";
            try
            {
                using (var writer = new ReferenceWriter())
                {
                    writer.Write(new[]
                    {
                        new BundleBuildBaselineFile
                        {
                            version = CurrentVersion,
                            bundles = recordList.ToArray()
                        }
                    });
                    writer.Flush(temporaryPath);
                }

                if (File.Exists(filePath))
                    File.Replace(temporaryPath, filePath, null);
                else
                    File.Move(temporaryPath, filePath);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        internal static List<BundleBuildBaselineInfo> Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("基准文件路径不能为空。", nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Bundle 基准文件不存在。", filePath);

            using var reader = new ReferenceReader();
            var files = ReadRecords<BundleBuildBaselineFile>(reader, filePath);
            if (files.Count != 1 || files[0].version != CurrentVersion)
                throw new InvalidDataException("Bundle 基准文件版本不受支持。请重新生成基准文件。");
            return new List<BundleBuildBaselineInfo>(files[0].bundles ?? Array.Empty<BundleBuildBaselineInfo>());
        }

        internal static List<BundleBuildBaselineInfo> CreateFrom(
            string directory,
            string manifestName,
            IBundleReferenceManifest manifest)
        {
            if (string.IsNullOrWhiteSpace(directory) || manifest == null)
                throw new ArgumentException("Bundle 目录和 Manifest 不能为空。");

            var names = manifest.GetAllAssetBundles() ?? Array.Empty<string>();
            var cache = new Dictionary<string, BuiltBundleData>(StringComparer.OrdinalIgnoreCase);
            var result = new List<BundleBuildBaselineInfo>(names.Length);
            for (var i = 0; i < names.Length; i++)
            {
                var bundleName = names[i];
                var path = BundleReferenceManifest.manifest.GetBundlePath(bundleName);
                var data = BundleReferenceCompareUtility.ReadBuiltAssets(path);
                cache[bundleName] = data;
                BundleReferenceCompareUtility.CollectDependencyData(bundleName, manifest, cache);
                result.Add(new BundleBuildBaselineInfo
                {
                    bundleName = bundleName,
                    size = data.size,
                    assetNames = ToSortedArray(data.assetNames),
                    dependentBundles = data.dependentBundles?.ToArray() ?? Array.Empty<string>()
                });
            }
            result.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.bundleName, right.bundleName));
            return result;
        }

        private static List<T> ReadRecords<T>(ReferenceReader reader, string path)
            where T : IBundleReferenceBinary, new()
        {
            return new List<T>(reader.Read<T>(path));
        }

        private static string[] ToSortedArray(IEnumerable<string> values)
        {
            var result = values == null ? new List<string>() : new List<string>(values);
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result.ToArray();
        }
    }
}
