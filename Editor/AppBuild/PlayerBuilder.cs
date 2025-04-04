#if UNITY_EDITOR

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio
{
    public class PlayerBuilder
    {
        const string androidKeystorePass = "PowerCellStudio_keke";
        const string androidKeyaliasName = "PowerCellStudio";
        const string androidKeyaliasPass = "PowerCellStudio_keke";
        private const string GameName = "TestGame";
        
        public static string GetBuildTargetName(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.Android:
                    return $"{GameName}.apk";
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return $"{GameName}.exe";
                case BuildTarget.StandaloneOSXIntel:
                case BuildTarget.StandaloneOSXIntel64:
                case BuildTarget.StandaloneOSX:
                    return $"{GameName}.app";
                case BuildTarget.iOS:
                    return $"{GameName}-local";
                case BuildTarget.WebGL:
                    return $"{GameName}";
                default:
                    Debug.Log("Target not implemented.");
                    return null;
            }
        }

        [MenuItem(@"Build/Addressable/Default Build", false, 1001)]
        public static void DefaultPlayerBuilder()
        {
            ConfigMenu.CreateConfigAssetByForce();
            AssetDatabase.DeleteAsset("Assets/StreamingAssets");
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }
            if (!AddressableBuilder.IsBuildOnPlayerBuild())
            {
                var result =  AddressableBuilder.BuildAddressables();
                if (!result) return;
            }
            var options = new BuildPlayerOptions();
            options.options = BuildOptions.None;
            // options.locationPathName = Environment.CurrentDirectory;
            BuildPlayerOptions playerSettings = BuildPlayerWindow.DefaultBuildMethods.GetBuildPlayerOptions(options);
            var path = Environment.CurrentDirectory + $"/Build/{playerSettings.target}/";
            playerSettings.locationPathName = path + GetBuildTargetName(playerSettings.target);
            playerSettings.options |= BuildOptions.CompressWithLz4;
            PlayerSettings.SetScriptingBackend(playerSettings.targetGroup , ScriptingImplementation.IL2CPP);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            BuildPipeline.BuildPlayer(playerSettings);
            EditorUtility.RevealInFinder($"Build/{playerSettings.target}/");
        }
        
        [MenuItem(@"Build/Addressable/Window Build", false, 1002)]
        public static void BuildWindowAssets()
        {
            ConfigMenu.CreateConfigAssetByForce();
            AssetDatabase.DeleteAsset("Assets/StreamingAssets");
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }
            if (!AddressableBuilder.IsBuildOnPlayerBuild())
            {
                var result =  AddressableBuilder.BuildAddressables();
                if (!result) return;
            }
            var path = Path.Combine(Environment.CurrentDirectory, "Build/StandaloneWindows/");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            BuildPlayerOptions playerSettings = new BuildPlayerOptions();
            playerSettings.locationPathName = Path.Combine(path, GetBuildTargetName(BuildTarget.StandaloneWindows));
            playerSettings.scenes = EditorBuildSettings.scenes.Where(o => o.enabled).Select(o => o.path).ToArray();
            playerSettings.targetGroup = BuildTargetGroup.Standalone;
            playerSettings.target = BuildTarget.StandaloneWindows;
            playerSettings.options |= BuildOptions.CompressWithLz4;
            PlayerSettings.SetScriptingBackend(playerSettings.targetGroup , ScriptingImplementation.IL2CPP);
            BuildPipeline.BuildPlayer(playerSettings);
            EditorUtility.RevealInFinder("Build/StandaloneWindows/");
        }

        [MenuItem(@"Build/Addressable/Andriod Build", false, 1003)]
        public static void BuildAndroidAssets()
        {
            ConfigMenu.CreateConfigAssetByForce();
            AssetDatabase.DeleteAsset("Assets/StreamingAssets");
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }
            if (!File.Exists(PlayerSettings.Android.keystoreName))
            {
                var keyPath = EditorUtility.OpenFolderPanel("Select the folder of excel files", Environment.CurrentDirectory, "");
                if(string.IsNullOrEmpty(keyPath)) return;
                PlayerSettings.Android.keystoreName = keyPath;
            }
            PlayerSettings.Android.keystorePass = androidKeystorePass;
            PlayerSettings.Android.keyaliasName = androidKeyaliasName;
            PlayerSettings.Android.keyaliasPass = androidKeyaliasPass;
            if (!AddressableBuilder.IsBuildOnPlayerBuild())
            {
                var result =  AddressableBuilder.BuildAddressables();
                if (!result) return;
            }
            var path = Path.Combine(Environment.CurrentDirectory, "Build/Android/");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            BuildPlayerOptions playerSettings = new BuildPlayerOptions();
            playerSettings.locationPathName = Path.Combine(path, GetBuildTargetName(BuildTarget.Android));
            playerSettings.scenes = EditorBuildSettings.scenes.Where(o => o.enabled).Select(o => o.path).ToArray();
            playerSettings.targetGroup = BuildTargetGroup.Android;
            playerSettings.target = BuildTarget.Android;
            playerSettings.options |= BuildOptions.CompressWithLz4;
            PlayerSettings.SetScriptingBackend(playerSettings.targetGroup , ScriptingImplementation.IL2CPP);

            // playerSettings.locationPathName = path + GetBuildTargetName(playerSettings.target);
            BuildPipeline.BuildPlayer(playerSettings);
            EditorUtility.RevealInFinder("Build/Andriod/");
        }
        
        [MenuItem(@"Build/Addressable/WebGl Build", false, 1004)]
        public static void BuildWebGlAssets()
        {
            ConfigMenu.CreateConfigAssetByForce();
            AssetDatabase.DeleteAsset("Assets/StreamingAssets");
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }
            if (!AddressableBuilder.IsBuildOnPlayerBuild())
            {
                var result =  AddressableBuilder.BuildAddressables();
                if (!result) return;
            }
            var path = Path.Combine(Environment.CurrentDirectory, "Build/WebGL/");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            BuildPlayerOptions playerSettings = new BuildPlayerOptions();
            playerSettings.locationPathName = Path.Combine(path, GetBuildTargetName(BuildTarget.WebGL));
            playerSettings.scenes = EditorBuildSettings.scenes.Where(o => o.enabled).Select(o => o.path).ToArray();
            playerSettings.targetGroup = BuildTargetGroup.WebGL;
            playerSettings.target = BuildTarget.WebGL;
            // playerSettings.options |= BuildOptions.CompressWithLz4;
            PlayerSettings.SetScriptingBackend(playerSettings.targetGroup , ScriptingImplementation.IL2CPP);

            // playerSettings.locationPathName = path + GetBuildTargetName(playerSettings.target);
            BuildPipeline.BuildPlayer(playerSettings);
            EditorUtility.RevealInFinder("Build/WebGl/");
        }

        [MenuItem(@"Build/Addressable/Switch Build", false, 1005)]
        public static void BuildSwitchAssets()
        {
            ConfigMenu.CreateConfigAssetByForce();
            AssetDatabase.DeleteAsset("Assets/StreamingAssets");
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }
            if (!AddressableBuilder.IsBuildOnPlayerBuild())
            {
                var result =  AddressableBuilder.BuildAddressables();
                if (!result) return;
            }
            var path = Path.Combine(Environment.CurrentDirectory, "Build/Switch/");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            BuildPlayerOptions playerSettings = new BuildPlayerOptions();
            playerSettings.locationPathName = Path.Combine(path, GetBuildTargetName(BuildTarget.Switch));
            playerSettings.scenes = EditorBuildSettings.scenes.Where(o => o.enabled).Select(o => o.path).ToArray();
            playerSettings.targetGroup = BuildTargetGroup.Switch;
            playerSettings.target = BuildTarget.Switch;
            playerSettings.options |= BuildOptions.CompressWithLz4;
            PlayerSettings.SetScriptingBackend(playerSettings.targetGroup , ScriptingImplementation.IL2CPP);

            BuildPipeline.BuildPlayer(playerSettings);
            EditorUtility.RevealInFinder("Build/Switch/");
        }
    }
}
#endif