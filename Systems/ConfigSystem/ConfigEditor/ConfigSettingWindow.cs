#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio
{
    public class ConfigSettingWindow : EditorWindow
    {
        public class ConfigSettingSave
        {
            public string excelPath;
            public string csFilePath;
            public string assetFilePath;
            public string UIPrefabPath;
            public string localizationCSVPath;
        }

        public class SaveKey
        {
            public static string excelPath = "excelPath";
            public static string csFilePath = "csFilePath";
            public static string assetFilePath = "assetFilePath";
            public static string UIPrefabPath = "UIPrefabPath";
            public static string localizationCSVPath = "localizationCSVPath";
        }
        
        [MenuItem("Tools/Config/Config Setting Window", false, 99)]
        static void OpenEditorSettingWindow()
        {
            EditorWindow.GetWindow<ConfigSettingWindow>(false, "Config Setting Window", true).Show();
        }
        
        private ConfigSettingSave _save;

        // private GUIStyle _guiStyle;

        void OnEnable()
        {
            _save = new ConfigSettingSave();
            var defaultExcelPath = Path.Combine(Environment.CurrentDirectory, "ExcelFiles");
            var defaultCsPath = "Assets/ConfigScript/";
            var defaultAssetPath = "Assets/Resources/";
            var defaultLocalCsvPath = Path.Combine(defaultExcelPath, "Localization");
            _save.excelPath = EditorPrefs.GetString(SaveKey.excelPath, defaultExcelPath);
            _save.csFilePath = EditorPrefs.GetString(SaveKey.csFilePath, defaultCsPath);
            _save.assetFilePath = EditorPrefs.GetString(SaveKey.assetFilePath, defaultAssetPath);
            _save.UIPrefabPath = EditorPrefs.GetString(SaveKey.UIPrefabPath, string.Empty);
            _save.localizationCSVPath = EditorPrefs.GetString(SaveKey.localizationCSVPath, defaultLocalCsvPath);
            //设置绘制按钮的格式
            // InitGuiStyle();
        }

        // private void InitGuiStyle()
        // {
        //     if(_guiStyle != null) return;
        //     _guiStyle = new GUIStyle(EditorStyles.miniButton);
        //     _guiStyle.fontSize = 15;
        //     _guiStyle.fixedHeight = 25;
        //     // _guiStyle.fixedWidth = 200;
        // }

        private void OnDisable()
        {
            _save = null;
            // _guiStyle = null;
            // _guiStyle = null;
        }
        
        void OnGUI()
        {
            _save.excelPath = EditorGUILayout.TextField("excel file Path:", _save.excelPath);
            _save.csFilePath = EditorGUILayout.TextField("cs file Path:", _save.csFilePath);
            _save.assetFilePath = EditorGUILayout.TextField("asset file Path:", _save.assetFilePath);
            _save.localizationCSVPath = EditorGUILayout.TextField("Output CSV File Path", _save.localizationCSVPath);
            // InitGuiStyle();
            GUILayout.Space(30);
            if (GUILayout.Button("Save Settings"))
            {
                SaveSettings();
            }
            GUILayout.Space(10);
            if (GUILayout.Button("Create Cs Files"))
            {
                SaveSettings();
                ConfigMenu.CreateCsFiles();
                _save.excelPath = EditorPrefs.GetString(SaveKey.excelPath);
            }
            GUILayout.Space(10);
            if (GUILayout.Button("Create Config Assets"))
            {
                SaveSettings();
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

        private class Mark
        {
            public bool has = false;
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
                    AddTextsFromGameObject(prefab.transform, "", stringBuilder, mark);
                    if (mark.has) EditorUtility.SetDirty(prefab);
                }
                mark.has = false;
            }
            mark = null;
            File.WriteAllText(outputFilePath, stringBuilder.ToString());            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Export Complete", $"Exported text data to {outputFilePath}", "OK");
            System.Diagnostics.Process.Start(outputFilePath);
        }

        private void AddTextsFromGameObject(Transform transform, string parentPath, StringBuilder stringBuilder, Mark mark)
        {
            // Prepare the path string
            string currentPath = string.IsNullOrEmpty(parentPath) ? transform.name : $"{parentPath}_{transform.name}";

            // Get Text component (you might consider using TryGetComponent for better performance in the latest Unity versions)
            var textComponent = transform.GetComponent<TextEx>();
            if (textComponent != null && textComponent.staticText)
            {
                mark.has = true;
                textComponent.localizationKey = currentPath;
                // Use AppendLine for automatic newline and efficient string building
                stringBuilder.AppendLine($"{currentPath},{textComponent.text}");
            }

            // Recursively process child transforms
            foreach (Transform child in transform)
            {
                AddTextsFromGameObject(child, currentPath, stringBuilder, mark);
            }
        }

        private void SaveSettings()
        {
            if (string.IsNullOrEmpty(_save.excelPath) || !Directory.Exists(_save.excelPath))
            {
                _save.excelPath =
                    EditorUtility.OpenFolderPanel("Select the folder of excel files", Environment.CurrentDirectory, "");
            }

            EditorPrefs.SetString(SaveKey.excelPath, _save.excelPath);
            if (string.IsNullOrEmpty(_save.csFilePath) || !Directory.Exists(_save.csFilePath))
            {
                _save.csFilePath = "Assets/ConfigScript/";
            }

            if (string.IsNullOrEmpty(_save.assetFilePath) || !Directory.Exists(_save.assetFilePath))
            {
                _save.assetFilePath = "Assets/Resources/";
            }

            EditorPrefs.SetString(SaveKey.csFilePath, _save.csFilePath);
            EditorPrefs.SetString(SaveKey.assetFilePath, _save.assetFilePath);
            EditorPrefs.SetString(SaveKey.UIPrefabPath, _save.UIPrefabPath);
            EditorPrefs.SetString(SaveKey.localizationCSVPath, _save.localizationCSVPath);
        }
    }
}
#endif