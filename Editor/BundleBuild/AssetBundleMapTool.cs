using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio.Editor
{
    public class AssetBundleMapTool
    {
        public static void CreateAssetBundleMap()
        {
            string[] assetBuneleNames = AssetDatabase.GetAllAssetBundleNames();
            // 获取资源路径和bundle映射数据
            var mapDataSource = GetBundleAssetData(assetBuneleNames);
            // 检查文件夹
            var folder = Path.Combine(Application.streamingAssetsPath, ConstSetting.BundleAssetConfigFolder);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            // Chunk化保存
            ChunkMaker.StreamWriteSync(folder, ConstSetting.BundleAssetConfigName, mapDataSource, data => data.assetName, 256);
        }

        private static IEnumerable<ScriptableAssetBundleData> GetBundleAssetData(string[] bundleNames)
        {
            var allAssets = new HashSet<string>();
            var duplicateAssets = new HashSet<string>();
            // 先收集所有包的资源路径，避免将后续包中的资源误判为未分包依赖。
            var assignedAssetPaths = new HashSet<string>();
            var assetPathsByBundle = new string[bundleNames.Length][];
            for (var i = 0; i < bundleNames.Length; i++)
            {
                var assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(bundleNames[i]);
                assetPathsByBundle[i] = assetPaths;
                assignedAssetPaths.UnionWith(assetPaths);
            }
            HashSet<string> allAssetPaths = new HashSet<string>();
            for (var i = 0; i < bundleNames.Length; i++)
            {
                var bundleName = bundleNames[i];
                var directAssetPaths = assetPathsByBundle[i];
                // 用来存储所有处理过的资源路径
                allAssetPaths.Clear();
                allAssetPaths.UnionWith(directAssetPaths);
                
                string[] dependencies = AssetDatabase.GetDependencies(directAssetPaths, true); 
                foreach (string dependency in dependencies)
                {
                    if (!dependency.StartsWith("Assets/")) continue;
                    var extension = Path.GetExtension(dependency);
                    
                    if (extension == ".cs" || extension == ".dll") continue;
                    // 已分包资源由各自的直接资源集合收录，仅补充未分包依赖。
                    if (!assignedAssetPaths.Contains(dependency))
                    {
                        allAssetPaths.Add(dependency);
                    }
                }
                
                foreach (var name in allAssetPaths)
                {
                    if (Path.GetExtension(name) == ".shader") continue;
                    if (!allAssets.Add(name))
                    {
                        duplicateAssets.Add(name);
                        continue;
                    }

                    yield return new ScriptableAssetBundleData()
                    {
                        // hashCode = name.GenHashCode(),
                        assetName = name,
                        assetBundle = bundleName
                    };
                }
            }

            if (duplicateAssets.Count <= 0) yield break;
            var sb = new StringBuilder();
            sb.AppendLine("Duplicate packaged assets:");  
            foreach (var duplicateAsset in duplicateAssets)
            {
                sb.AppendLine(duplicateAsset);
            }
            AssetLogger.LogError(sb.ToString());
        }
        
    }
}