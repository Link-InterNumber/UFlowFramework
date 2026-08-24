using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerCellStudio.Editor
{
    /// <summary>
    /// 读取并以纯文本方式显示 BundleReferenceData 分析文件。
    /// Reads and displays BundleReferenceData analysis files as plain text.
    /// </summary>
    public sealed class BundleReferenceTextViewerWindow : EditorWindow
    {
        private const string DefaultDirectory = "Analysis";

        private ListView _contentView;
        private readonly List<string> _contentLines = new List<string>();
        private Label _fileLabel;
        private string _currentFilePath;

        [MenuItem("Tools/UFlow/Bundle Reference Text Viewer")]
        private static void ShowWindow()
        {
            var window = GetWindow<BundleReferenceTextViewerWindow>();
            window.titleContent = new GUIContent("Bundle Reference Text Viewer");
            window.minSize = new Vector2(640f, 400f);
            window.Show();
        }

        private void CreateGUI()
        {
            rootVisualElement.style.flexDirection = FlexDirection.Column;

            var toolbar = new UnityEditor.UIElements.Toolbar();
            var openButton = new Button(OpenAnalysisFile)
            {
                text = "打开分析文件",
                tooltip = "读取保存的 BundleReferenceData 分析文件"
            };
            toolbar.Add(openButton);

            var clearButton = new Button(ClearContent)
            {
                text = "清空"
            };
            toolbar.Add(clearButton);

            _fileLabel = new Label("未打开文件");
            _fileLabel.style.flexGrow = 1f;
            _fileLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            toolbar.Add(_fileLabel);
            rootVisualElement.Add(toolbar);

            _contentView = new ListView
            {
                makeItem = () =>
                {
                    var label = new Label();
                    label.style.whiteSpace = WhiteSpace.Normal;
                    return label;
                },
                bindItem = (element, index) =>
                {
                    var label = (Label)element;
                    label.text = _contentLines[index];
                    label.style.unityFontStyleAndWeight = index == 0
                        ? FontStyle.Bold
                        : FontStyle.Normal;
                },
                itemsSource = _contentLines,
                fixedItemHeight = 18f,
                selectionType = SelectionType.None
            };
            _contentView.style.flexGrow = 1f;
            _contentView.style.paddingLeft = 8f;
            _contentView.style.paddingRight = 8f;
            _contentView.style.paddingTop = 8f;
            _contentView.style.paddingBottom = 8f;
            rootVisualElement.Add(_contentView);
        }

        private void OpenAnalysisFile()
        {
            var path = EditorUtility.OpenFilePanel(
                "选择 BundleReferenceData 分析文件",
                ResolveInitialDirectory(),
                "bin");
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                var lines = ReadAnalysisLines(path);
                _currentFilePath = path;
                _fileLabel.text = path;
                ShowLines(lines);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "读取分析文件失败",
                    exception.Message,
                    "确定");
            }
        }

        private static string ResolveInitialDirectory()
        {
            var directory = Path.GetFullPath(DefaultDirectory);
            return Directory.Exists(directory) ? directory : Application.dataPath;
        }

        private static List<string> ReadAnalysisLines(string path)
        {
            var lines = new List<string>();
            var bundleCount = 0;

            using (var reader = new ReferenceReader())
            {
                foreach (var data in reader.Read<BundleReferenceInfo>(path)
                             .OrderByDescending(item => item.defects.Length))
                {
                    bundleCount++;
                    lines.Add($"Bundle: {data.bundleName}");
                    lines.Add($"  依赖 Bundle ({data.bundleDependent?.Length ?? 0}):");

                    if (data.bundleDependent == null || data.bundleDependent.Length == 0)
                    {
                        lines.Add("    无");
                    }
                    else
                    {
                        for (var i = 0; i < data.bundleDependent.Length; i++)
                            lines.Add($"    - {data.bundleDependent[i]}");
                    }

                    lines.Add($"  缺陷标签 ({data.defects?.Length ?? 0}):");
                    if (data.defects == null || data.defects.Length == 0)
                    {
                        lines.Add("    无");
                    }
                    else
                    {
                        for (var i = 0; i < data.defects.Length; i++)
                            lines.Add($"    - {data.defects[i]}");
                    }

                    lines.Add(new string('-', 80));
                }
            }

            if (bundleCount == 0)
                lines.Add("分析文件中没有 Bundle 数据。");
            else
                lines.Insert(0, $"Bundle 数量: {bundleCount}\n文件: {path}\n");

            return lines;
        }

        private void ShowLines(IReadOnlyList<string> lines)
        {
            if (_contentView == null)
                return;

            _contentLines.Clear();
            if (lines != null)
            {
                for (var i = 0; i < lines.Count; i++)
                    _contentLines.Add(lines[i] ?? string.Empty);
            }

            _contentView.itemsSource = _contentLines;
            _contentView.Rebuild();
        }

        private void ClearContent()
        {
            _currentFilePath = null;
            if (_fileLabel != null)
                _fileLabel.text = "未打开文件";
            _contentLines.Clear();
            _contentView?.Rebuild();
        }

        private void OnDisable()
        {
            _currentFilePath = null;
            _contentView = null;
            _fileLabel = null;
        }
    }
}
