using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio
{
    public static class ClipInspectorRenderer
    {
        // 返回 inspector 占用的额外高度
        public static float DrawInspector(ActClipData selection, ActAsset asset, float yPos)
        {
            float inspectorHeight = 90f;
            EditorGUILayout.Space(yPos - 15);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(ActEditorWindow.HeaderWidth);
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField(selection.editorName);
                    selection.start = Mathf.Max(0, EditorGUILayout.FloatField("Start", selection.start));
                    selection.length = Mathf.Max(0.01f, EditorGUILayout.FloatField("Length", selection.length));
                    // 获取并绘制特定类型的参数
                    var fieldInfo = selection.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var field in fieldInfo)
                    {
                        if (field.Name == "id" || field.Name == "start" || field.Name == "length")
                            continue; // 跳过基础字段
                        inspectorHeight += 20f;
                        var fieldType = field.FieldType;
                        if (fieldType == typeof(int))
                        {
                            int value = (int)field.GetValue(selection);
                            int newValue = EditorGUILayout.IntField(ObjectNames.NicifyVariableName(field.Name), value);
                            if (newValue != value)
                                field.SetValue(selection, newValue);
                        }
                        else if (fieldType == typeof(float))
                        {
                            float value = (float)field.GetValue(selection);
                            float newValue = EditorGUILayout.FloatField(ObjectNames.NicifyVariableName(field.Name), value);
                            if (newValue != value)
                                field.SetValue(selection, newValue);
                        }
                        else if (fieldType == typeof(string))
                        {
                            string value = (string)field.GetValue(selection);
                            string newValue = EditorGUILayout.TextField(ObjectNames.NicifyVariableName(field.Name), value);
                            if (newValue != value)
                                field.SetValue(selection, newValue);
                        }
                        else if (fieldType == typeof(bool))
                        {
                            bool value = (bool)field.GetValue(selection);
                            bool newValue = EditorGUILayout.Toggle(ObjectNames.NicifyVariableName(field.Name), value);
                            if (newValue != value)
                                field.SetValue(selection, newValue);
                        }
                        else if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
                        {
                            UnityEngine.Object value = (UnityEngine.Object)field.GetValue(selection);
                            UnityEngine.Object newValue = EditorGUILayout.ObjectField(ObjectNames.NicifyVariableName(field.Name), value, fieldType, false);
                            if (newValue != value)
                                field.SetValue(selection, newValue);
                        }
                        else
                        {
                            EditorGUILayout.LabelField($"Unsupported field type: {fieldType.Name}");
                        }
                    }

                    if (GUILayout.Button("Delete Clip"))
                    {
                        foreach (var tk in asset.tracks)
                        {
                            if (tk.clips.Remove(selection))
                            {
                                selection = null;
                                break;
                            }
                        }
                        GUI.FocusControl(null);
                        // 调用后外层会 Repaint
                    }
                }
            }
            return inspectorHeight;
        }
    }
}