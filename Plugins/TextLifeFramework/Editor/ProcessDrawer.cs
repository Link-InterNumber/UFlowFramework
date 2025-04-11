/*  
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.  
 * If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.  
 *  
 * Copyright (c) Ruoy  
 */
using System;
using System.Collections.Generic;
using System.Reflection;
using EnjoyGameClub.TextLifeFramework.Core;
using UnityEditor;
using UnityEngine;

namespace EnjoyGameClub.TextLifeFramework.Editor
{
    [CustomPropertyDrawer(typeof(AnimationProcess), true)]
    public class ProcessDrawer : PropertyDrawer
    {
        public ProcessDrawer()
        {
            Init();
        }

        private Dictionary<Type, List<FieldInfo>> _dictionary = new Dictionary<Type, List<FieldInfo>>();
        private const float SPACING = 10f;
        private const float WIDTH_OFFSET = 10;
        private const float X_OFFSET = 10;
        private const float PROPERTY_X_OFFSET = 20;
        private const int HEADER_FONT_SIZE = 14;
        private const int HEADER_HEIGHT = 30; 


        private GUIStyle _headerStyle;
        private Color _borderColor;
        private bool _isFolder = true;


        private void Init()
        {
            _headerStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = Mathf.RoundToInt(HEADER_FONT_SIZE),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(0, 0, 3, 3),
                normal =
                {
                    textColor = EditorGUIUtility.isProSkin
                        ? new Color(0.8f, 0.8f, 0.8f)
                        : Color.black
                }
            };
            _borderColor = new Color(0.2f, 0.2f, 0.2f, 1f);

        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var currentObject = property.managedReferenceValue;
            if (currentObject == null)
            {
                return;
            }

            // 绘制下拉菜单
            _isFolder = EditorGUI.Foldout(position, _isFolder, label);
            if (_isFolder)
            {
                // 绘制边框
                EditorGUI.DrawRect(
                    new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width - WIDTH_OFFSET,
                        GetPropertyHeight(property, label) - EditorGUIUtility.singleLineHeight), _borderColor);
                position.y += EditorGUIUtility.singleLineHeight + 1;

                // 绘制属性
                EditorGUI.LabelField(
                    new Rect(position.x + X_OFFSET, position.y, position.width - WIDTH_OFFSET - PROPERTY_X_OFFSET,
                        HEADER_HEIGHT), currentObject.GetType().Name.ToUpperInvariant(), _headerStyle);
                position.y += HEADER_HEIGHT;
                var fields = GetBaseClassFields(currentObject.GetType(), typeof(AnimationProcess));
                foreach (var field in fields)
                {
                    var fieldProp = property.FindPropertyRelative(field.Name);
                    if (fieldProp != null)
                    {
                        float fieldHeight = EditorGUI.GetPropertyHeight(fieldProp, true);
                        EditorGUI.PropertyField(
                            new Rect(position.x + X_OFFSET, position.y,
                                position.width - WIDTH_OFFSET - PROPERTY_X_OFFSET,
                                fieldHeight),
                            fieldProp,
                            true);
                        position.y += fieldHeight + 2;
                    }
                }
            }
        }
        

        private List<FieldInfo> GetBaseClassFields(Type type, Type stopType)
        {
            if (_dictionary.TryGetValue(type, out List<FieldInfo> fieldInfos))
            {
                return fieldInfos;
            }

            Type baseType = type;
            Stack<FieldInfo[]> fields = new();
            while (type != null && type != stopType.BaseType) // 遍历到 BaseBuff 但不继续往上
            {
                fields.Push(type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly));
                type = type.BaseType;
            }

            fieldInfos = new List<FieldInfo>();
            while (fields.Count > 0)
            {
                fieldInfos.AddRange(fields.Pop());
            }

            _dictionary.Add(baseType, fieldInfos);
            return fieldInfos;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!_isFolder)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            float height = EditorGUIUtility.singleLineHeight + 4; // 标题的高度
            var currentObject = property.managedReferenceValue;

            if (currentObject == null)
            {
                return height + 30; // 额外留出 Null 提示的高度
            }

            Type objectType = currentObject.GetType();
            var fields = GetBaseClassFields(objectType, typeof(AnimationProcess));

            height += HEADER_HEIGHT;
            foreach (var field in fields)
            {
                var fieldProp = property.FindPropertyRelative(field.Name);
                if (fieldProp != null)
                {
                    height += EditorGUI.GetPropertyHeight(fieldProp, true) + 2;
                }
            }
            
            return height;
        }
    }
}