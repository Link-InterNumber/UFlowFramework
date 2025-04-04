using UnityEditor;
using UnityEngine;

namespace PowerCellStudio
{
    [CustomPropertyDrawer(typeof(SpaceAfterAttribute))]
    public class SpaceAfterDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, label, true);
            SpaceAfterAttribute spaceAfter = (SpaceAfterAttribute)attribute;
            position.y += EditorGUI.GetPropertyHeight(property, label, true);
            position.height = spaceAfter.spaceHeight;
            EditorGUI.LabelField(position, GUIContent.none);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SpaceAfterAttribute spaceAfter = (SpaceAfterAttribute)attribute;
            return EditorGUI.GetPropertyHeight(property, label, true) + spaceAfter.spaceHeight;
        }
    }
}