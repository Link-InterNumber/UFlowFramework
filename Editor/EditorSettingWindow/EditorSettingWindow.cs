#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio.Editor
{
    public class EditorSettingWindow : EditorWindow
    {
        
        public static void ShowWindow()
        {
            EditorWindow.GetWindow<EditorSettingWindow>(false, "Editor Setting Window", true).Show();
        }

        private List<IEditorSettingWindowItem> items;
        private Vector2 scrollPosition;
        private const float tabHeight = 25;
        private const float tabMaxWidth = 150;

        // private static GUIStyle titleStyle = new GUIStyle(EditorStyles.label)
        // {
        //     fontSize = 25,
        //     fontStyle = FontStyle.Bold,
        // };

        void OnEnable()
        {
            scrollPosition = Vector2.zero;
            items = new List<IEditorSettingWindowItem>(); 
            _selectIndex = 0;
            
            var interfaceType = typeof(IEditorSettingWindowItem);
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                if (interfaceType.IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                {
                    // 创建实例
                    var instance = (IEditorSettingWindowItem)Activator.CreateInstance(type);
                    items.Add(instance);
                }
            }
            
            if (items.Count > 0) items[0].InitSave();
            // for (var i = 0; i < items.Count; i++)
            // {
            //     items[i].InitSave();
            // }
        }

        private void OnDisable()
        {
            if (items == null) return;
            for (var i = 0; i < items.Count; i++)
            {
                items[i].OnDestroy();
            }
            items = null;
        }
        
        private int _selectIndex;
        void OnGUI()
        {
            EditorUIStyle.DrawWindowBackground(new Rect(Vector2.zero, position.size));
            if (items == null || items.Count == 0)
            {
                EditorGUILayout.HelpBox("No editor setting modules were found.", MessageType.Info);
                return;
            }

            EditorUIStyle.DrawImguiHeader("Editor Settings", "Configure UFlow editor tools from one place.");

            float currentWidth = position.width - 20; // Leave space for scroll bar
            float xOffset = 0;
            float yOffset = 50;


            GUILayout.BeginVertical();
            for (var i = 0; i < items.Count; i++)
            {
                var tab = items[i];
                // 根据 tab.itemName 长度计算 tab 的宽度
                float tabWidth = Mathf.Min(tabMaxWidth, tab.itemName.Length * 8); // Mathf.Min(tabMaxWidth, currentWidth / items.Count);

                if (xOffset + tabWidth > currentWidth)
                {
                    // Move to next line
                    xOffset = 0;
                    yOffset += tabHeight + 5; // 5 for margin
                }

                // Use a rect to create a button position
                Rect tabRect = new Rect(xOffset, yOffset, tabWidth, tabHeight);
                
                var previousColor = GUI.backgroundColor;
                if (i == _selectIndex) GUI.backgroundColor = EditorUIStyle.AccentColor;
                if (GUI.Button(tabRect, tab.itemName, EditorUIStyle.TabButton))
                {
                    items[_selectIndex].SaveData();
                    items[_selectIndex].OnDestroy();
                    _selectIndex = i;
                    items[_selectIndex].InitSave();
                }
                GUI.backgroundColor = previousColor;

                xOffset += tabWidth + 5; // Increment x position with margin
            }
            GUILayout.Space(tabHeight);
            GUILayout.EndVertical();

            GUILayout.Label(items[_selectIndex].itemName, EditorUIStyle.WindowTitle);
            using (var scroll = new EditorGUILayout.ScrollViewScope(scrollPosition, EditorUIStyle.PanelBox))
            {
                scrollPosition = scroll.scrollPosition;
                items[_selectIndex].OnGUI(this);
            }
            
            GUILayout.Space(10);
            if (GUILayout.Button("Save", EditorUIStyle.PrimaryButton))
            {
                items[_selectIndex].SaveData();
            }
            GUILayout.Space(10);
        }
    }
}
#endif