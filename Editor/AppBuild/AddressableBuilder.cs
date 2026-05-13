#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using System;
using UnityEngine;
using static PowerCellStudio.ConfigSettingItem;

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


        static void SetConfigFolderAddressableGroup()
        {
            var configFolder = EditorSaveUtils.GetEditorPref(SaveKey.assetFilePath, "Assets/ConfigAsset/");
            if (string.IsNullOrEmpty(configFolder))
            {
                LinkLog.LogWarning("Config folder path is empty, skipped assigning Addressables group.");
                return;
            }

            configFolder = configFolder.Replace('\\', '/');
            if (configFolder[configFolder.Length - 1] == '/')
            {
                configFolder = configFolder.Substring(0, configFolder.Length - 1);
            }

            string guid = AssetDatabase.AssetPathToGUID(configFolder);
            if (string.IsNullOrEmpty(guid))
            {
                LinkLog.LogWarning($"Config folder '{configFolder}' could not be found, skipped assigning Addressables group.");
                return;
            }

            var group = settings.FindGroup(ConfigManager.assetLabel);
            if (group == null)
            {
                var defaultSchemas = settings.DefaultGroup != null ? settings.DefaultGroup.Schemas : null;
                group = settings.CreateGroup(ConfigManager.assetLabel, false, false, false, defaultSchemas);
                if (group == null)
                {
                    LinkLog.LogError($"Failed to create Addressables group '{ConfigManager.assetLabel}'.");
                    return;
                }
            }

            var entry = settings.CreateOrMoveEntry(guid, group);
            if (entry == null)
            {
                LinkLog.LogError($"Failed to assign config folder '{configFolder}' to Addressables group '{ConfigManager.assetLabel}'.");
                return;
            }

            entry.address = configFolder;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

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

        [MenuItem("Build/Addressable/Build Addressable Bundle only", false, 1000)]
        public static bool BuildAddressables()
        {
            if(settings == null) GetSettingsObject(settings_asset);
            SetConfigFolderAddressableGroup();
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