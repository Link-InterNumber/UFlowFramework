#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using System;
using UnityEngine;

namespace PowerCellStudio
{
    internal class AddressableBuilder
    {
        public static string build_script
            = "Assets/AddressableAssetsData/DataBuilders/BuildScriptPackedMode.asset";

        public static string settings_asset
            = "Assets/AddressableAssetsData/AddressableAssetSettings.asset";

        public static string profile_name = "Default";
        private static AddressableAssetSettings settings;

        static void GetSettingsObject(string settingsAsset)
        {
            // This step is optional, you can also use the default settings:
            //settings = AddressableAssetSettingsDefaultObject.Settings;
            settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(settingsAsset) as AddressableAssetSettings;
            if (settings == null)
                Debug.LogError($"{settingsAsset} couldn't be found or isn't a settings object.");
        }

        static void SetProfile(string profile)
        {
            string profileId = settings.profileSettings.GetProfileId(profile);
            if (String.IsNullOrEmpty(profileId))
                LinkLog.LogWarning($"Couldn't find a profile named, {profile}, using current profile instead.");
            else
                settings.activeProfileId = profileId;
        }

        static void SetBuilder(IDataBuilder builder)
        {
            int index = settings.DataBuilders.IndexOf((ScriptableObject) builder);

            if (index > 0)
                settings.ActivePlayerDataBuilderIndex = index;
            else
                LinkLog.LogWarning($"{builder} must be added to the DataBuilders list before it can be made active. Using last run builder instead.");
        }

        static bool BuildAddressableContent()
        {
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            bool success = string.IsNullOrEmpty(result.Error);
            if (!success)
                LinkLog.LogError("Addressables build error encountered: " + result.Error);
            return success;
        }

        [MenuItem("Build/Addressables/Build Addressables only", false, 1000)]
        public static bool BuildAddressables()
        {
            if(settings == null) GetSettingsObject(settings_asset);
            SetProfile(profile_name);
            IDataBuilder builderScript = AssetDatabase.LoadAssetAtPath<ScriptableObject>(build_script) as IDataBuilder;

            if (builderScript == null)
            {
                Debug.LogError(build_script + " couldn't be found or isn't a build script.");
                return false;
            }
            SetBuilder(builderScript);
            return BuildAddressableContent();
        }

        public static bool IsBuildOnPlayerBuild()
        {
            if(settings == null) GetSettingsObject(settings_asset);
            return settings.BuildAddressablesWithPlayerBuild != AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
        }
    }
}

#endif