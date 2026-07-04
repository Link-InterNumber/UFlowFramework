/*  
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.  
 * If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.  
 *  
 * Copyright (c) Ruoy  
 */
using System;
using System.Collections.Generic;
using System.Linq;
using EnjoyGameClub.TextLifeFramework.Core;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace EnjoyGameClub.TextLifeFramework.Editor
{
    [CustomPropertyDrawer(typeof(ProcessList))]
    public class ProcessListDrawer : PropertyDrawer
    {
        private Dictionary<string, ReorderableList> _reorderableListsMap;
        private const string PROPERTY_NAME = "ProcessesList";
        private const int HEADER_HEIGHT = 30;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty listProperty = property.FindPropertyRelative(PROPERTY_NAME);
            var list = GetReorderableList(listProperty, property, label);
            list?.DoList(position);
            property.serializedObject.ApplyModifiedProperties();
        }


        private ReorderableList GetReorderableList(SerializedProperty listProperty, SerializedProperty property,
            GUIContent label)
        {
            var path = listProperty.propertyPath;
            _reorderableListsMap ??= new();
            if (_reorderableListsMap.TryGetValue(path, out ReorderableList list))
            {
                return list;
            }

            list = new ReorderableList(property.serializedObject, listProperty, true, true, true, true);
            list.drawHeaderCallback = rect => { EditorGUI.LabelField(rect, label); };
            list.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                SerializedProperty element = listProperty.GetArrayElementAtIndex(index);
                rect.height = EditorGUIUtility.singleLineHeight;
                rect.x += 10;
                // 获取类型名称
                string typeName = "Null";
                if (element.managedReferenceValue != null)
                {
                    typeName = element.managedReferenceValue.GetType().Name;
                }

                EditorGUI.PropertyField(rect, element, new GUIContent(typeName), true);
            };
            list.elementHeightCallback = index =>
            {
                SerializedProperty element = listProperty.GetArrayElementAtIndex(index);
                return EditorGUI.GetPropertyHeight(element) + 4;
            };

            list.onAddDropdownCallback = (rect, reorderableList) =>
            {
                GenericMenu menu = new GenericMenu();
                var baseBuffType = typeof(AnimationProcess);
                var types = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(type => baseBuffType.IsAssignableFrom(type) && type != baseBuffType);
            
                foreach (var type in types)
                {
                    menu.AddItem(new GUIContent(type.Name), false, () =>
                    {
                        int newIndex = listProperty.arraySize;
                        listProperty.arraySize++;
                        SerializedProperty newElement = listProperty.GetArrayElementAtIndex(newIndex);
                        newElement.managedReferenceValue = Activator.CreateInstance(type);
                        listProperty.serializedObject.ApplyModifiedProperties();
                    });
                }
            
                menu.ShowAsContext();
            };

            list.onRemoveCallback = reorderableList =>
            {
                if (reorderableList.index >= 0)
                {
                    listProperty.DeleteArrayElementAtIndex(reorderableList.index);
                    listProperty.serializedObject.ApplyModifiedProperties();
                }
            };
            _reorderableListsMap.Add(path, list);
            return list;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var listProp = property.FindPropertyRelative(PROPERTY_NAME);
            var list = GetReorderableList(listProp, property, label);
            if (list == null) return EditorGUIUtility.singleLineHeight;
            return list.GetHeight() + HEADER_HEIGHT;
        }
    }
}