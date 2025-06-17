#if UNITY_EDITOR
using UnityEngine
using UnityEditor;
using Object = UnityEngine.Object;

namespace PowerCellStudio
{
    [CustomPropertyDrawer(typeof(AssetPath))]
    public class AssetPathDrawer : PropertyDrawer
    {
        private Object m_obj; 

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // 获取字段
            SerializedProperty myStringProperty = property.FindPropertyRelative("path");
            // 计算每个字段的高度
            float singleLineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = 5; // 字段间的间距

            // 绘制 String 字段
            Rect stringRect = new Rect(position.x, position.y, position.width, singleLineHeight);

            var fieldType = fieldInfo.FieldType;
            if (fieldType.IsGenericType)
            {
                Type genericTypeDefinition = fieldType.GetGenericTypeDefinition();

                if (!string.IsNullOrEmpty(myStringProperty.stringValue) && m_obj == null)
                {
                    m_obj = AssetDatabase.LoadAssetAtPath(myStringProperty.stringValue);
                }
                m_obj = EditorGUILayout.ObjectField("AudioClip", m_obj, genericTypeDefinition, false);
                if (m_obj)
                {
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m_obj, out string guid, out long _))
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        myStringProperty.stringValue = path;                    
                    }
                }
                else
                {
                    myStringProperty.stringValue = string.Empty;
                }
                EditorGUI.PropertyField(stringRect, myStringProperty, new GUIContent("Path"));

            }
            else
            {
                EditorGUI.PropertyField(stringRect, myStringProperty, new GUIContent("Path"));
            }
        }
    }
}
#endif