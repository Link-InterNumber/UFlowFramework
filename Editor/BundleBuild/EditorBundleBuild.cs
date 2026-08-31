#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEngine;
using System;
using UnityEditor;

namespace PowerCellStudio.Editor
{
    public class EditorBundleBuild
    {

        public static void BuildAsserBundleOnly()
        {
            ConfigMenu.CreateConfigAssetByForce();
            var buildPath = Path.Combine(Application.streamingAssetsPath,
                AssetsBundleBuildUtils.GetBuildFoldName(EditorUserBuildSettings.activeBuildTarget));
            Directory.Delete(buildPath, true);
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
            ConfigMenu.CreateConfigAssetByForce();
            var buildPath = Path.Combine(Application.streamingAssetsPath,
                AssetsBundleBuildUtils.GetBuildFoldName(EditorUserBuildSettings.activeBuildTarget));
            Directory.Delete(buildPath, true);
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
        
        public static void BuildAsserBundleIncrementally()
        {
            ConfigMenu.CreateConfigAssetByForce();
            var buildPath = Path.Combine(Application.streamingAssetsPath,
                AssetsBundleBuildUtils.GetBuildFoldName(EditorUserBuildSettings.activeBuildTarget));
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
        
        public static void BuildPlayAppOnly()
        {
            var options = new BuildPlayerOptions();
            // options.locationPathName = Environment.CurrentDirectory;
            BuildPlayerOptions playerSettings = BuildPlayerWindow.DefaultBuildMethods.GetBuildPlayerOptions(options);
            if (!PlayerBuilder.ValidateBuildConfiguration(playerSettings.target))
            {
                return;
            }

            var buildTargetName = PlayerBuilder.GetBuildTargetName(playerSettings.target);
            if (string.IsNullOrEmpty(buildTargetName))
            {
                return;
            }

            var buildPath = Path.Combine(Environment.CurrentDirectory, $"Build/{playerSettings.target}/");
            if (!Directory.Exists(buildPath))
                Directory.CreateDirectory(buildPath);
            playerSettings.locationPathName = Path.Combine(buildPath, buildTargetName);
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