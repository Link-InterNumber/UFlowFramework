using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio.Editor
{
    public partial class AdvancedUICodeGeneratorWindow
    {
        private ScriptFileInfo CreateScriptFileInfo(string className, string content, bool openAfterGenerate)
        {
            return new ScriptFileInfo($"{_outputFolder}/{className}.cs", content, openAfterGenerate);
        }

        private void TryWriteScriptFiles(List<ScriptFileInfo> scriptFiles)
        {
            _pendingScriptFiles = scriptFiles;
            var existingFiles = scriptFiles
                .Select(file => file.AssetPath)
                .Where(File.Exists)
                .ToList();

            if (existingFiles.Count > 0)
            {
                ConfirmEditorWindow.ShowWindow(
                    () =>
                    {
                        foreach (var file in existingFiles)
                        {
                            AssetDatabase.DeleteAsset(file);
                        }
                        WritePendingScriptFiles();
                    },
                    () =>
                    {
                        _pendingScriptFiles = null;
                        _pendingBindInfo = null;
                        ClearPendingBindInfo();
                        Debug.Log("UI script generation canceled because target files already exist.");
                    },
                    "Overwrite UI Scripts",
                    $"Target folder already contains these scripts:\n{string.Join("\n", existingFiles)}\n\nDelete old files and generate new scripts?");
                return;
            }

            WritePendingScriptFiles();
        }

        private void WritePendingScriptFiles()
        {
            if (_pendingScriptFiles == null) return;

            ScriptFileInfo openFile = null;
            foreach (var scriptFile in _pendingScriptFiles)
            {
                WriteScriptFile(scriptFile);
                if (scriptFile.openAfterGenerate) openFile = scriptFile;
            }

            SavePendingBindInfo(_pendingBindInfo);
            AssetDatabase.Refresh();
            if (openFile != null)
            {
                var scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(openFile.AssetPath);
                if (scriptAsset != null) AssetDatabase.OpenAsset(scriptAsset);
            }

            if (_pendingBindInfo != null)
            {
                if (TryBindPrefabComponent(_pendingBindInfo, false))
                {
                    ClearPendingBindInfo();
                }
            }

            Debug.Log($"UI scripts generated in: {_outputFolder}");
            _pendingScriptFiles = null;
            _pendingBindInfo = null;
        }

        private void WriteScriptFile(ScriptFileInfo scriptFile)
        {
            File.WriteAllText(scriptFile.AssetPath, scriptFile.content, Encoding.UTF8);
        }
    }
}