#if UNITY_EDITOR

using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;
using TextEditor = UnityEditor.UI.TextEditor;

namespace PowerCellStudio
{
    public class TextExMenu
    {
        [MenuItem("GameObject/UI/TextEx", false, 2)]
        public static void AddText(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("TextEx");
            CreateText(go);
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

        [MenuItem("GameObject/UI/Replace TextEx", false, 30)]
        public static void ReplaceWithText(MenuCommand menuCommand)
        {
            if (!Selection.activeGameObject) return;
            var text = Selection.activeGameObject.GetComponent<Text>();
            if(text is TextEx) return;
            var replaceInfo = new ReplaceTextInfo();
            if (text)
            {
                replaceInfo.textString = text.text;
                replaceInfo.style = text.fontStyle;
                replaceInfo.size = text.fontSize;
                replaceInfo.alignment = text.alignment;
                replaceInfo.horizontal = text.horizontalOverflow;
                replaceInfo.vertical = text.verticalOverflow;
                replaceInfo.bestFit = text.resizeTextForBestFit;
                replaceInfo.color = text.color;
                replaceInfo.raycast = text.raycastTarget;
                GameObject.DestroyImmediate(text);
            }
            else
            {
                var meshPro = Selection.activeGameObject.GetComponent<TextMeshProUGUI>();
                if(!meshPro) return;
                replaceInfo.textString = meshPro.text;
                replaceInfo.style = TextNTextMeshProUGUISwitch.ConvertFontStyle(meshPro.fontStyle);
                replaceInfo.size = meshPro.fontSize;
                replaceInfo.alignment = TextNTextMeshProUGUISwitch.ConvertTextAlignmentOptions(meshPro.alignment);
                replaceInfo.horizontal = HorizontalWrapMode.Overflow;
                replaceInfo.vertical = VerticalWrapMode.Overflow;
                replaceInfo.color = meshPro.color;
                replaceInfo.raycast = meshPro.raycastTarget;
                replaceInfo.colorGradient = meshPro.colorGradient;
                replaceInfo.bestFit = meshPro.enableAutoSizing;
                GameObject.DestroyImmediate(meshPro);
            }

            var newText = Selection.activeGameObject.AddComponent<TextEx>();
            var fontPath = EditorPrefs.GetString(FontSettingItem.fontPath, FontSettingItem.defaultFontPath);
            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (font) newText.font = font;
            newText.text = replaceInfo.textString;
            newText.fontStyle = replaceInfo.style;
            newText.fontSize = (int)replaceInfo.size;
            newText.alignment = replaceInfo.alignment;
            newText.horizontalOverflow = replaceInfo.horizontal;
            newText.verticalOverflow = replaceInfo.vertical;
            newText.resizeTextForBestFit = replaceInfo.bestFit;
            newText.color = replaceInfo.color;
            newText.raycastTarget = replaceInfo.raycast;
        }
        
        static TextEx CreateText(GameObject gameObject)
        {
            gameObject.AddComponent<RectTransform>().sizeDelta = new Vector2(160, 30);
            var text = gameObject.AddComponent<TextEx>();
            text.fontSize = 20;
            text.color = Color.black;
            text.alignment = TextAnchor.MiddleLeft;
            text.text = "Text..";
            text.raycastTarget = false;
            var fontPath = EditorPrefs.GetString(FontSettingItem.fontPath, FontSettingItem.defaultFontPath);
            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (font) text.font = font;
            return text;
        }
    }

    [CustomEditor(typeof(TextEx), true)]
    [CanEditMultipleObjects]
    public class TextExEditor : TextEditor
    {
        private SerializedProperty m_staticText;
        private SerializedProperty m_localizationKey;
        private SerializedProperty m_changeFontWhenLanChange;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_staticText = serializedObject.FindProperty("staticText");
            m_localizationKey = serializedObject.FindProperty("localizationKey");
            m_changeFontWhenLanChange = serializedObject.FindProperty("changeFontWhenLanChange");
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            m_staticText = null;
            m_localizationKey = null;
            m_changeFontWhenLanChange = null;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Localization");
            EditorGUILayout.PropertyField(m_changeFontWhenLanChange);
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
                    && GUILayout.Button("添加本地化Key到配置") )
                {
                    var localizationKey = m_localizationKey.stringValue;
                    var table = LocalizationSettings.StringDatabase.GetTable(ConstSetting.LocalizationStringTable);
                    // var sharedTable = AssetDatabase.LoadAssetAtPath<SharedTableData>()
                    if (table != null)
                    {
                        var entry = table.AddEntry(localizationKey, serializedObject.FindProperty("m_Text")?.stringValue??string.Empty);
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
                    if (table != null)
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
                    else
                    {
                        EditorGUILayout.LabelField("本地化文本", "No Table");
                    }
                }
            }
            serializedObject.ApplyModifiedProperties();
        }

        private string GetPathName(Transform tr)
        {
            if (!tr) return "TextEx";
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
                if(na == "Canvas (Environment)") continue;
                path.Append(na);
                if(i > 0) path.Append("_");
            }
            names = null;
           return path.ToString();
        }
    }
}
#endif