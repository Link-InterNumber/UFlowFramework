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
                }
                else if(_layoutGroup.objectReferenceValue is HorizontalLayoutGroup horizontalLayoutGroup)
                {
                    _spacing.floatValue = horizontalLayoutGroup.spacing;
                    _direction.enumValueIndex = (int) RecycleScrollRect.ListDirection.HORIZONTAL;
                }
                else
                {
                    _layoutGroup.objectReferenceValue = null;
                    Debug.LogError("ListManager needs a [VerticalLayoutGroup] or a [HorizontalLayoutGroup]!");
                }
            }
            else
            {
                _spacing.floatValue = 0f;
                _direction.enumValueIndex = (int) RecycleScrollRect.ListDirection.HORIZONTAL;
            }
            EditorGUILayout.PropertyField(_layoutGroup);
            EditorGUILayout.PropertyField(_direction);
            EditorGUILayout.PropertyField(_spacing);
            // if(!EditorGUI.EndChangeCheck() || !_listManager)
            // {
            //     serializedObject.ApplyModifiedProperties();
            //     return;
            // }
            
            // if ((ListManager.ListDirection) _direction.enumValueIndex == ListManager.ListDirection.VERTICAL)
            // {
            //     var hl = _listManager.GetComponent<HorizontalLayoutGroup>();
            //     if (hl) GameObject.DestroyImmediate(hl);
            //     if(!_listManager.GetComponent<VerticalLayoutGroup>())
            //     {
            //         _layoutGroup.objectReferenceValue = _listManager.AddComponent<VerticalLayoutGroup>();
            //     }
            //     serializedObject.ApplyModifiedProperties();
            // }
            // else
            // {
            //     var vl = _listManager.GetComponent<VerticalLayoutGroup>();
            //     if (vl) GameObject.DestroyImmediate(vl);
            //     if(!_listManager.GetComponent<HorizontalLayoutGroup>())
            //     {
            //         _layoutGroup.objectReferenceValue = _listManager.AddComponent<HorizontalLayoutGroup>();
            //     }
            // }
            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif