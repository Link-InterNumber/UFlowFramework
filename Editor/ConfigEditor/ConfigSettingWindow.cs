#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio.Editor
{
    public class ConfigSettingWindow : EditorWindow
    {
        public static void OpenEditorSettingWindow()
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

            EditorUIStyle.DrawWindowBackground(new Rect(Vector2.zero, GUILayoutUtility.GetRect(0f, 0f).size));
            EditorUIStyle.DrawImguiHeader("Configuration Generator", "Configure source and output paths, then generate configuration content.");

            using (new EditorGUILayout.VerticalScope(EditorUIStyle.PanelBox))
            {
                EditorGUILayout.LabelField("Paths", EditorUIStyle.SectionTitle);
                _save.excelPath = EditorGUILayout.TextField("Excel folder", _save.excelPath);
                _save.csFilePath = EditorGUILayout.TextField("C# output folder", _save.csFilePath);
            }
            // _save.localizationCSVPath = EditorGUILayout.TextField("Output CSV File Path", _save.localizationCSVPath);

            GUILayout.Space(EditorUIStyle.ContentSpacing);
            if (GUILayout.Button("Save Settings", EditorUIStyle.PrimaryButton))
            {
                SaveData();
            }

            GUILayout.Space(EditorUIStyle.ContentSpacing);
            using (new EditorGUILayout.VerticalScope(EditorUIStyle.PanelBox))
            {
                EditorGUILayout.LabelField("Generation", EditorUIStyle.SectionTitle);
                if (GUILayout.Button("Create C# Files", GUILayout.Height(EditorUIStyle.SecondaryButtonHeight)))
                {
                    SaveData();
                    ConfigMenu.CreateCsFiles();
                    _save.excelPath = EditorSaveUtils.GetEditorPref(SaveKey.excelPath, string.Empty);
                }

                if (GUILayout.Button("Create Config Assets", GUILayout.Height(EditorUIStyle.SecondaryButtonHeight)))
                {
                    SaveData();
                    ConfigMenu.CreateConfigAsset();
                }

                if (GUILayout.Button("Delete Config Assets", EditorUIStyle.DestructiveButton))
                {
                    ConfigMenu.DeleteConfigAsset();
                }
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

            GUILayout.Space(EditorUIStyle.ContentSpacing);
            using (new EditorGUILayout.VerticalScope(EditorUIStyle.PanelBox))
            {
                GUILayout.Label("Prefab Text Export", EditorUIStyle.SectionTitle);
                EditorGUILayout.LabelField("Export TextEx components from prefabs to localization CSV.", EditorUIStyle.MutedLabel);
                _save.UIPrefabPath = EditorGUILayout.TextField("Prefab folder", _save.UIPrefabPath);

                if (GUILayout.Button("Export Prefab Text", EditorUIStyle.PrimaryButton))
                {
                    if (string.IsNullOrEmpty(_save.UIPrefabPath))
                    {
                        EditorUtility.DisplayDialog("Error", "Please specify a valid folder path.", "OK");
                        return;
                    }
                    EditorSaveUtils.SetEditorPref(SaveKey.UIPrefabPath, _save.UIPrefabPath);
                    UnityLocalizationWriter.CollectTextsFromGameObject(_save.UIPrefabPath);
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