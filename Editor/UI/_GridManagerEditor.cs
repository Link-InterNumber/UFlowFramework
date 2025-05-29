// #if UNITY_EDITOR
//
// using UnityEditor;
// using UnityEngine;
// using UnityEngine.UI;
//
// namespace PowerCellStudio
// {
//     [CustomEditor(typeof(GridManager), true)]
//     public sealed class GridManagerEditor : Editor
//     {
//         private SerializedProperty _layoutGroup;
//         private SerializedProperty _scroll;
//         private SerializedProperty _maskObj;
//         private SerializedProperty _prefab;
//         private SerializedProperty _direction;
//         private SerializedProperty _optimize;
//         private GridManager _listManager => target as GridManager;
//         
//         
//         // public GridLayoutGroup grid;
//         // public ScrollRect scroll;
//         // public Mask maskObj;
//         // public RectTransform prefab;
//         // public GridDirection direction = GridDirection.Horizontal;
//         // public bool optimize = true;
//         private void OnEnable()
//         {
//             _layoutGroup = serializedObject.FindProperty("grid");
//             _scroll = serializedObject.FindProperty("scroll");
//             _maskObj = serializedObject.FindProperty("maskObj");
//             _prefab = serializedObject.FindProperty("prefab");
//             _direction = serializedObject.FindProperty("direction");
//             _optimize = serializedObject.FindProperty("optimize");
//         }
//         
//         
//         public override void OnInspectorGUI()
//         {
//             serializedObject.Update();
//             EditorGUILayout.PropertyField(_scroll);
//             EditorGUILayout.PropertyField(_maskObj);
//             EditorGUILayout.PropertyField(_prefab);
//             EditorGUILayout.PropertyField(_optimize);
//             // EditorGUI.BeginChangeCheck();
//             if (_layoutGroup.objectReferenceValue)
//             {
//                 if  (!(_layoutGroup.objectReferenceValue is GridLayoutGroup))
//                 {
//                     _layoutGroup.objectReferenceValue = null;
//                     Debug.LogError("GridManager needs a [GridLayoutGroup]!");
//                 }
//             }
//             EditorGUILayout.PropertyField(_layoutGroup);
//             EditorGUILayout.PropertyField(_direction);
//             // if(!EditorGUI.EndChangeCheck() || !_listManager)
//             // {
//             //     serializedObject.ApplyModifiedProperties();
//             //     return;
//             // }
//             
//             // if ((GridManager.ListDirection) _direction.enumValueIndex == GridManager.ListDirection.VERTICAL)
//             // {
//             //     var hl = _listManager.GetComponent<HorizontalLayoutGroup>();
//             //     if (hl) GameObject.DestroyImmediate(hl);
//             //     if(!_listManager.GetComponent<VerticalLayoutGroup>())
//             //     {
//             //         _layoutGroup.objectReferenceValue = _listManager.AddComponent<VerticalLayoutGroup>();
//             //     }
//             //     serializedObject.ApplyModifiedProperties();
//             // }
//             // else
//             // {
//             //     var vl = _listManager.GetComponent<VerticalLayoutGroup>();
//             //     if (vl) GameObject.DestroyImmediate(vl);
//             //     if(!_listManager.GetComponent<HorizontalLayoutGroup>())
//             //     {
//             //         _layoutGroup.objectReferenceValue = _listManager.AddComponent<HorizontalLayoutGroup>();
//             //     }
//             // }
//             serializedObject.ApplyModifiedProperties();
//         }
//     }
// }
//
// #endif