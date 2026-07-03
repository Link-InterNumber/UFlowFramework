#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using Color = UnityEngine.Color;
using Object = UnityEngine.Object;

namespace PowerCellStudio.Editor
{
    public class LoggerSettingItem : IEditorSettingWindowItem
    {
        private class LoggerFormat
        {
            public string logType;
            public bool genericArgument;
            public bool enableLog = true;
            public bool enableWarning = true;
            public bool enableError = true;
            public Color logColor;
            public string loglabel;
            public Color  warningColor; 
            public string  warningLabel;
            public Color  errorColor; 
            public string  ErrorLabel; 
            public Color  ExceptionColor; 
            public string  ExceptionLabel;
        }

        public string itemName => "LoggerSetting";

        private List<LoggerFormat> _loggerFormats;

        private Object _csvTextAsset;
        
        public void InitSave()
        {
            _loggerFormats = new List<LoggerFormat>();
            var csvTextAssetPath = EditorPrefs.GetString("LogFilePath", string.Empty);
            if (string.IsNullOrEmpty(csvTextAssetPath)) return;
            _csvTextAsset = AssetDatabase.LoadAssetAtPath(csvTextAssetPath, typeof(TextAsset));
        }

        public void OnDestroy()
        {
            _loggerFormats = null;
        }

        public void OnGUI(EditorWindow window)
        {
            _csvTextAsset = EditorGUILayout.ObjectField(_csvTextAsset, typeof(TextAsset), true);
            if (_csvTextAsset && _loggerFormats.Count == 0)
            {
                ReadCSV(_csvTextAsset as TextAsset);
            }
            if (_csvTextAsset == null)
            {
                if (GUILayout.Button("Create Logger Asset"))
                {
                    CreateLoggerAsset();
                }
                return;
            }
            EditorGUILayout.Space(10);
            for (var i= 0; i < _loggerFormats.Count; i ++)
            {
                var format = _loggerFormats[i];
                EditorGUILayout.BeginHorizontal();
                format.logType = EditorGUILayout.TextField("Log Type", format.logType);
                format.genericArgument = EditorGUILayout.Toggle("GenericArgument", format.genericArgument);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                format.logColor = EditorGUILayout.ColorField("Log Color", format.logColor);
                format.loglabel = EditorGUILayout.TextField("Log Label", format.loglabel);
                format.enableLog = EditorGUILayout.Toggle("Enable Log", format.enableLog);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                format.warningColor = EditorGUILayout.ColorField("Warning Color", format.warningColor);
                format.warningLabel = EditorGUILayout.TextField("Warning Label", format.warningLabel);
                format.enableWarning = EditorGUILayout.Toggle("Enable _loggerFormats", format.enableWarning);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                format.errorColor = EditorGUILayout.ColorField("Error Color", format.errorColor);
                format.ErrorLabel = EditorGUILayout.TextField("Error Label", format.ErrorLabel);
                format.enableError = EditorGUILayout.Toggle("Enable Error", format.enableError);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                format.ExceptionColor = EditorGUILayout.ColorField("Exception Color", format.ExceptionColor);
                format.ExceptionLabel = EditorGUILayout.TextField("Exception Label", format.ExceptionLabel);
                format.enableError = EditorGUILayout.Toggle("Enable Exception", format.enableError);
                EditorGUILayout.EndHorizontal();
                if (GUILayout.Button("-"))
                {
                    _loggerFormats.RemoveAt(i);
                    break; // 退出循环以避免修改列表时出错
                }
                EditorGUILayout.Space();
            }
            // TODO 添加加号按钮和减号按钮，可以对数据进行赠删
            if (GUILayout.Button("+"))
            {
                _loggerFormats.Add(new LoggerFormat()
                {
                    logType = "Custom",
                    logColor = Color.white,
                    loglabel = "Log",
                    warningColor = Color.yellow,
                    warningLabel = "Warning",
                    errorColor = Color.red,
                    ErrorLabel = "Error",
                    ExceptionColor = Color.red,
                    ExceptionLabel = "Exception",
                });
            }

            if (GUILayout.Button("Create Cs Files"))
            {
                GenerateScript();
                SaveData();
                AssetDatabase.Refresh();
            }
        }

        public void SaveData()
        {
            SaveCSV(_loggerFormats);
            string csvFilePath = AssetDatabase.GetAssetPath(_csvTextAsset);
            if (string.IsNullOrEmpty(csvFilePath)) return;
            EditorPrefs.SetString("LogFilePath", csvFilePath);
        }
        
        private static void CreateLoggerAsset()
        {
            StringBuilder csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("Log Type,Generic Argument,Enable Log,Enable Warning,Enable Error,Log Color,Log Label,Warning Color,Warning Label,Error Color,Error Label,Exception Color,Exception Label");
            string outputFilePath = EditorUtility.SaveFilePanelInProject(
                "Save Logger Asset",
                "NewLogger",
                "csv",
                "Select save location"
            );
            if (string.IsNullOrEmpty(outputFilePath)) return;
            File.WriteAllText(outputFilePath, csvBuilder.ToString());
        }

        private void ReadCSV(TextAsset csv)
        {
            _loggerFormats.Clear();
            string csvText = csv.text;
            // TODO 根据csv生成List<LoggerSettingItem>
            string[] lines = csvText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < lines.Length; i++) // 从1开始跳过表头
            {
                var parts = lines[i].Split(',');
                if (parts.Length == 13)
                {
                    var format = new LoggerFormat
                    {
                        logType = parts[0].Trim(),
                        genericArgument = parts[1].Trim() == "True",
                        enableLog = parts[2].Trim() == "True",
                        enableWarning = parts[3].Trim() == "True",
                        enableError = parts[4].Trim() == "True",
                        logColor = ColorExtension.ParseHex(parts[5].Trim()),
                        loglabel = parts[6].TrimEnd('\r', '\n'),
                        warningColor = ColorExtension.ParseHex(parts[7].Trim()),
                        warningLabel = parts[8].TrimEnd('\r', '\n'),
                        errorColor = ColorExtension.ParseHex(parts[9].Trim()),
                        ErrorLabel = parts[10].TrimEnd('\r', '\n'),
                        ExceptionColor = ColorExtension.ParseHex(parts[11].Trim()),
                        ExceptionLabel = parts[12].TrimEnd('\r', '\n')
                    };
                    _loggerFormats.Add(format);
                }
            }
        }

        private void SaveCSV(List<LoggerFormat> logFormats)
        {
            if (_csvTextAsset == null) return;
            // TODO 根据List<LoggerSettingItem>生成csv并保存
            StringBuilder csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("Log Type,Generic Argument,Enable Log,Enable Warning,Enable Error,Log Color,Log Label,Warning Color,Warning Label,Error Color,Error Label,Exception Color,Exception Label");

            foreach (var format in logFormats)
            {
                csvBuilder.Append($"{format.logType},");
                csvBuilder.Append($"{format.genericArgument},");
                csvBuilder.Append($"{format.enableLog},");
                csvBuilder.Append($"{format.enableWarning},");
                csvBuilder.Append($"{format.enableError},");
                csvBuilder.Append($"{ColorExtension.FormatHex(format.logColor)},");
                csvBuilder.Append($"{format.loglabel},");
                csvBuilder.Append($"{ColorExtension.FormatHex(format.warningColor)},");
                csvBuilder.Append($"{format.warningLabel},");
                csvBuilder.Append($"{ColorExtension.FormatHex(format.errorColor)},");
                csvBuilder.Append($"{format.ErrorLabel},");
                csvBuilder.Append($"{ColorExtension.FormatHex(format.ExceptionColor)},");
                csvBuilder.Append($"{format.ExceptionLabel}");
                csvBuilder.Append("\n");
            }

            string csvFilePath = AssetDatabase.GetAssetPath(_csvTextAsset);
            // string savePath = Path.Combine(Path.GetDirectoryName(csvFilePath), "UpdatedLoggerSettings.csv");

            File.WriteAllText(csvFilePath, csvBuilder.ToString());
        }

