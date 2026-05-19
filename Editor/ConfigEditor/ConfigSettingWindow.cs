#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio
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

    internal sealed class ConfigSettingLogic
    {
        private sealed class ConfigSettingSave
        {
            public string excelPath;
            public string csFilePath;
            public string assetFilePath;
            public string UIPrefabPath;
            public string localizationCSVPath;
        }

        public static class SaveKey
        {
            public static readonly string excelPath = "excelPath";
            public static readonly string csFilePath = "csFilePath";
            public static readonly string UIPrefabPath = "UIPrefabPath";
            public static readonly string localizationCSVPath = "localizationCSVPath";
        }

        private sealed class Mark
        {
            public bool has;
        }

        private const string DefaultCsPath = "Assets/ConfigScript/";
        private const string DefaultAssetPath = "Assets/StreamingAssets/ConfigAsset/";

        private ConfigSettingSave _save;

        public void Initialize()
        {
            _save = new ConfigSettingSave();
            var defaultExcelPath = Path.Combine(Environment.CurrentDirectory, "ExcelFiles");
            var defaultLocalCsvPath = Path.Combine(defaultExcelPath, "Localization");
            _save.excelPath = EditorSaveUtils.GetEditorPref(SaveKey.excelPath, defaultExcelPath);
            _save.csFilePath = EditorSaveUtils.GetEditorPref(SaveKey.csFilePath, DefaultCsPath);
            _save.assetFilePath = DefaultAssetPath;
            _save.UIPrefabPath = EditorSaveUtils.GetEditorPref(SaveKey.UIPrefabPath, string.Empty);
            _save.localizationCSVPath = EditorSaveUtils.GetEditorPref(SaveKey.localizationCSVPath, defaultLocalCsvPath);
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
            EditorGUILayout.LabelField("asset file Path:", _save.assetFilePath);
            _save.localizationCSVPath = EditorGUILayout.TextField("Output CSV File Path", _save.localizationCSVPath);

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
                ConfigMenu.CreateLocalizationCsv();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Delete Config Assets"))
            {
                ConfigMenu.DeleteConfigAsset();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Create Localization csv"))
            {
                ConfigMenu.CreateLocalizationCsv();
            }

            GUILayout.Space(10);
            GUILayout.Label("Export Text Components to CSV", EditorStyles.boldLabel);
            _save.UIPrefabPath = EditorGUILayout.TextField("Folder Path", _save.UIPrefabPath);

            if (GUILayout.Button("Export"))
            {
                if (string.IsNullOrEmpty(_save.UIPrefabPath))
                {
                    EditorUtility.DisplayDialog("Error", "Please specify a valid folder path.", "OK");
                    return;
                }

                if (string.IsNullOrEmpty(_save.localizationCSVPath))
                {
                    EditorUtility.DisplayDialog("Error", "Please specify a valid output file path.", "OK");
                    return;
                }

                ExportTextsToCSV(_save.UIPrefabPath, _save.localizationCSVPath);
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

            if (string.IsNullOrEmpty(_save.assetFilePath) || !Directory.Exists(_save.assetFilePath))
            {
                _save.assetFilePath = "Assets/Resources/";
            }

            if (_save.assetFilePath[_save.assetFilePath.Length - 1] != '/')
            {
                _save.assetFilePath += '/';
            }

            if (_save.csFilePath[_save.csFilePath.Length - 1] != '/')
            {
                _save.csFilePath += '/';
            }

            EditorSaveUtils.SetEditorPref(SaveKey.csFilePath, _save.csFilePath);
            EditorSaveUtils.SetEditorPref(SaveKey.UIPrefabPath, _save.UIPrefabPath);
            EditorSaveUtils.SetEditorPref(SaveKey.localizationCSVPath, _save.localizationCSVPath);
        }

        private void ExportTextsToCSV(string folderPath, string outputFilePath)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Path,Text");
            var mark = new Mark();
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                if (prefab != null)
                {
                    AddTextsFromGameObject(prefab.transform, string.Empty, stringBuilder, mark);
                    if (mark.has)
                    {
                        EditorUtility.SetDirty(prefab);
                    }
                }

                mark.has = false;
            }

            File.WriteAllText(outputFilePath, stringBuilder.ToString());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Export Complete", $"Exported text data to {outputFilePath}", "OK");
            System.Diagnostics.Process.Start(outputFilePath);
        }

        private void AddTextsFromGameObject(Transform transform, string parentPath, StringBuilder stringBuilder, Mark mark)
        {
            string currentPath = string.IsNullOrEmpty(parentPath) ? transform.name : $"{parentPath}_{transform.name}";

            var textComponent = transform.GetComponent<TextEx>();
            if (textComponent != null && textComponent.staticText)
            {
                mark.has = true;
                textComponent.localizationKey = currentPath;
                stringBuilder.AppendLine($"{currentPath},{textComponent.text}");
            }

            foreach (Transform child in transform)
            {
                AddTextsFromGameObject(child, currentPath, stringBuilder, mark);
            }
        }
    }
}
#endif