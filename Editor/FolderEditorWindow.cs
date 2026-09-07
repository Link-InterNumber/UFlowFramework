using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio.Editor
{
    public abstract class FolderEditorWindow : EditorWindow
    {
        protected virtual void OnGUI()
        {
            if (Selection.assetGUIDs.Length <= 0)
            {
                EditorGUILayout.HelpBox("请先在 Project 窗口选择一个文件夹。", MessageType.Info);
            }
            else
            {
                var folder = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
                if (!Directory.Exists(folder))
                {
                    EditorGUILayout.HelpBox("当前选择不是文件夹，请重新选择。", MessageType.Warning);
                    return;
                }
                using (new EditorGUILayout.VerticalScope(EditorUIStyle.PanelBox))
                {
                    EditorGUILayout.LabelField("当前选中的文件夹", EditorUIStyle.SectionTitle);
                    EditorGUILayout.SelectableLabel(folder, EditorStyles.textField,
                        GUILayout.Height(EditorGUIUtility.singleLineHeight));
                }
            }
        }
        
        protected abstract string _filter { get; }
        
        public delegate void DealWithFileHandle(string[] guids);
        protected void DrawButton(string buttonName, DealWithFileHandle action)
        {
            DrawButton(buttonName, action, GUI.skin.button);
        }

        protected void DrawButton(string buttonName, DealWithFileHandle action, GUIStyle style)
        {
            if (!GUILayout.Button(buttonName, style)) return;
            if (Selection.assetGUIDs.Length == 0)
            {
                EditorUtility.DisplayDialog("未选择文件夹", "请先在 Project 窗口选择一个文件夹。", "确定");
                return;
            }
            var guids = GetSelectedGuids(string.IsNullOrEmpty(_filter)? "": _filter);
            action(guids);
        }

        protected string[] GetSelectedGuids(string filter)
        {
            var folder = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
            var guids = AssetDatabase.FindAssets(filter, new[] {folder});
            return guids;
        }
        
        protected void WriteDataToFile(string saveFolder, string fileName, string text)
        {
            if (!Directory.Exists(saveFolder))
            {
                Directory.CreateDirectory(saveFolder);
            }
            var time = DateTime.Now;
            var timeMark = $"_{time:yyyy-MM-dd-HH-mm-ss}";
            fileName = Path.GetFileNameWithoutExtension(fileName) + timeMark + Path.GetExtension(fileName);
            var path = Path.Combine(saveFolder, fileName);
            using (StreamWriter sw = new StreamWriter(path, false, Encoding.UTF8))
            {
                sw.Write(text);
                sw.Close();
            }
        }
    }
}