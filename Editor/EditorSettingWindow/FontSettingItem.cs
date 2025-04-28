#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio
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
            _fontPath = EditorPrefs.GetString(FontSettingItem.fontPath, FontSettingItem.defaultFontPath);
            _textMeshProFontPath = EditorPrefs.GetString(FontSettingItem.textMeshProFontPath, FontSettingItem.defaultTextMeshProFontPath);

            _font = AssetDatabase.LoadAssetAtPath<Font>(_fontPath);
            _textMeshProFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(_textMeshProFontPath);
        }

        public void OnDestroy()
        {
            _font = null;
            _textMeshProFont = null;
        }

        public void OnGUI()
        {
            GUILayout.Label("itemName");
            _font = (Font) EditorGUILayout.ObjectField("Text Font: ", _font, typeof(Font));
            _textMeshProFont = (TMP_FontAsset) EditorGUILayout.ObjectField("TMP Font Asset", _textMeshProFont, typeof(TMP_FontAsset));
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
            EditorPrefs.SetString(FontSettingItem.fontPath, _fontPath);
            EditorPrefs.SetString(FontSettingItem.textMeshProFontPath, _textMeshProFontPath);
        }
    }
}
#endif