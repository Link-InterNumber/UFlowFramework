#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio.Editor
{
    public class ConfigSettingWindow : EditorWindow
    {
        [MenuItem("Tools/Config/Config Setting Window", false, 99)]
        static void OpenEditorSettingWindow()
        {
            EditorWindow.GetWindow<ConfigSettingWindow>(false, "Config Setting Window", true).Show();
        }
        
        private readonly ConfigSettingLogic _logic = new ConfigSettingLogic();

        void OnEnable()
        {
            _logic.Initialize();
        }

        private void OnDisable()
        {
            _logic.Dispose();
        }
        
        void OnGUI()
        {
            _logic.OnGUI();
        }
    }

    public sealed class ConfigSettingLogic
    {
        private sealed class ConfigSettingSave
        {
            public string excelPath;
            public string csFilePath;
            public string UIPrefabPath;
            // public string localizationCSVPath;
        }

        public static class SaveKey
        {
            public static readonly string excelPath = "excelPath";
            public static readonly string csFilePath = "csFilePath";
            public static readonly string UIPrefabPath = "UIPrefabPath";
            // public static readonly string localizationCSVPath = "localizationCSVPath";
        }

        private sealed class TempMark
        {
            public bool has;
        }

        private const string DefaultCsPath = "Assets/ConfigScript/";
        // private const string DefaultAssetPath = "Assets/StreamingAssets/ConfigAsset/";
        public const string LocalizationFolderName = "Localization";

        private ConfigSettingSave _save;

        public void Initialize()
        {
            _save = new ConfigSettingSave();
            var defaultExcelPath = Path.Combine(Environment.CurrentDirectory, "ExcelFiles");
            var defaultLocalCsvPath = Path.Combine(defaultExcelPath, "Localization");
            _save.excelPath = EditorSaveUtils.GetEditorPref(SaveKey.excelPath, defaultExcelPath);
            _save.csFilePath = EditorSaveUtils.GetEditorPref(SaveKey.csFilePath, DefaultCsPath);
            _save.UIPrefabPath = EditorSaveUtils.GetEditorPref(SaveKey.UIPrefabPath, string.Empty);
            // _save.localizationCSVPath = EditorSaveUtils.GetEditorPref(SaveKey.localizationCSVPath, defaultLocalCsvPath);
        }

        public void Dispose()
        {
            _save = null;
        }

        public void OnGUI()
        {
            if (_save == null)
            {
                Initialize();
            }

            _save.excelPath = EditorGUILayout.TextField("excel file Path:", _save.excelPath);
            _save.csFilePath = EditorGUILayout.TextField("cs file Path:", _save.csFilePath);
            // _save.localizationCSVPath = EditorGUILayout.TextField("Output CSV File Path", _save.localizationCSVPath);

            GUILayout.Space(30);
            if (GUILayout.Button("Save Settings"))
            {
                SaveData();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Create Cs Files"))
            {
                SaveData();
                ConfigMenu.CreateCsFiles();
                _save.excelPath = EditorSaveUtils.GetEditorPref(SaveKey.excelPath, string.Empty);
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Create Config Assets"))
            {
                SaveData();
                ConfigMenu.CreateConfigAsset();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Delete Config Assets"))
            {
                ConfigMenu.DeleteConfigAsset();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Create Localization csv"))
            {
                UnityLocalizationCsvExporter.Export();
                var csvPath = Path.Combine(_save.excelPath, LocalizationFolderName)+"/";
                var fullPath = Path.GetFullPath(csvPath);
                if (File.Exists(fullPath) || Directory.Exists(fullPath))
                {
                    EditorUtility.RevealInFinder(fullPath);
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", $"Path not found:\n{fullPath}", "OK");
                }
            }

            GUILayout.Space(10);
            GUILayout.Label("Export Text components on prefab to CSV", EditorStyles.boldLabel);
            _save.UIPrefabPath = EditorGUILayout.TextField("Prefab Folder Path", _save.UIPrefabPath);

            if (GUILayout.Button("Export"))
            {
                if (string.IsNullOrEmpty(_save.UIPrefabPath))
                {
                    EditorUtility.DisplayDialog("Error", "Please specify a valid folder path.", "OK");
                    return;
                }
                UnityLocalizationWriter.CollectTextsFromGameObject(_save.UIPrefabPath);
                UnityLocalizationCsvExporter.Export();
                var csvPath = Path.Combine(_save.excelPath, LocalizationFolderName);
                var fullPath = Path.GetFullPath(csvPath);
                if (File.Exists(fullPath) || Directory.Exists(fullPath))
                {
                    EditorUtility.RevealInFinder(fullPath);
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", $"Path not found:\n{fullPath}", "OK");
                }
            }
        }

        public void SaveData()
        {
            if (_save == null)
            {
                Initialize();
            }

            if (string.IsNullOrEmpty(_save.excelPath) || !Directory.Exists(_save.excelPath))
            {
                _save.excelPath =
                    EditorUtility.OpenFolderPanel("Select the folder of excel files", Environment.CurrentDirectory, string.Empty);
            }

            EditorSaveUtils.SetEditorPref(SaveKey.excelPath, _save.excelPath);
            if (string.IsNullOrEmpty(_save.csFilePath) || !Directory.Exists(_save.csFilePath))
            {
                _save.csFilePath = DefaultCsPath;
            }

            if (_save.csFilePath[_save.csFilePath.Length - 1] != '/')
            {
                _save.csFilePath += '/';
            }

            EditorSaveUtils.SetEditorPref(SaveKey.csFilePath, _save.csFilePath);
            EditorSaveUtils.SetEditorPref(SaveKey.UIPrefabPath, _save.UIPrefabPath);
        }
    }
}
#endif