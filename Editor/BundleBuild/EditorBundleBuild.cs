#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System;
using UnityEditor;

namespace PowerCellStudio
{
    public class EditorBundleBuild
    {
        [MenuItem("Build/AssetBundle/CreateMyData", false, 1)]
        public static void SaveAssetBundleData()
        {
            if (!Directory.Exists(Application.streamingAssetsPath))
                return;
            // get manifest
            var manifestList = Directory.GetFiles(Application.streamingAssetsPath)
                .Where(o => Path.GetExtension(o) == ".manifest")
                .Select(o => Path.GetFileNameWithoutExtension(o))
                .ToList();

            //创建数据资源文件
            //泛型是继承自ScriptableObject的类
            var assetData = ScriptableObject.CreateInstance<ScriptableAssetBundle>();
            GetBundleAssetData(manifestList, assetData);
            //前一步创建的资源只是存在内存中，现在要把它保存到本地
            //通过编辑器API，创建一个数据资源文件，第二个参数为资源文件在Assets目录下的路径
            var folder = Path.Combine("Assets", "Resources", ConstSetting.BundleAssetConfigFolder);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var path = Path.Combine("Assets","Resources", ConstSetting.BundleAssetConfigFolder, ConstSetting.BundleAssetConfigName);
            AssetDatabase.CreateAsset(assetData, path);
            //保存创建的资源
            AssetDatabase.SaveAssets();
            //刷新界面
            AssetDatabase.Refresh();
        }

        private static void GetBundleAssetData(List<string> manifestList, ScriptableAssetBundle assetData)
        {
            AssetBundle.UnloadAllAssetBundles(true);
            foreach (var item in manifestList)
            {
                var bundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, item));
                if (string.IsNullOrEmpty(bundle.name))
                {
                    bundle.Unload(true);
                    continue;
                }

                var assets = bundle.GetAllAssetNames();
                foreach (var name in assets)
                {
                    assetData.source.Add(new ScriptableAssetBundleData()
                    {
                        hashCode = name.GenHashCode(),
                        assetName = name,
                        assetBundle = bundle.name
                    });
                }

                bundle.Unload(true);
            }

            AssetBundle.UnloadAllAssetBundles(true);
        }

        [MenuItem("Build/AssetBundle/Build AsserBundle", false, 2)]
        public static void BuildAsserBundleOnly()
        {
            ConfigMenu.CreateConfigAssetByForce();
            AssetDatabase.DeleteAsset("StreamingAssets");
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }
            BuildPipeline.BuildAssetBundles(Application.streamingAssetsPath, BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle, EditorUserBuildSettings.activeBuildTarget);
            SaveAssetBundleData();
        }
        
        [MenuItem("Build/AssetBundle/Build AsserBundle Incrementally", false, 2)]
        public static void BuildAsserBundleIncrementally()
        {
            ConfigMenu.CreateConfigAssetByForce();
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }
            BuildPipeline.BuildAssetBundles(Application.streamingAssetsPath, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
            SaveAssetBundleData();
        }

        [MenuItem("Build/AssetBundle/Build Play", false, 4)]
        public static void BuildPlayApp()
        {
            BuildAsserBundleOnly();
            
            var options = new BuildPlayerOptions();
            // options.locationPathName = Environment.CurrentDirectory;
            BuildPlayerOptions playerSettings = BuildPlayerWindow.DefaultBuildMethods.GetBuildPlayerOptions(options);
            var buildPath = Path.Combine(Environment.CurrentDirectory, $"Build/{playerSettings.target}/");
            if (!Directory.Exists(buildPath))
                Directory.CreateDirectory(buildPath);
            playerSettings.locationPathName = Path.Combine(buildPath, PlayerBuilder.GetBuildTargetName(playerSettings.target));
            playerSettings.scenes = EditorBuildSettings.scenes.Where(o => o.enabled).Select(o => o.path).ToArray();
            playerSettings.options = BuildOptions.None;
            playerSettings.options |= BuildOptions.CompressWithLz4;
            PlayerSettings.SetScriptingBackend(playerSettings.targetGroup, ScriptingImplementation.IL2CPP);
            BuildPipeline.BuildPlayer(playerSettings);
            EditorUtility.RevealInFinder($"Build/{playerSettings.target}/");
        }
    }
}
#endif