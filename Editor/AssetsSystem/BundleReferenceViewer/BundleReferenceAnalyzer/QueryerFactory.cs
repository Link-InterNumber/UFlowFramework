using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio.Editor
{
    public static class QueryerFactory
    {
        public static async Task<BundleReferenceQueryer> GenerateQueryerByCurrentProject(int analysisBatchSize = 64)
        {
            BundleReferenceQueryer queryer = new BundleReferenceQueryer();
            string[] bundleNames = AssetDatabase.GetAllAssetBundleNames();
            for (var i = 0; i < bundleNames.Length; i++)
            {
                try
                {
                    var bundleDependents = AssetDatabase.GetAssetBundleDependencies(bundleNames[i], false);
                    queryer.AddBundleData(bundleNames[i], bundleDependents);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    continue;
                }
                if ((i + 1) % analysisBatchSize == 0)
                    await Task.Yield();
            }

            queryer.SeBundleCount();
            return queryer;
        }

        public static BundleReferenceQueryer GenerateQueryerByCurrentProjectSync()
        {
            var queryer = new BundleReferenceQueryer();
            var bundleNames = AssetDatabase.GetAllAssetBundleNames();
            for (var i = 0; i < bundleNames.Length; i++)
            {
                try
                {
                    var bundleDependents = AssetDatabase.GetAssetBundleDependencies(bundleNames[i], false);
                    queryer.AddBundleData(bundleNames[i], bundleDependents);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            queryer.SeBundleCount();
            return queryer;
        }

        public static BundleReferenceQueryer GenerateQueryerByExitedBuild()
        {
            if (BundleReferenceManifest.manifest == null)
            {
                EditorUtility.DisplayDialog("先调用BundleReferenceManifest.PrepareManifest()", "", "Ok");
                return null;
            }

            string[] bundleFiles = Directory.GetFiles(BundleReferenceManifest.bundleDirectory, "*.bundle", SearchOption.AllDirectories);
            BundleReferenceQueryer queryer = new BundleReferenceQueryer();
            for (var i = 0; i < bundleFiles.Length; i++)
            {
                var fileName = Path.GetFileNameWithoutExtension(bundleFiles[i]);
                var bundleReference = BundleReferenceManifest.manifest.GetDirectDependencies(bundleFiles[i]);
                queryer.AddBundleData(fileName, bundleReference ?? Array.Empty<string>());
            }
            queryer.SeBundleCount();
            return queryer;
        }

        public static string serializedAssetDirectory = "Analysis/";
        
        public static BundleReferenceQueryer GenerateQueryerBySerializedData(string assetPath)
        {
            if (!File.Exists(assetPath))
            {
                EditorUtility.DisplayDialog("序列化数据文件不存在", $"The file {assetPath} does not exist.", "OK");
                return null;
            }

            using var reader = new ReferenceReader();
            var queryer = new BundleReferenceQueryer();
            var datas = reader.Read<BundleReferenceInfo>(assetPath);
            foreach (var data in datas)
            {
                queryer.AddBundleData(data.bundleName, data.bundleDependent);
                queryer.SetBundleDefects(data.bundleName, data.defects?.ToList() ?? new List<string>());
            }
            queryer.SeBundleCount();
            return queryer;
        }

        public static void SaveSerializedDataFromQueryer(BundleReferenceQueryer queryer, string assetPath)
        {
            var datas = queryer.GetAllBundleData();
            using var writer = new ReferenceWriter();
            var serializableDatas = new List<BundleReferenceInfo>(datas.Count);
            foreach (var data in datas)
            {
                serializableDatas.Add(new BundleReferenceInfo
                {
                    bundleName = data.Key,
                    bundleDependent = data.Value.bundleDependent.ToArray(),
                    defects = data.Value.tags.ToArray()
                });
            }
            writer.Write(serializableDatas);
            writer.Flush(assetPath);
        }
    }
}