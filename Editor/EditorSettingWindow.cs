#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio
{
    public class EditorSettingWindow : EditorWindow
    {
        public class  EditorSettingKey
        {
            public static string fontPath = "fontPath";
            public static string textMeshProFontPath = "textMeshProFontPath";
        }
        
        public class EditorSettingSave
        {
            public static string defaultFontPath = "Assets/FrameWork/Fonts/ZiHunBianTaoTi.ttf";
            public static string defaultTextMeshProFontPath = "Assets/FrameWork/Fonts/ZiHunBianTaoTiSDF.asset";
            public string fontPath = defaultFontPath;
            public string textMeshProFontPath = defaultTextMeshProFontPath;
        }
        
        private EditorSettingSave _save;
        private Font _font;
        private TMP_FontAsset _textMeshProFontPath;
        
        [MenuItem("Tools/Editor Setting Window")]
        static void OpenEditorSettingWindow()
        {
            EditorWindow.GetWindow<EditorSettingWindow>(false, "Editor Setting Window", true).Show();
        }
        
        void OnEnable()
        {
            _save = new EditorSettingSave();
            _save.fontPath = EditorPrefs.GetString(EditorSettingKey.fontPath,EditorSettingSave.defaultFontPath);
            _save.textMeshProFontPath = EditorPrefs.GetString(EditorSettingKey.textMeshProFontPath, EditorSettingSave.defaultTextMeshProFontPath);
            _font = AssetDatabase.LoadAssetAtPath<Font>(_save.fontPath);
            _textMeshProFontPath = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(_save.textMeshProFontPath);
        }

        private void OnDisable()
        {
            _font = null;
            _textMeshProFontPath = null;
            _save = null;
        }
        
        void OnGUI()
        {
            DisplayFontSetting();
            ExportPrefabText();
            if (GUILayout.Button("Save"))
            {
                SaveSettings();
            }
        }

        private void DisplayFontSetting()
        {
            GUILayout.Label("默认字体： ");
            _font = (Font) EditorGUILayout.ObjectField("字体", _font, typeof(Font));
            _textMeshProFontPath = (TMP_FontAsset) EditorGUILayout.ObjectField("TMP字体", _textMeshProFontPath, typeof(TMP_FontAsset));
            GUILayout.Space(20);
        }

        private void SaveSettings()
        {
            if (_font)
            {
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(_font, out string guid, out long a))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    _save.fontPath = path;                    
                }
            }
            else
            {
                _save.fontPath = EditorSettingSave.defaultFontPath;
            }
            
            if (_textMeshProFontPath)
            {
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(_textMeshProFontPath, out string guid, out long a))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    _save.textMeshProFontPath = path;                    
                }
            }
            else
            {
                _save.textMeshProFontPath = EditorSettingSave.defaultTextMeshProFontPath;
            }
            EditorPrefs.SetString(EditorSettingKey.fontPath, _save.fontPath);
            EditorPrefs.SetString(EditorSettingKey.textMeshProFontPath, _save.textMeshProFontPath);
        }

        private void ExportPrefabText()
        {
            GUILayout.Label("Export Text Components to CSV", EditorStyles.boldLabel);

            folderPath = EditorGUILayout.TextField("Folder Path", folderPath);
            outputFilePath = EditorGUILayout.TextField("Output File Path", outputFilePath);

            if (GUILayout.Button("Export"))
            {
                if (string.IsNullOrEmpty(folderPath))
                {
                    EditorUtility.DisplayDialog("Error", "Please specify a valid folder path.", "OK");
                    return;
                }

                if (string.IsNullOrEmpty(outputFilePath))
                {
                    EditorUtility.DisplayDialog("Error", "Please specify a valid output file path.", "OK");
                    return;
                }
                ExportTextsToCSV();
            }
            GUILayout.Space(20);
        }

        private class Mark
        {
            public bool has = false;
        }

        private void ExportTextsToCSV()
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
                    if (mark.has) AssetDatabase.SetDirty(prefab);
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
            Text textComponent = transform.GetComponent<TextEx>();
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
    }
}
#endif