using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio
{
    public class AssetBundleConfigTool
    {
        [MenuItem("Build/AssetBundle/AssetBundleMap", false, 1)]
        public static void CreateAssetBundleConfig()
        {
            var buildPath = Path.Combine(Application.streamingAssetsPath,
                AssetsBundleBuildUtils.GetBuildFoldName(EditorUserBuildSettings.activeBuildTarget));
            if (!Directory.Exists(buildPath))
                return;
            string[] assetBuneleNames = AssetDatabase.GetAllAssetBundleNames();
            //创建数据资源文件
            //泛型是继承自ScriptableObject的类
            var assetData = new ScriptableAssetBundle();
            GetBundleAssetData(assetBuneleNames, assetData);
            //前一步创建的资源只是存在内存中，现在要把它保存到本地
            //通过编辑器API，创建一个数据资源文件，第二个参数为资源文件在Assets目录下的路径
            var folder = Path.Combine(Application.streamingAssetsPath, ConstSetting.BundleAssetConfigFolder);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var bytes = SerializeUtils.SerializeToBinary(assetData);
            var encryptData = EncryptUtils.AESEncrypt(bytes, ConstSetting.FileEncryptionKey);
            var path = Path.Combine(Application.streamingAssetsPath, ConstSetting.BundleAssetConfigFolder, ConstSetting.BundleAssetConfigName);
            File.WriteAllBytes(path, encryptData);

            //保存创建的资源
            AssetDatabase.SaveAssets();
            //刷新界面
            AssetDatabase.Refresh();
        }

        private static void GetBundleAssetData(string[] bundleNames, ScriptableAssetBundle assetData)
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
                
                // var bundle = AssetBundle.LoadFromFile(Path.Combine(foldPath, item));
                // if (string.IsNullOrEmpty(bundle.name))
                // {
                //     bundle.Unload(true);
                //     continue;
                // }
                //
                // var assets = bundle.GetAllAssetNames();
                foreach (var name in allAssetPaths)
                {
                    if (Path.GetExtension(name) == "shader") continue;
                    if (allAssets.Contains(name))
                    {
                        AssetLog.LogError($"Duplicate packaged assets: {name}");
                        continue;
                    }
                    assetData.source.Add(new ScriptableAssetBundleData()
                    {
                        // hashCode = name.GenHashCode(),
                        assetName = name,
                        assetBundle = bundleName
                    });
                    allAssets.Add(name);
                }
                // bundle.Unload(true);
            }

            AssetBundle.UnloadAllAssetBundles(true);
        }
    }
}