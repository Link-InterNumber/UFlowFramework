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

        // private void OnFocus()
        // {
        //     _save.fontPath = EditorPrefs.GetString("fontPath", "Assets/FrameWork/Fonts/ZiHunBianTaoTi.ttf");
        //     _save.textMeshProFontPath = EditorPrefs.GetString("textMeshProFontPath", "Assets/FrameWork/Fonts/ZiHunBianTaoTiSDF.asset");
        //     _font = AssetDatabase.LoadAssetAtPath<Font>(_save.fontPath);
        //     _textMeshProFontPath = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(_save.textMeshProFontPath);
        // }
        
        void OnGUI()
        {
            GUILayout.Label("默认字体： ");
            _font = (Font) EditorGUILayout.ObjectField("字体", _font, typeof(Font));
            _textMeshProFontPath = (TMP_FontAsset) EditorGUILayout.ObjectField("TMP字体", _textMeshProFontPath, typeof(TMP_FontAsset));
            if (GUILayout.Button("Save"))
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
}
#endif