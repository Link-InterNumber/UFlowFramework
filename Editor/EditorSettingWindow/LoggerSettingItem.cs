#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text;
using Color = UnityEngine.Color;

namespace PowerCellStudio
{
    public class LoggerSettingItem : IEditorSettingWindowItem
    {
        private class LoggerFormat
        {
            public string LogType;
            public bool GenericArgument;
            public Color LogColor;
            public string Loglabel; 
            public Color  WarningColor; 
            public string  Warninglabel; 
            public Color  ErrorColor; 
            public string  Errorlabel; 
            public Color  ExceptionColor; 
            public string  Exceptionlabel;
        }

        public string itemName => "LoggerSetting";

        private List<LoggerFormat> _loggerFormats;

        private Object _csvTextAsset;
        
        public void InitSave()
        {
            _loggerFormats = new List<LoggerFormat>();
        }

        public void OnDestroy()
        {
            _loggerFormats = null;
        }

        public void OnGUI(EditorWindow window)
        {
            _csvTextAsset = EditorGUILayout.ObjectField(_csvTextAsset, typeof(TestAsset), true);
            if (_csvTextAsset && _loggerFormats.Length == 0)
            {
                ReadCSV(TestAsset);
            }
            else if (_loggerFormats.Length > 0)
            {
                _loggerFormats.Clear();
            }
            if (_loggerFormats == null || _loggerFormats.Length == 0) return;
            for (var i= 0; i < _loggerFormats.Length; i ++)
            {
                var format = _loggerFormats[i];
                EditorGUILayout.BeginHorizontal()
                format.LogType = EditorGUILayout.TextField("Log Type", format.LogType);
                format.GenericArgument = EditorGUILayout.Toggle("GenericArgument", format.GenericArgument);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal()
                format.LogColor = EditorGUILayout.ColorField("Log Color", format.LogColor);
                format.Loglabel = EditorGUILayout.TextField("Log Label", format.Loglabel);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal()
                format.WarningColor = EditorGUILayout.ColorField("Warning Color", format.WarningColor);
                format.Warninglabel = EditorGUILayout.TextField("Warning Label", format.Warninglabel);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal()
                format.ErrorColor = EditorGUILayout.ColorField("Error Color", format.ErrorColor);
                format.Errorlabel = EditorGUILayout.TextField("Error Label", format.Errorlabel);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal()
                format.ExceptionColor = EditorGUILayout.ColorField("Exception Color", format.ExceptionColor);
                format.Exceptionlabel = EditorGUILayout.TextField("Exception Label", format.Exceptionlabel);
                EditorGUILayout.EndHorizontal();
                if (GUILayout.Button("-"))
                {
                    _loggerFormats.RemoveAt(i);
                    break; // 退出循环以避免修改列表时出错
                }
            }
            // TODO 添加加号按钮和减号按钮，可以对数据进行赠删
            if (GUILayout.Button("+"))
            {
                _loggerFormats.Add(new LoggerFormat());
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
        }

        private void ReadCSV(TestAsset csv)
        {
            _loggerFormats.Clear();
            string csvText = csv.text;
            // TODO 根据csv生成List<LoggerSettingItem>
            string[] lines = csvText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < lines.Length; i++) // 从1开始跳过表头
            {
                var parts = lines[i].Split(',');
                if (parts.Length == 10)
                {
                    var format = new LoggerFormat
                    {
                        LogType = parts[0].Trim(),
                        GenericArgument = parts[1].Trim() == "True",
                        LogColor = ColorExtension.ParseHex(parts[2].Trim()),
                        Loglabel = parts[3].Trim(),
                        WarningColor = ColorExtension.ParseHex(parts[4].Trim()),
                        Warninglabel = parts[5].Trim(),
                        ErrorColor = ColorExtension.ParseHex(parts[6].Trim()),
                        Errorlabel = parts[7].Trim(),
                        ExceptionColor = ColorExtension.ParseHex(parts[8].Trim()),
                        Exceptionlabel = parts[9].Trim()
                    };
                    _loggerFormats.Add(format);
                }
            }
        }

        private void SaveCSV(List<LoggerSettingItem> logFormats)
        {
            // TODO 根据List<LoggerSettingItem>生成csv并保存
            StringBuilder csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("LogType, GenericArgument,Log Color,Log Label,Warning Color,Warning Label,Error Color,Error Label,Exception Color,Exception Label");

            foreach (var format in logFormats)
            {
                csvBuilder.AppendLine($"{format.LogType}, {format.GenericArgument},{ColorToHex(format.LogColor)},{format.Loglabel}," +
                                      $"{ColorToHex(format.WarningColor)},{format.Warninglabel}," +
                                      $"{ColorToHex(format.ErrorColor)},{format.Errorlabel}," +
                                      $"{ColorToHex(format.ExceptionColor)},{format.Exceptionlabel}");
            }

            string csvFilePath = AssetDatabase.GetAssetPath(_csvTextAsset);
            // string savePath = Path.Combine(Path.GetDirectoryName(csvFilePath), "UpdatedLoggerSettings.csv");

            File.WriteAllText(csvFilePath, csvBuilder.ToString());
        }

