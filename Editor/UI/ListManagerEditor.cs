#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    [CustomEditor(typeof(RecycleScrollRect), true)]
    public sealed class ListManagerEditor : Editor
    {
        private SerializedProperty _layoutGroup;
        private SerializedProperty _scroll;
        private SerializedProperty _maskObj;
        private SerializedProperty _prefab;
        private SerializedProperty _direction;
        private SerializedProperty _optimize;
        private SerializedProperty _spacing;
        private SerializedProperty _numPerLine
        private RecycleScrollRect recycleScrollRect => target as RecycleScrollRect;
        
        private void OnEnable()
        {
            _layoutGroup = serializedObject.FindProperty("layoutGroup");
            _scroll = serializedObject.FindProperty("scroll");
            _maskObj = serializedObject.FindProperty("maskObj");
            _prefab = serializedObject.FindProperty("prefab");
            _direction = serializedObject.FindProperty("direction");
            _optimize = serializedObject.FindProperty("optimize");
            _spacing = serializedObject.FindProperty("spacing");
            _numPerLine = serializedObject.FindProperty("numPerLine");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_scroll);
            EditorGUILayout.PropertyField(_maskObj);
            EditorGUILayout.PropertyField(_prefab);
            EditorGUILayout.PropertyField(_optimize);
            // EditorGUI.BeginChangeCheck();
            if (_layoutGroup.objectReferenceValue)
            {
                if (_layoutGroup.objectReferenceValue is VerticalLayoutGroup verticalLayoutGroup)
                {
                    _spacing.floatValue = verticalLayoutGroup.spacing;
                    _direction.enumValueIndex = (int) RecycleScrollRect.ListDirection.VERTICAL;
                    _numPerLine.intValue = 1;
                }
                else if(_layoutGroup.objectReferenceValue is HorizontalLayoutGroup horizontalLayoutGroup)
                {
                    _spacing.floatValue = horizontalLayoutGroup.spacing;
                    _direction.enumValueIndex = (int) RecycleScrollRect.ListDirection.HORIZONTAL;
                    _numPerLine.intValue = 1;
                }
                else if(_layoutGroup.objectReferenceValue is GridLayoutGroup gridLayoutGroup)
                {

                }
                else
                {
                    _layoutGroup.objectReferenceValue = null;
                    _numPerLine.intValue = 1;
                    Debug.LogError("ListManager needs a [VerticalLayoutGroup] or a [HorizontalLayoutGroup]!");
                }
            }
            else
            {
                _numPerLine.intValue = 1;
                _spacing.floatValue = 0f;
                _direction.enumValueIndex = (int) RecycleScrollRect.ListDirection.HORIZONTAL;
            }
            EditorGUILayout.PropertyField(_layoutGroup);
            EditorGUILayout.PropertyField(_direction);
            EditorGUILayout.PropertyField(_spacing);
            EditorGUILayout.PropertyField(_numPerLine);
            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif