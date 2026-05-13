#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System;
using UnityEditor;
using static PowerCellStudio.ConfigSettingItem;

namespace PowerCellStudio
{
    public class EditorBundleBuild
    {
        private static void SetConfigFolderAssetBundleName()
        {
            var configFolder = EditorSaveUtils.GetEditorPref(SaveKey.assetFilePath, "Assets/ConfigAsset/");
            var importer = AssetImporter.GetAtPath(configFolder);
            if (importer != null)
            {
                importer.assetBundleName = ConfigManager.assetLabel;
                EditorUtility.SetDirty(importer);
            }
        }

        [MenuItem("Build/AssetBundle/Build AssetBundle", false, 2)]
        public static void BuildAsserBundleOnly()
        {
            SetConfigFolderAssetBundleName();
            ConfigMenu.CreateConfigAssetByForce();
            var buildPath = Path.Combine(Application.streamingAssetsPath,
                AssetsBundleBuildUtils.GetBuildFoldName(EditorUserBuildSettings.activeBuildTarget));
            AssetDatabase.DeleteAsset(buildPath);
            if (!Directory.Exists(buildPath))
            {
                Directory.CreateDirectory(buildPath);
            }
            BuildPipeline.BuildAssetBundles(buildPath, 
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle, 
                EditorUserBuildSettings.activeBuildTarget);
            AssetBundleMapTool.CreateAssetBundleMap();
            GenerateRemoteManifestWindow.ShowWindowWithHandle(()=> Debug.Log("Build AsserBundle Successfully!"));
        }

        private static void BuildAsserBundle(bool resetRemoteManifest, Action onRemoteManifestGenerated)
        {
            SetConfigFolderAssetBundleName();
            ConfigMenu.CreateConfigAssetByForce();
            var buildPath = Path.Combine(Application.streamingAssetsPath,
                AssetsBundleBuildUtils.GetBuildFoldName(EditorUserBuildSettings.activeBuildTarget));
            AssetDatabase.DeleteAsset(buildPath);
            if (!Directory.Exists(buildPath))
            {
                Directory.CreateDirectory(buildPath);
            }
            BuildPipeline.BuildAssetBundles(buildPath, 
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle, 
                EditorUserBuildSettings.activeBuildTarget);
            AssetBundleMapTool.CreateAssetBundleMap();
            if (resetRemoteManifest) 
                GenerateRemoteManifestWindow.ShowWindowWithHandle(onRemoteManifestGenerated);
            else 
                onRemoteManifestGenerated?.Invoke();
        }
        
        [MenuItem("Build/AssetBundle/Build AssetBundle Incrementally", false, 2)]
        public static void BuildAsserBundleIncrementally()
        {
            SetConfigFolderAssetBundleName();
            ConfigMenu.CreateConfigAssetByForce();
            var buildPath = Path.Combine(Application.streamingAssetsPath,
                AssetsBundleBuildUtils.GetBuildFoldName(EditorUserBuildSettings.activeBuildTarget));
            AssetDatabase.DeleteAsset(buildPath);
            if (!Directory.Exists(buildPath))
            {
                Directory.CreateDirectory(buildPath);
            }
            BuildPipeline.BuildAssetBundles(buildPath,
                BuildAssetBundleOptions.ChunkBasedCompression,
                EditorUserBuildSettings.activeBuildTarget);
            AssetBundleMapTool.CreateAssetBundleMap();
            GenerateRemoteManifestWindow.ShowWindowWithHandle(()=> Debug.Log("Build AsserBundle Successfully!"));
        }

        [MenuItem("Build/AssetBundle/Build Play", false, 4)]
        public static void BuildPlayApp()
        {
            ConfirmEditorWindow.ShowWindow(() =>
                {
                    BuildAsserBundle(true, BuildPlayAppOnly);
                },
                () =>
                {
                    BuildAsserBundle(false, BuildPlayAppOnly);
                },
                "Build AsserBundle",
                "需要重新设置远程分包配置吗？\nNeed to reset the [Remote Manifest]?");
        }
        
        [MenuItem("Build/AssetBundle/Build Play Only", false, 5)]
        public static void BuildPlayAppOnly()
        {
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