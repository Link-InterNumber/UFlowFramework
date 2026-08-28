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
    /// 读取并以纯文本方式显示 BundleReferenceReport 分析文件。
    /// Reads and displays BundleReferenceReport analysis files as plain text.
    /// </summary>
    public sealed class BundleReferenceTextViewerWindow : EditorWindow
    {
        private const string DefaultDirectory = "Analysis";

        private ListView _contentView;
        private readonly List<string> _contentLines = new List<string>();
        private Label _fileLabel;
        private Label _summaryLabel;
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
            rootVisualElement.style.paddingLeft = 8f;
            rootVisualElement.style.paddingRight = 8f;
            rootVisualElement.style.paddingTop = 6f;
            rootVisualElement.style.paddingBottom = 8f;
            rootVisualElement.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 1f);
            rootVisualElement.style.flexDirection = FlexDirection.Column;

            var header = new VisualElement();
            header.style.marginBottom = 6f;
            var title = new Label("Bundle 分析文本");
            title.style.fontSize = 16f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.88f, 0.92f, 0.98f, 1f);
            header.Add(title);
            var subtitle = new Label("查看序列化 Bundle 引用分析文件中的依赖和缺陷信息。");
            subtitle.style.fontSize = 11f;
            subtitle.style.marginTop = 2f;
            subtitle.style.color = new Color(0.62f, 0.66f, 0.72f, 1f);
            header.Add(subtitle);
            rootVisualElement.Add(header);

            var toolbar = new UnityEditor.UIElements.Toolbar();
            toolbar.style.paddingLeft = 6f;
            toolbar.style.paddingRight = 6f;
            toolbar.style.paddingTop = 4f;
            toolbar.style.paddingBottom = 4f;
            toolbar.style.marginBottom = 6f;
            toolbar.style.height = 28f;
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
            _fileLabel.style.marginLeft = 10f;
            _fileLabel.style.color = new Color(0.62f, 0.68f, 0.78f, 1f);
            _fileLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            toolbar.Add(_fileLabel);
            rootVisualElement.Add(toolbar);

            _summaryLabel = new Label("尚未加载分析数据");
            _summaryLabel.style.marginLeft = 8f;
            _summaryLabel.style.marginBottom = 6f;
            _summaryLabel.style.color = new Color(0.62f, 0.68f, 0.78f, 1f);
            rootVisualElement.Add(_summaryLabel);

            _contentView = new ListView
            {
                makeItem = () =>
                {
                    var label = new Label();
                    label.style.whiteSpace = WhiteSpace.PreWrap;
                    return label;
                },
                bindItem = (element, index) =>
                {
                    var label = (Label)element;
                    label.text = _contentLines[index];
                    label.style.minHeight = 18f;
                    label.style.paddingLeft = 6f;
                    label.style.paddingRight = 6f;
                    label.style.color = index == 0
                        ? new Color(0.92f, 0.94f, 1f, 1f)
                        : new Color(0.76f, 0.79f, 0.86f, 1f);
                    label.style.unityFontStyleAndWeight = index == 0
                        ? FontStyle.Bold
                        : FontStyle.Normal;
                },
                itemsSource = _contentLines,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                selectionType = SelectionType.None
            };
            _contentView.style.flexGrow = 1f;
            _contentView.style.paddingLeft = 8f;
            _contentView.style.paddingRight = 8f;
            _contentView.style.paddingTop = 8f;
            _contentView.style.paddingBottom = 8f;
            _contentView.style.backgroundColor = new Color(0.16f, 0.17f, 0.2f, 0.95f);
            _contentView.style.borderTopLeftRadius = 4f;
            _contentView.style.borderTopRightRadius = 4f;
            _contentView.style.borderBottomLeftRadius = 4f;
            _contentView.style.borderBottomRightRadius = 4f;
            rootVisualElement.Add(_contentView);
        }

        private void OpenAnalysisFile()
        {
            var path = EditorUtility.OpenFilePanel(
                "选择 BundleReferenceReport 分析文件",
                ResolveInitialDirectory(),
                "bin");
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                var lines = ReadAnalysisLines(path);
                _currentFilePath = path;
                _fileLabel.text = Path.GetFileName(path);
                _fileLabel.tooltip = path;
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
                var report = reader.ReadSingle<BundleReferenceReport>(path);
                lines.Add($"分析时间: {report.dateTime}");
                lines.Add($"Bundle 总数: {report.bundleCount}");
                lines.Add($"缺陷 Bundle 数: {report.bundleDefectReports.Count}");
                lines.Add(new string('-', 80));

                foreach (var data in report.bundleDefectReports
                             .OrderByDescending(item => item.defectLevel)
                             .ThenBy(item => item.bundleName))
                {
                    bundleCount++;
                    lines.Add($"Bundle: {data.bundleName}");
                    lines.Add($"  缺陷等级: {data.defectLevel}");
                    lines.Add($"  缺陷标签: {data.tag}");
                    lines.Add("  缺陷详情:");
                    if (string.IsNullOrEmpty(data.defectDetail))
                        lines.Add("    无");
                    else
                    {
                        foreach (var detail in data.defectDetail.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                            lines.Add($"    {detail}");
                    }

                    lines.Add(new string('-', 80));
                }
            }

            if (bundleCount == 0)
                lines.Add("分析报告中没有缺陷 Bundle 数据。");

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
            if (_summaryLabel != null)
                _summaryLabel.text = _contentLines.Count == 0
                    ? "分析文件中没有 Bundle 数据。"
                    : $"共 {_contentLines.Count:N0} 行分析内容";
        }

        private void ClearContent()
        {
            _currentFilePath = null;
            if (_fileLabel != null)
                _fileLabel.text = "未打开文件";
            if (_summaryLabel != null)
                _summaryLabel.text = "尚未加载分析数据";
            _contentLines.Clear();
            _contentView?.Rebuild();
        }

        private void OnDisable()
        {
            _currentFilePath = null;
            _contentView = null;
            _fileLabel = null;
            _summaryLabel = null;
        }
    }
}
