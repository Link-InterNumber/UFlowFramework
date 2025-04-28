#if UNITY_EDITOR
using System.IO;
using System.Text;
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
            public static string defaultFontPath = "Assets/UFlowFramework/Fonts/ZiHunBianTaoTi.ttf";
            public static string defaultTextMeshProFontPath = "Assets/UFlowFramework/Fonts/ZiHunBianTaoTiSDF.asset";
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
    }
}
#endif