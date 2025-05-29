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
        private RecycleScrollRect recycleScrollRect => target as RecycleScrollRect;
        
        private void OnEnable()
        {
            _layoutGroup = serializedObject.FindProperty("layoutGroup");
            _scroll = serializedObject.FindProperty("scroll");
            _maskObj = serializedObject.FindProperty("maskObj");
            _prefab = serializedObject.FindProperty("prefab");
            _direction = serializedObject.FindProperty("direction");
            _optimize = serializedObject.FindProperty("optimize");
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
                if (_layoutGroup.objectReferenceValue is VerticalLayoutGroup)
                {
                    _direction.enumValueIndex = (int) RecycleScrollRect.ListDirection.VERTICAL;
                }
                else if(_layoutGroup.objectReferenceValue is HorizontalLayoutGroup)
                {
                    _direction.enumValueIndex = (int) RecycleScrollRect.ListDirection.HORIZONTAL;
                }
                else if(_layoutGroup.objectReferenceValue is GridLayoutGroup gridLayoutGroup)
                {
                    if (gridLayoutGroup.startAxis == GridLayoutGroup.Axis.Horizontal)
                    {
                        _direction.enumValueIndex = (int) RecycleScrollRect.ListDirection.VERTICAL;
                    }
                    else if (gridLayoutGroup.startAxis == GridLayoutGroup.Axis.Vertical)
                    {
                        _direction.enumValueIndex = (int) RecycleScrollRect.ListDirection.HORIZONTAL;
                    }
                }
                else
                {
                    _layoutGroup.objectReferenceValue = null;
                    Debug.LogError("ListManager needs a [VerticalLayoutGroup] or a [HorizontalLayoutGroup]!");
                }
            }
            else
            {
                _direction.enumValueIndex = (int) RecycleScrollRect.ListDirection.HORIZONTAL;
            }
            EditorGUILayout.PropertyField(_layoutGroup);
            EditorGUILayout.PropertyField(_direction);
            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif