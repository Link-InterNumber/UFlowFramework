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
                        {
                            field.SetValue(selection, newValue);
                            if (newValue is AnimationClip)
                            {
                                selection.length = Mathf.Max(0.1f, ((AnimationClip)newValue).length);
                            }
                            else if (newValue is AudioClip)
                            {
                                selection.length = Mathf.Max(0.1f, ((AudioClip)newValue).length);
                            }
                            else if (newValue is GameObject go)
                            {
                                // 可选：根据预制体内容调整 length
                                var particleSystems = go.GetComponentsInChildren<ParticleSystem>();
                                float maxDuration = 0f;
                                foreach (var ps in particleSystems)
                                {
                                    var main = ps.main;
                                    maxDuration = Mathf.Max(maxDuration, main.duration + main.startDelay.constant);
                                }
                                selection.length = Mathf.Max(0.1f, maxDuration);
                            }
                        }
                    }
                    else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(AssetPath<>))
                    {
                        inspectorHeight += 20f;
                        var genericArg = fieldType.GetGenericArguments()[0];
                        var assetPathObj = field.GetValue(selection);
                        var assetPathField = fieldType.GetField("assetPath", BindingFlags.Public | BindingFlags.Instance);
                        
                        string currentPath = (string)assetPathField.GetValue(assetPathObj);
                        var currentAsset = AssetDatabase.LoadAssetAtPath(currentPath, genericArg);
                        var Obj = EditorGUILayout.ObjectField($"{genericArg.Name}", currentAsset, genericArg, false);
                        var newPath = Obj ? AssetDatabase.GetAssetPath(Obj) : string.Empty;
                        if (newPath != currentPath)
                        {
                            assetPathField.SetValue(assetPathObj, newPath);
                            field.SetValue(selection, assetPathObj);
                            if (Obj is AnimationClip animClip)
                            {
                                selection.length = Mathf.Max(0.1f, animClip.length);
                            }
                            else if (Obj is AudioClip audioClip)
                            {
                                selection.length = Mathf.Max(0.1f, audioClip.length);
                            }
                            else if (Obj is GameObject go)
                            {
                                // 可选：根据预制体内容调整 length
                                var particleSystems = go.GetComponentsInChildren<ParticleSystem>();
                                float maxDuration = 0f;
                                foreach (var ps in particleSystems)
                                {
                                    var main = ps.main;
                                    maxDuration = Mathf.Max(maxDuration, main.duration + main.startDelay.constant);
                                }
                                selection.length = Mathf.Max(0.1f, maxDuration);
                            }
                        }
                        EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(field.Name), newPath);
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