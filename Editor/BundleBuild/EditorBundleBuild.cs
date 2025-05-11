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
            AssetBundleConfigTool.CreateAssetBundleConfig();
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
            AssetBundleConfigTool.CreateAssetBundleConfig();
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