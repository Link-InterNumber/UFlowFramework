using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio
{
    public abstract class FolderEditorWindow : EditorWindow
    {
        protected virtual void OnGUI()
        {
            if (Selection.assetGUIDs.Length <= 0)
            {
                GUILayout.Label("请先选择一个文件夹!!! ");
            }
            else
            {
                var folder = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
                if (!Directory.Exists(folder))
                {
                    GUILayout.Label("请先选择一个文件夹!!! ");
                    return;
                }
                GUILayout.Label($"当前选中的文件夹：{folder}");
            }
        }
        
        protected abstract string _filter { get; }
        
        public delegate void DealWithFileHandle(string[] guids);
        protected void DrawButton(string buttonName, DealWithFileHandle action)
        {
            if (!GUILayout.Button(buttonName)) return;
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