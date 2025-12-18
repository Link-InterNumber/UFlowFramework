using System;
using UnityEngine;
using UnityEditor;

namespace PowerCellStudio
{
    public class ContentInputEditorWindow : EditorWindow
    {
        private string _inputContent = "";

        public string inputContent { set => _inputContent = value; }

        private string _lableText = "Enter Content:";
        public string lableText { set => _lableText = value; }

        private static readonly string _saveKey = "ContentInputEditorWindow_LastInput";

        private Action<string> _callback;
        public Action<string> callback
        {
            set => _callback = value;
        }
        
        public static void ShowWindow(Action<string> callback, string title, string lableText, string defaultValue)
        {
            ContentInputEditorWindow editorWindow = GetWindow<ContentInputEditorWindow>(true, title, true);
            editorWindow.callback = callback;
            if (string.IsNullOrEmpty(defaultValue))
            {
                var saveKey = $"{title}_{_saveKey}";
                editorWindow.inputContent = EditorSaveUtils.GetEditorPref(saveKey, "");
            }
            else
            {
                editorWindow.inputContent = defaultValue;
            }
            if (!string.IsNullOrEmpty(lableText))
            {
                editorWindow.lableText = lableText;
            }
            editorWindow.minSize = new Vector2(300, 100);
            editorWindow.maxSize = new Vector2(600, 100);
            editorWindow.ShowModalUtility();
        }

        void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label(_lableText, EditorStyles.boldLabel);
            
            GUILayout.Space(5);
            _inputContent = EditorGUILayout.TextField("Content", _inputContent);
            
            GUILayout.Space(15);
            
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Confirm", GUILayout.Width(100)))
            {
                var saveKey = $"{titleContent.text}_{_saveKey}";
                EditorSaveUtils.SetEditorPref(saveKey, _inputContent);
                _callback?.Invoke(_inputContent.Trim());
                Close();
            }
            
            if (GUILayout.Button("Cancel", GUILayout.Width(100)))
            {
                Close();
            }
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}