        private void GenerateScript()
        {
            if (_csvTextAsset == null) return;
            string csvFilePath = AssetDatabase.GetAssetPath(_csvTextAsset);
            string defaultPath = Path.GetDirectoryName(csvFilePath);
            string outputFilePath = EditorUtility.SaveFilePanelInProject(
                "Save Logger Script",
                "Logger",
                "cs",
                "Select save location",
                defaultPath
            );
            if (string.IsNullOrEmpty(outputFilePath)) return;

            using (var csWriter = new CsWriter())
            {
                csWriter.WriteUsing("System", "UnityEngine")
                    .Space()
                    .WriteLine("namespace PowerCellStudio")
                    .StartWriteBody();

                for (var i = 0; i < _loggerFormats.Count; i++)
                {
                    var format = _loggerFormats[i];
                    string logType = format.logType;
                    string logColor = format.logColor.FormatHex();
                    string logLabel = format.loglabel;
                    string warningColor = format.warningColor.FormatHex();
                    string warningLabel = format.warningLabel;
                    string errorColor = format.errorColor.FormatHex();
                    string errorLabel = format.ErrorLabel;
                    string exceptionColor = format.ExceptionColor.FormatHex();
                    string exceptionLabel = format.ExceptionLabel;

                    csWriter.WriteLine($"public static class {logType}Logger")
                        .StartWriteBody()
                        .WriteField(CsWriter.FieldSign.Public, "bool", "enableLog", format.enableLog.ToString().ToLowerInvariant(), CsWriter.FieldSign2.Static)
                        .WriteField(CsWriter.FieldSign.Public, "bool", "enableWarning", format.enableWarning.ToString().ToLowerInvariant(), CsWriter.FieldSign2.Static)
                        .WriteField(CsWriter.FieldSign.Public, "bool", "enableError", format.enableError.ToString().ToLowerInvariant(), CsWriter.FieldSign2.Static)
                        .Space();

                    csWriter.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Static, "void", "Log", "object message")
                        .WriteLine("if (!enableLog) return;")
                        .WriteLine("if (Application.isPlaying && !ApplicationManager.enableLog) return;")
                        .WriteLine($"Debug.Log($\"[<color={logColor}>{logType} {logLabel}</color>] {{message}}\");")
                        .EndWriteMethod();

                    csWriter.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Static, "void", "LogWarning", "object message")
                        .WriteLine("if (!enableWarning) return;")
                        .WriteLine("if (Application.isPlaying && !ApplicationManager.enableWarning) return;")
                        .WriteLine($"Debug.LogWarning($\"[<color={warningColor}>{logType} {warningLabel}</color>] {{message}}\");")
                        .EndWriteMethod();

                    csWriter.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Static, "void", "LogError", "object message")
                        .WriteLine("if (!enableError) return;")
                        .WriteLine("if (Application.isPlaying && !ApplicationManager.enableError) return;")
                        .WriteLine($"Debug.LogError($\"[<color={errorColor}>{logType} {errorLabel}</color>] {{message}}\");")
                        .EndWriteMethod();

                    csWriter.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Static, "void", "ThrowException", "object message")
                        .WriteLine("if (!enableError) return;")
                        .WriteLine("if (Application.isPlaying && !ApplicationManager.enableError) return;")
                        .WriteLine($"throw new Exception($\"[<color={exceptionColor}>{logType} {exceptionLabel}</color>] {{message}}\");")
                        .EndWriteMethod();

                    if (format.genericArgument)
                    {
                        csWriter.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Static, "void", "Log<T>", "object message")
                            .WriteLine("if (!enableLog) return;")
                            .WriteLine("if (Application.isPlaying && !ApplicationManager.enableLog) return;")
                            .WriteLine($"Debug.Log($\"[<color={logColor}>{logType} {logLabel}</color>:{{typeof(T).Name}}] {{message}}\");")
                            .EndWriteMethod();

                        csWriter.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Static, "void", "LogWarning<T>", "object message")
                            .WriteLine("if (!enableWarning) return;")
                            .WriteLine("if (Application.isPlaying && !ApplicationManager.enableWarning) return;")
                            .WriteLine($"Debug.LogWarning($\"[<color={warningColor}>{logType} {warningLabel}</color>:{{typeof(T).Name}}] {{message}}\");")
                            .EndWriteMethod();

                        csWriter.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Static, "void", "LogError<T>", "object message")
                            .WriteLine("if (!enableError) return;")
                            .WriteLine("if (Application.isPlaying && !ApplicationManager.enableError) return;")
                            .WriteLine($"Debug.LogError($\"[<color={errorColor}>{logType} {errorLabel}</color>:{{typeof(T).Name}}] {{message}}\");")
                            .EndWriteMethod();

                        csWriter.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Static, "void", "ThrowException<T>", "object message")
                            .WriteLine("if (!enableError) return;")
                            .WriteLine("if (Application.isPlaying && !ApplicationManager.enableError) return;")
                            .WriteLine($"throw new Exception($\"[<color={exceptionColor}>{logType} {exceptionLabel}</color>:{{typeof(T).Name}}] {{message}}\");")
                            .EndWriteMethod();
                    }

                    csWriter.EndWriteBody()
                        .Space();
                }

                csWriter.EndWriteBody();
                File.WriteAllText(outputFilePath, csWriter.ToString());
            }
        }
    }
}
#endif