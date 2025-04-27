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

        public string itemName => "设置默认字体";

        public void InitSave()
        {
            _fontPath = EditorPrefs.GetString(FontSettingItem.fontPath, FontSettingItem.defaultFontPath);
            _textMeshProFontPath = EditorPrefs.GetString(FontSettingItem.textMeshProFontPath, FontSettingItem.defaultTextMeshProFontPath);

            _font = AssetDatabase.LoadAssetAtPath<Font>(_save.fontPath);
            _textMeshProFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(_save.textMeshProFontPath);
        }

        public void OnDestroy()
        {
            _font = null;
            _textMeshProFont = null;
        }

        public void OnGUI()
        {
            GUILayout.Label("itemName");
            _font = (Font) EditorGUILayout.ObjectField("字体", _font, typeof(Font));
            _textMeshProFont = (TMP_FontAsset) EditorGUILayout.ObjectField("TMP字体", _textMeshProFont, typeof(TMP_FontAsset));
            // 绘制分割线
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
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
                _fontPath = EditorSettingSave.defaultFontPath;
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
                _textMeshProFontPath = EditorSettingSave.defaultTextMeshProFontPath;
            }
            EditorPrefs.SetString(FontSettingItem.fontPath, _fontPath);
            EditorPrefs.SetString(FontSettingItem.textMeshProFontPath, _textMeshProFontPath);
        }
    }
}
#endif