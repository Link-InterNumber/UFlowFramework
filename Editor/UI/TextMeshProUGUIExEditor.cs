#if UNITY_EDITOR

using System.Collections.Generic;
using System.Text;
using TMPro;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace PowerCellStudio
{
    public class TextMeshProExMenu
    {
        [MenuItem("GameObject/UI/TextMeshProUGUIEx", false, 3)]
        public static void AddTextMeshPro(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("TextMeshProUGUIEx");
            CreateTextMeshPro(go);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            GameObject parent = menuCommand.context as GameObject;
            if (parent != null)
            {
                string uniqueName = GameObjectUtility.GetUniqueNameForSibling(parent.transform, go.name);
                go.name = uniqueName;
                Undo.SetTransformParent(go.transform, parent.transform, "Parent " + go.name);
                GameObjectUtility.SetParentAndAlign(go, parent);
            }
            Selection.activeGameObject = go;
        }
        
        [MenuItem("GameObject/UI/Replace TextMeshProUGUIEx", false, 31)]
        public static void ReplaceWithTextMeshPro(MenuCommand menuCommand)
        {
            if (!Selection.activeGameObject) return;
            var text = Selection.activeGameObject.GetComponent<TextMeshProUGUI>();
            if(text is TextMeshProUGUIEx) return;
            var replaceInfo = new ReplaceTextInfo();
            if (text)
            {
                replaceInfo.textString = text.text;
                replaceInfo.meshProStyle = text.fontStyle;
                replaceInfo.size = text.fontSize;
                replaceInfo.meshProAlignment = text.alignment;
                replaceInfo.meshProHorizontal = text.horizontalAlignment;
                replaceInfo.meshProVertical = text.verticalAlignment;
                replaceInfo.color = text.color;
                replaceInfo.raycast = text.raycastTarget;
                replaceInfo.colorGradient = text.colorGradient;
                replaceInfo.autoSize = text.enableAutoSizing;
                GameObject.DestroyImmediate(text);
            }
            else
            {
                var t = Selection.activeGameObject.GetComponent<Text>();
                if(!t) return;
                replaceInfo.textString = t.text;
                replaceInfo.meshProStyle = TextNTextMeshProUGUISwitch.ConvertFontStyle(t.fontStyle);
                replaceInfo.size = text.fontSize;
                replaceInfo.meshProAlignment = TextNTextMeshProUGUISwitch.ConvertTextAnchor(t.alignment);
                replaceInfo.meshProHorizontal = HorizontalAlignmentOptions.Center;
                replaceInfo.meshProVertical = VerticalAlignmentOptions.Middle;
                replaceInfo.color = t.color;
                replaceInfo.raycast = t.raycastTarget;
                replaceInfo.autoSize = t.resizeTextForBestFit;
                GameObject.DestroyImmediate(t);
            }

            var newText = Selection.activeGameObject.AddComponent<TextMeshProUGUIEx>();
            var fontPath = EditorPrefs.GetString(FontSettingItem.textMeshProFontPath, FontSettingItem.defaultTextMeshProFontPath);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if (font) newText.font = font;
            newText.text = replaceInfo.textString;
            newText.fontStyle = replaceInfo.meshProStyle;
            newText.fontSize = replaceInfo.size;
            newText.alignment = replaceInfo.meshProAlignment;
            newText.horizontalAlignment = replaceInfo.meshProHorizontal;
            newText.verticalAlignment = replaceInfo.meshProVertical;
            newText.color = replaceInfo.color;
            newText.raycastTarget = replaceInfo.raycast;
            newText.colorGradient = replaceInfo.colorGradient;
            newText.enableAutoSizing = replaceInfo.autoSize;
        }
        
        static TextMeshProUGUIEx CreateTextMeshPro(GameObject gameObject)
        {
            gameObject.AddComponent<RectTransform>().sizeDelta = new Vector2(160, 30);
            var text = gameObject.AddComponent<TextMeshProUGUIEx>();
            text.fontSize = 20;
            text.color = Color.black;
            text.alignment = TextAlignmentOptions.Midline;
            text.text = "Text..";
            text.raycastTarget = false;
            var fontPath = EditorPrefs.GetString(FontSettingItem.textMeshProFontPath, FontSettingItem.defaultTextMeshProFontPath);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if (font) text.font = font;
            return text;
        }
    }

    [CustomEditor(typeof(TextMeshProUGUIEx), true)]
    [CanEditMultipleObjects]
    public class TextMeshProUGUIExEditor : TMP_EditorPanelUI
    {
        private SerializedProperty m_staticText;
        private SerializedProperty m_localizationKey;
        // private GUIStyle _style;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_staticText = serializedObject.FindProperty("staticText");
            m_localizationKey = serializedObject.FindProperty("localizationKey");
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            m_staticText = null;
            m_localizationKey = null;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Localization");
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_staticText);
            if (m_staticText.boolValue)
            {
                if (string.IsNullOrEmpty(m_localizationKey.stringValue)
                    && !Application.isPlaying 
                    && GUILayout.Button("生成本地化Key"))
                {
                    var localizationKey = GetPathName((target as TextEx)?.transform);
                    m_localizationKey.stringValue = localizationKey;
                }
                if (!string.IsNullOrEmpty(m_localizationKey.stringValue)
                    && !Application.isPlaying 
                    && GUILayout.Button("添加本地化Key到配置"))
                {
                    var localizationKey = m_localizationKey.stringValue;
                    var table = LocalizationSettings.StringDatabase.GetTable(ConstSetting.LocalizationStringTable);
                    if (table != null)
                    {
                        var entry = table.AddEntry(localizationKey, serializedObject.FindProperty("m_text")?.stringValue??string.Empty);
                        table.SharedData.AddKey(entry.Key, entry.KeyId);
                        EditorUtility.SetDirty(table);
                        EditorUtility.SetDirty(table.SharedData);
                        AssetDatabase.SaveAssetIfDirty(table.SharedData);
                        AssetDatabase.SaveAssetIfDirty(table);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                }
                
                EditorGUILayout.PropertyField(m_localizationKey);
                if (!string.IsNullOrEmpty(m_localizationKey.stringValue))
                {
                    var entryKey = m_localizationKey.stringValue;
                    var table = LocalizationSettings.StringDatabase.GetTable(ConstSetting.LocalizationStringTable);
                    if (table == null)
                    {
                        EditorGUILayout.LabelField("本地化文本", "No Table");
                    }
                    else
                    {
                        var entry = table.GetEntry(entryKey);
                        if (entry != null)
                        {
                            var text = entry.GetLocalizedString();
                            EditorGUILayout.LabelField("本地化文本", text);
                        }
                        else
                        {
                            EditorGUILayout.LabelField("本地化文本", "No Entry");
                        }
                    }
                }
            }
            serializedObject.ApplyModifiedProperties();
        }

        private string GetPathName(Transform tr)
        {
            if (!tr) return "TextMeshProUGUIEx";
            Transform cur = tr;
            var path = new StringBuilder();
            var names = new List<string>();
            while (cur.parent)
            {
                names.Add(cur.name);
                cur = cur.parent;
            }
            names.Add(cur.name);
            for (var i = names.Count - 1; i >= 0; i--)
            {
                var na = names[i];
                path.Append(na);
                if(i > 0) path.Append("_");
            }
            names = null;
           return path.ToString();
        }
    }
}
#endif