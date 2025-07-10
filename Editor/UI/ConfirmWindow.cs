using System;
using UnityEngine;
using UnityEditor;

namespace PowerCellStudio
{
    public class ConfirmWindow : EditorWindow
    {
        private string _inputContent = "";

        public string inputContent {set => _inputContent = value;}

        private Action _onConfirm;
        public Action onConfirm
        {
            set => _onConfirm = value;
        }
        
        private Action _onCancel;
        public Action onCancel
        {
            set => _onCancel = value;
        }

        public static void ShowWindow(Action confirm, Action cancel, string title, string showContent)
        {
            ConfirmWindow window = GetWindow<ConfirmWindow>(true, title, true);
            window.onConfirm = confirm;
            window.onCancel = cancel;
            window.inputContent = showContent;
            window.minSize = new Vector2(300, 100);
            window.maxSize = new Vector2(600, 100);
            window.ShowModalUtility();
        }

        void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label(_inputContent, EditorStyles.boldLabel);
            GUILayout.Space(15);
            var needClose = false;
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Confirm", GUILayout.Width(100)))
            {
                _onConfirm?.Invoke();
                needClose = true;
            }
            
            if (GUILayout.Button("Cancel", GUILayout.Width(100)))
            {
                _onCancel?.Invoke();
                needClose = true;
            }
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            if (needClose) Close();
        }
    }
}