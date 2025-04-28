#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio
{
    public class EditorSettingWindow : EditorWindow
    {
        
        [MenuItem("Tools/Editor Setting Window")]
        static void OpenEditorSettingWindow()
        {
            EditorWindow.GetWindow<EditorSettingWindow>(false, "Editor Setting Window", true).Show();
        }

        private List<IEditorSettingWindowItem> items;
        private Vector2 scrollPosition;
        private string splitStr;

        private static GUIStyle titleStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 25,
            fontStyle = FontStyle.Bold,
        };

        void OnEnable()
        {
            scrollPosition = Vector2.zero;
            items = new List<IEditorSettingWindowItem>();
            // titleStyle = new GUIStyle(EditorStyles.label);
            // titleStyle.fontSize = 25;
            // titleStyle.fontStyle = FontStyle.Bold;
            // titleStyle.normal.textColor = Color.white;
            
            splitStr = "------------------------ Divider --------------------------";
            var interfaceType = typeof(IEditorSettingWindowItem);
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                if (interfaceType.IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                {
                    // 创建实例
                    var instance = (IEditorSettingWindowItem)Activator.CreateInstance(type);
                
                    // 使用实例
                    items.Add(instance);
                }
            }
            
            for (var i = 0; i < items.Count; i++)
            {
                items[i].InitSave();
            }
        }

        private void OnDisable()
        {
            for (var i = 0; i < items.Count; i++)
            {
                items[i].OnDestroy();
            }
            items = null;
            splitStr = null;
        }
        
        void OnGUI()
        {
            if (items == null) return;
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            for (var i = 0; i < items.Count; i++)
            {
                GUILayout.Label(items[i].itemName, titleStyle);
                items[i].OnGUI();
                // GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(5));
                // Define the rect for the line
        
                // Draw the line
                GUILayout.Space(10);
                GUILayout.Label(splitStr);
                GUILayout.Space(10);
            }
            GUILayout.EndScrollView();
            GUILayout.Space(10);
            if (GUILayout.Button("Save"))
            {
                for (var i = 0; i < items.Count; i++)
                {
                    items[i].SaveData();
                }
            }
            GUILayout.Space(10);
        }
    }
}
#endif