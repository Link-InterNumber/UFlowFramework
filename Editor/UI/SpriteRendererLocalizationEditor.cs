using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace PowerCellStudio
{
    [CustomEditor(typeof(SpriteRendererLocalization), true)]
    public class SpriteRendererLocalizationEditor : Editor
    {
        private SerializedProperty m_img;
        private SerializedProperty m_localizationKey;

        private SpriteRendererLocalization _target => target as SpriteRendererLocalization;

        private void OnEnable()
        {
            m_localizationKey = serializedObject.FindProperty("localizationKey");
            m_img = serializedObject.FindProperty("img");
        }

        private void OnDisable()
        {
            m_localizationKey = null;
            m_img = null;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            if (m_img.objectReferenceValue == null)
            {
                m_img.objectReferenceValue = _target.GetComponent<SpriteRenderer>();
            }
            
            if (string.IsNullOrEmpty(m_localizationKey.stringValue)
                && !Application.isPlaying 
                && GUILayout.Button("生成本地化Key"))
            {
                var localizationKey = GetPathName(_target.transform);
                m_localizationKey.stringValue = localizationKey;
            }
            
            if (!string.IsNullOrEmpty(m_localizationKey.stringValue)
                && !Application.isPlaying 
                && GUILayout.Button("添加本地化Key到配置"))
            {
                var collection = LocalizationEditorSettings.GetAssetTableCollection(ConstSetting.LocalizationAssetTable);
                var localizationKey = m_localizationKey.stringValue;
                AssetTable table = LocalizationSettings.AssetDatabase.GetTable(ConstSetting.LocalizationAssetTable);
                if (table != null)
                {
                    var sprite = (m_img.objectReferenceValue as SpriteRenderer).sprite;
                    var assetPath = AssetDatabase.GetAssetPath(sprite);
                    var guid = AssetDatabase.AssetPathToGUID(assetPath);
                    // var entry = new AssetTableEntry()
                    var entry = table.AddEntry(localizationKey, guid);
                    collection.AddAssetToTable(table, entry.Key, sprite, true);
                    table.SharedData.AddKey(entry.Key, entry.KeyId);
                    EditorUtility.SetDirty(table);
                    EditorUtility.SetDirty(table.SharedData);
                    AssetDatabase.SaveAssetIfDirty(table.SharedData);
                    AssetDatabase.SaveAssetIfDirty(table);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private string GetPathName(Transform tr)
        {
            if (!tr) return "SpriteRendererLocalization";
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