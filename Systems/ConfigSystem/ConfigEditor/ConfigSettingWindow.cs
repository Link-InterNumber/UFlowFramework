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
        }

        public class SaveKey
        {
            public static string excelPath = "excelPath";
            public static string csFilePath = "csFilePath";
            public static string assetFilePath = "assetFilePath";
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
            var defaultExcelPath = Environment.CurrentDirectory + "/ExcelFiles/";
            var defaultCsPath = "Assets/ConfigScript/";
            var defaultAssetPath = "Assets/Resources/";
            _save.excelPath = EditorPrefs.GetString(SaveKey.excelPath, defaultExcelPath);
            _save.csFilePath = EditorPrefs.GetString(SaveKey.csFilePath, defaultCsPath);
            _save.assetFilePath = EditorPrefs.GetString(SaveKey.assetFilePath, defaultAssetPath);

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
        }
    }
}
#endif