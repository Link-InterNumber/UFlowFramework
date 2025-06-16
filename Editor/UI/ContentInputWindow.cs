using UnityEngine;
using UnityEditor;

namespace PowerCellStudio
{
    public class ContentInputWindow : EditorWindow
    {
        private string _inputContent = "";

        public string inputContent {set => _inputContent = value;}

        private Action<string> _callback;
        
        public static void ShowWindow(Action<string> callback, string title, string defaultValue)
        {
            _callback = callback;
            ContentInputWindow window = GetWindow<ContentInputWindow>(true, title, true);
            window.inputContent = defaultValue;
            window.minSize = new Vector2(300, 100);
            window.maxSize = new Vector2(600, 100);
            window.ShowModalUtility();
        }
        
        void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("Enter Content", EditorStyles.boldLabel);
            
            GUILayout.Space(5);
            _inputContent = EditorGUILayout.TextField("Content", _inputContent);
            
            GUILayout.Space(15);
            
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Confirm", GUILayout.Width(100)))
            {
                callback?.Invoke(_inputContent.Trim());
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