        private void GenerateScript()
        {
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

            using (var reader = new StreamReader(csvFilePath))
            using (var writer = new StreamWriter(outputFilePath))
            {
                // 写入文件头部
                writer.WriteLine("using System;");
                writer.WriteLine("using UnityEngine;");
                writer.WriteLine("");
                writer.WriteLine("namespace PowerCellStudio");
                writer.WriteLine("{");

                for (var i= 0; i < _loggerFormats.Length; i ++)
                {
                    var format = _loggerFormats[i];
                    string logType = format.LogType;
                    string logColor = format.LogColor.FormatHex();
                    string logLabel = format.Loglabel;
                    string warningColor = format.WarningColor.FormatHex();
                    string warningLabel = format.WarningLabel;
                    string errorColor = format.ErrorColor.FormatHex();
                    string errorLabel = format.errorLabel;
                    string exceptionColor = format.ExceptionColor.FormatHex();
                    string exceptionLabel = format.ExceptionLabel;

                    writer.WriteLine($"    public static class {logType}Log");
                    writer.WriteLine("    {");


                    writer.WriteLine($"        public static void Log(object message)");
                    writer.WriteLine("        {");
                    writer.WriteLine($"            if(Application.isPlaying && !ApplicationManager.enableLog) return;");
                    writer.WriteLine($"            Debug.Log($\"[<color={logColor}>{logType} {logLabel}</color>] \{message\}\");");
                    writer.WriteLine("        }");
                    writer.WriteLine("");
                    writer.WriteLine($"        public static void LogWarning(object message)");
                    writer.WriteLine("        {");
                    writer.WriteLine($"            if(Application.isPlaying && !ApplicationManager.enableWarning) return;");
                    writer.WriteLine($"            Debug.LogWarning($\"[<color={warningColor}>{logType} {warningLabel}</color>] \{message\}\");");
                    writer.WriteLine("        }");
                    writer.WriteLine("");
                    writer.WriteLine($"        public static void LogError(object message)");
                    writer.WriteLine("        {");
                    writer.WriteLine($"            if(Application.isPlaying && !ApplicationManager.enableError) return;");
                    writer.WriteLine($"            Debug.LogError($\"[<color={errorColor}>{logType} {errorLabel}</color>] \{message\}\");");
                    writer.WriteLine("        }");
                    writer.WriteLine("");
                    writer.WriteLine($"        public static Exception Exception(object message)");
                    writer.WriteLine("        {");
                    writer.WriteLine($"            if(Application.isPlaying && !ApplicationManager.enableError) return null;");
                    writer.WriteLine($"            return new Exception($\"[<color={exceptionColor}>{logType} {exceptionLabel}</color>] \{message\}\");");
                    writer.WriteLine("        }");

                    if (format.GenericArgument)
                    {
                        writer.WriteLine($"        public static void Log<T>(object message)");
                        writer.WriteLine("        {");
                        writer.WriteLine($"            if(Application.isPlaying && !ApplicationManager.enableLog) return;");
                        writer.WriteLine($"            Debug.Log($\"[<color={logColor}>{logType} {logLabel}</color>:\{typeof(T).Name\}] \{message\}\");");
                        writer.WriteLine("        }");
                        writer.WriteLine("");
                        writer.WriteLine($"        public static void LogWarning<T>(object message)");
                        writer.WriteLine("        {");
                        writer.WriteLine($"            if(Application.isPlaying && !ApplicationManager.enableWarning) return;");
                        writer.WriteLine($"            Debug.LogWarning($\"[<color={warningColor}>{logType} {warningLabel}</color>:\{typeof(T).Name\}] \{message\}\");");
                        writer.WriteLine("        }");
                        writer.WriteLine("");
                        writer.WriteLine($"        public static void LogError<T>(object message)");
                        writer.WriteLine("        {");
                        writer.WriteLine($"            if(Application.isPlaying && !ApplicationManager.enableError) return;");
                        writer.WriteLine($"            Debug.LogError($\"[<color={errorColor}>{logType} {errorLabel}</color>:\{typeof(T).Name\}] \{message\}\");");
                        writer.WriteLine("        }");
                        writer.WriteLine("");
                        writer.WriteLine($"        public static Exception Exception<T>(object message)");
                        writer.WriteLine("        {");
                        writer.WriteLine($"            if(Application.isPlaying && !ApplicationManager.enableError) return null;");
                        writer.WriteLine($"            return new Exception($\"[<color={exceptionColor}>{logType} {exceptionLabel}</color>:\{typeof(T).Name\}] \{message\}\");");
                        writer.WriteLine("        }");
                    }


                    writer.WriteLine("    }");
                    writer.WriteLine("");
                }

                writer.WriteLine("}");
            }
        }
    }
}