#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio.Editor
{
    public class FontSettingItem: IEditorSettingWindowItem
    {
        public static string fontPath = "fontPath";
        public static string textMeshProFontPath = "textMeshProFontPath";

        public static string defaultFontPath = "Assets/UFlowFramework/Fonts/ZiHunBianTaoTi.ttf";
        public static string defaultTextMeshProFontPath = "Assets/UFlowFramework/Fonts/ZiHunBianTaoTiSDF.asset";

        public string _fontPath = defaultFontPath;
        public string _textMeshProFontPath = defaultTextMeshProFontPath;

        private Font _font;
        private TMP_FontAsset _textMeshProFont;

        public string itemName => "Set Editor Font";

        public void InitSave()
        {
            _fontPath = EditorSaveUtils.GetEditorPref(FontSettingItem.fontPath, FontSettingItem.defaultFontPath);
            _textMeshProFontPath = EditorSaveUtils.GetEditorPref(FontSettingItem.textMeshProFontPath, FontSettingItem.defaultTextMeshProFontPath);

            _font = AssetDatabase.LoadAssetAtPath<Font>(_fontPath);
            _textMeshProFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(_textMeshProFontPath);
        }

        public void OnDestroy()
        {
            _fontPath = null;
            _textMeshProFontPath = null;
            _font = null;
            _textMeshProFont = null;
        }

        public void OnGUI(EditorWindow window)
        {
            GUILayout.Label("itemName");
            _font = (Font) EditorGUILayout.ObjectField("Text Font: ", _font, typeof(Font), false);
            _textMeshProFont = (TMP_FontAsset) EditorGUILayout.ObjectField("TMP Font Asset", _textMeshProFont, typeof(TMP_FontAsset), false);
        }

        public void SaveData()
        {
            if (_font)
            {
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(_font, out string guid, out long a))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    _fontPath = path;                    
                }
            }
            else
            {
                _fontPath = defaultFontPath;
            }
            
            if (_textMeshProFont)
            {
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(_textMeshProFont, out string guid, out long a))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    _textMeshProFontPath = path;                    
                }
            }
            else
            {
                _textMeshProFontPath = defaultTextMeshProFontPath;
            }
            EditorSaveUtils.SetEditorPref(FontSettingItem.fontPath, _fontPath);
            EditorSaveUtils.SetEditorPref(FontSettingItem.textMeshProFontPath, _textMeshProFontPath);
        }
    }
}
#endif