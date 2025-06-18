#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace PowerCellStudio
{
    [CustomPropertyDrawer(typeof(AssetPath<>))]
    public class AssetPathDrawer : PropertyDrawer
    {
        private Object m_obj;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // 获取字段
            SerializedProperty myStringProperty = property.FindPropertyRelative("assetPath");
            // 计算每个字段的高度
            float singleLineHeight = EditorGUIUtility.singleLineHeight;

            // 绘制 String 字段
            Rect stringRect = new Rect(position.x, position.y, position.width, singleLineHeight);

            var fieldType = fieldInfo.FieldType;
            var gens = fieldType.GetGenericArguments();
            var genericType = (gens != null && gens.Length > 0) ? gens[0] : null;
            if (genericType != null)
            {
                if (!string.IsNullOrEmpty(myStringProperty.stringValue) && m_obj == null)
                {
                    m_obj = AssetDatabase.LoadAssetAtPath(myStringProperty.stringValue, genericType);
                }
                // 缩进一格
                EditorGUI.indentLevel++;
                m_obj = EditorGUILayout.ObjectField($"└─ {genericType.Name}", m_obj, genericType, false);
                EditorGUI.indentLevel--;
                if (m_obj)
                {
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m_obj, out string guid, out long _))
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        myStringProperty.stringValue = path;
                        GUI.enabled = false;
                    }
                }
                else
                {
                    myStringProperty.stringValue = string.Empty;
                }
                EditorGUI.PropertyField(stringRect, myStringProperty, new GUIContent("Asset Path"));

            }
            else
            {
                GUI.enabled = true;
                EditorGUI.PropertyField(stringRect, myStringProperty, new GUIContent("Asset Path"));
            }
            EditorGUILayout.Space();
        }
    }
}
#endif