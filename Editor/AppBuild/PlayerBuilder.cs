#if UNITY_EDITOR

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio.Editor
{
    public class PlayerBuilder
    {
        private const string BuildNameMissingTitle = "Build Aborted";
        private const string BuildNameMissingMessage = "未配置程序名，请先在 Project Settings > Player > Product Name 中设置程序名后再构建。";
        private const string BuildNameInvalidMessage = "Product Name 不能作为文件名使用，请修改 Project Settings > Player > Product Name 后再构建。";
        private const string AndroidKeystoreMissingTitle = "Android Build Aborted";
        private const string AndroidKeystoreMissingMessage = "已启用 Custom Keystore，但 Project Settings > Player > Publishing Settings 中的 Keystore 路径不存在或未配置。";
        private const string AndroidKeystoreCredentialMissingMessage = "已启用 Custom Keystore，但 Keystore Password / Key Alias Name / Key Alias Password 未完整配置。";
        
        public static string GetBuildTargetName(BuildTarget target)
        {
            if (!TryGetBuildProductName(out var productName, false))
            {
                return null;
            }

            var dateStamp = DateTime.Now.ToString("yyyyMMddHHmm");
            switch (target)
            {
                case BuildTarget.Android:
                    return $"{productName}_{dateStamp}.apk";
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return $"{productName}_{dateStamp}.exe";
                case BuildTarget.StandaloneOSXIntel:
                case BuildTarget.StandaloneOSXIntel64:
                case BuildTarget.StandaloneOSX:
                    return $"{productName}_{dateStamp}.app";
                case BuildTarget.iOS:
                    return $"{productName}_{dateStamp}-local";
                case BuildTarget.WebGL:
                    return $"{productName}_{dateStamp}";
                default:
                    Debug.Log("Target not implemented.");
                    return null;
            }
        }

        public static bool ValidateBuildConfiguration(BuildTarget target)
        {
            if (!TryGetBuildProductName(out _, true))
            {
                return false;
            }

            if (target != BuildTarget.Android || !PlayerSettings.Android.useCustomKeystore)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(PlayerSettings.Android.keystoreName) || !File.Exists(PlayerSettings.Android.keystoreName))
            {
                EditorUtility.DisplayDialog(AndroidKeystoreMissingTitle, AndroidKeystoreMissingMessage, "OK");
                return false;
            }

            if (string.IsNullOrWhiteSpace(PlayerSettings.Android.keystorePass)
                || string.IsNullOrWhiteSpace(PlayerSettings.Android.keyaliasName)
                || string.IsNullOrWhiteSpace(PlayerSettings.Android.keyaliasPass))
            {
                EditorUtility.DisplayDialog(AndroidKeystoreMissingTitle, AndroidKeystoreCredentialMissingMessage, "OK");
                return false;
            }

            return true;
        }

        private static bool TryGetBuildProductName(out string productName, bool showDialog)
        {
            productName = PlayerSettings.productName?.Trim();
            if (string.IsNullOrEmpty(productName))
            {
                if (showDialog)
                {
                    EditorUtility.DisplayDialog(BuildNameMissingTitle, BuildNameMissingMessage, "OK");
                }

                return false;
            }

            var invalidFileNameChars = Path.GetInvalidFileNameChars();
            productName = new string(productName.Where(ch => !invalidFileNameChars.Contains(ch)).ToArray()).Trim();
            if (!string.IsNullOrEmpty(productName))
            {
                return true;
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(BuildNameMissingTitle, BuildNameInvalidMessage, "OK");
            }

            return false;
        }

        private static bool PrepareBuildAssets()
        {
            ConfigMenu.CreateConfigAssetByForce();
            if (AddressableBuilder.IsBuildOnPlayerBuild())
            {
                return true;
            }

            return AddressableBuilder.BuildAddressables();
        }

        [MenuItem(@"Build/Addressable/Default Build", false, 1001)]
        public static void DefaultPlayerBuilder()
        {
            var options = new BuildPlayerOptions();
            options.options = BuildOptions.None;
            BuildPlayerOptions playerSettings = BuildPlayerWindow.DefaultBuildMethods.GetBuildPlayerOptions(options);
            if (!ValidateBuildConfiguration(playerSettings.target))
            {
                return;
            }

            if (!PrepareBuildAssets()) return;

            var buildTargetName = GetBuildTargetName(playerSettings.target);
            if (string.IsNullOrEmpty(buildTargetName))
            {
                return;
            }

            var path = Path.Combine(Environment.CurrentDirectory, $"Build/{playerSettings.target}/");
            playerSettings.locationPathName = Path.Combine(path, buildTargetName);
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
            if (!ValidateBuildConfiguration(BuildTarget.StandaloneWindows))
            {
                return;
            }

            if (!PrepareBuildAssets())
            {
                return;
            }

            var path = Path.Combine(Environment.CurrentDirectory, "Build/StandaloneWindows/");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            var buildTargetName = GetBuildTargetName(BuildTarget.StandaloneWindows);
            if (string.IsNullOrEmpty(buildTargetName))
            {
                return;
            }
            BuildPlayerOptions playerSettings = new BuildPlayerOptions();
            playerSettings.locationPathName = Path.Combine(path, buildTargetName);
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
            if (!ValidateBuildConfiguration(BuildTarget.Android))
            {
                return;
            }

            if (!PrepareBuildAssets())
            {
                return;
            }

            var path = Path.Combine(Environment.CurrentDirectory, "Build/Android/");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            var buildTargetName = GetBuildTargetName(BuildTarget.Android);
            if (string.IsNullOrEmpty(buildTargetName))
            {
                return;
            }
            BuildPlayerOptions playerSettings = new BuildPlayerOptions();
            playerSettings.locationPathName = Path.Combine(path, buildTargetName);
            playerSettings.scenes = EditorBuildSettings.scenes.Where(o => o.enabled).Select(o => o.path).ToArray();
            playerSettings.targetGroup = BuildTargetGroup.Android;
            playerSettings.target = BuildTarget.Android;
            playerSettings.options |= BuildOptions.CompressWithLz4;
            PlayerSettings.SetScriptingBackend(playerSettings.targetGroup , ScriptingImplementation.IL2CPP);
            
            // playerSettings.locationPathName = path + GetBuildTargetName(playerSettings.target);
            BuildPipeline.BuildPlayer(playerSettings);
            EditorUtility.RevealInFinder("Build/Android/");
        }
        
        [MenuItem(@"Build/Addressable/WebGl Build", false, 1004)]
        public static void BuildWebGlAssets()
        {
            if (!ValidateBuildConfiguration(BuildTarget.WebGL))
            {
                return;
            }

            if (!PrepareBuildAssets())
            {
                return;
            }

            var path = Path.Combine(Environment.CurrentDirectory, "Build/WebGL/");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            var buildTargetName = GetBuildTargetName(BuildTarget.WebGL);
            if (string.IsNullOrEmpty(buildTargetName))
            {
                return;
            }
            BuildPlayerOptions playerSettings = new BuildPlayerOptions();
            playerSettings.locationPathName = Path.Combine(path, buildTargetName);
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
            if (!ValidateBuildConfiguration(BuildTarget.Switch))
            {
                return;
            }

            if (!PrepareBuildAssets())
            {
                return;
            }

            var path = Path.Combine(Environment.CurrentDirectory, "Build/Switch/");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            var buildTargetName = GetBuildTargetName(BuildTarget.Switch);
            if (string.IsNullOrEmpty(buildTargetName))
            {
                return;
            }
            BuildPlayerOptions playerSettings = new BuildPlayerOptions();
            playerSettings.locationPathName = Path.Combine(path, buildTargetName);
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