using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio
{
    public class AssetBundleMapTool
    {
        [MenuItem("Build/AssetBundle/AssetBundleMap", false, 1)]
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
            //保存创建的资源
            AssetDatabase.SaveAssets();
            //刷新界面
            AssetDatabase.Refresh();
        }

        private static IEnumerable<ScriptableAssetBundleData> GetBundleAssetData(string[] bundleNames)
        {
            AssetBundle.UnloadAllAssetBundles(true);
            var allAssets = new HashSet<string>();
            foreach (var bundleName in bundleNames)
            {
                var directAssetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);
                // 用来存储所有处理过的资源路径
                HashSet<string> allAssetPaths = new HashSet<string>(directAssetPaths);
                
                foreach (var path in directAssetPaths)
                {
                    // 检查每个直接标记资源的依赖关系
                    string[] dependencies = AssetDatabase.GetDependencies(path, true); // true: 包含所有依赖（间接和直接）

                    foreach (string dependency in dependencies)
                    {
                        if (!dependency.StartsWith("Assets/")) continue;
                        var extension = Path.GetExtension(dependency);
                        
                        if (extension == ".cs" || extension == ".dll") continue;
                        // 过滤掉属于其他分包的资源
                        string assetBundleName = AssetDatabase.GetImplicitAssetBundleName(dependency);
                        if (string.IsNullOrEmpty(assetBundleName))
                        {
                            allAssetPaths.Add(dependency);
                        }
                    }
                }
                foreach (var name in allAssetPaths)
                {
                    if (Path.GetExtension(name) == "shader") continue;
                    if (allAssets.Contains(name))
                    {
                        AssetLog.LogError($"Duplicate packaged assets: {name}");
                        continue;
                    }
                    allAssets.Add(name);

                    yield return new ScriptableAssetBundleData()
                    {
                        // hashCode = name.GenHashCode(),
                        assetName = name,
                        assetBundle = bundleName
                    };
                }
                // bundle.Unload(true);
            }

            AssetBundle.UnloadAllAssetBundles(true);
        }
    }
}