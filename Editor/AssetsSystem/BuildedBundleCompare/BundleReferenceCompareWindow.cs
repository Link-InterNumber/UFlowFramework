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
    /// 对比已构建 AssetBundle 与当前工程 Bundle 配置的编辑器窗口。
    /// </summary>
    public sealed class BundleReferenceCompareWindow : EditorWindow
    {
        [MenuItem("Tools/UFlow/Bundle Reference Compare", priority = 101)]
        private static void ShowWindow()
        {
            var window = GetWindow<BundleReferenceCompareWindow>();
            window.titleContent = new GUIContent("Bundle Reference Compare");
            window.Show();
        }

        private TextField _buildDirectoryField;
        private TextField _manifestNameField;
        private TextField _baselineFileField;
        private Label _summaryLabel;
        private ListView _bundleList;
        private VisualElement _bundleListResizer;
        private ScrollView _detailView;
        private readonly List<BundleCompareItem> _items = new List<BundleCompareItem>();
        private readonly Dictionary<string, BuiltBundleData> _builtData = new Dictionary<string, BuiltBundleData>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _currentBundleNames;
        private List<BundleBuildBaselineInfo> _baseline;
        private string _comparisonDirectory;
        private string _comparisonManifestName;
        private long _totalBuiltSize;
        
        private void CreateGUI()
        {
            rootVisualElement.style.paddingLeft = 8f;
            rootVisualElement.style.paddingRight = 8f;
            rootVisualElement.style.paddingTop = 6f;
            rootVisualElement.style.paddingBottom = 8f;
            rootVisualElement.style.backgroundColor = BundleReferenceCompareSettings.RootBackground;

            var header = new VisualElement();
            header.style.marginBottom = 6f;
            var title = new Label(BundleReferenceCompareSettings.HeaderTitle);
            title.style.fontSize = 16f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.88f, 0.92f, 0.98f, 1f);
            header.Add(title);
            var subtitle = new Label(BundleReferenceCompareSettings.HeaderSubtitle);
            subtitle.style.fontSize = 11f;
            subtitle.style.color = new Color(0.62f, 0.66f, 0.72f, 1f);
            subtitle.style.marginTop = 2f;
            header.Add(subtitle);
            rootVisualElement.Add(header);

            var toolbar = new UnityEditor.UIElements.Toolbar();
            toolbar.style.paddingLeft = 6f;
            toolbar.style.paddingRight = 6f;
            toolbar.style.marginBottom = 6f;
            var historyPath = EditorPrefs.GetString(BundleReferenceCompareSettings.HistoryBuildDirectoryKey, string.Empty);
            _buildDirectoryField = new TextField("已构建目录")
            {
                value = historyPath,
            };
            _buildDirectoryField.style.minWidth = 300;
            
            toolbar.Add(_buildDirectoryField);
            var browseButton = new Button(BrowseBuildDirectory) { text = "选择" };
            toolbar.Add(browseButton);
            
            var manifestName = EditorPrefs.GetString(BundleReferenceCompareSettings.HistoryManifestNameKey, string.Empty);
            _manifestNameField = new TextField("Manifest 名称") { value = manifestName };
            _manifestNameField.style.width = 180;
            toolbar.Add(_manifestNameField);
            toolbar.Add(new Button(Compare) { text = "开始对比" });
            toolbar.Add(new Button(Clear) { text = "清空" });

            rootVisualElement.Add(toolbar);

            var baselineToolbar = new UnityEditor.UIElements.Toolbar();
            baselineToolbar.style.paddingLeft = 6f;
            baselineToolbar.style.paddingRight = 6f;
            baselineToolbar.style.marginBottom = 6f;
            _baselineFileField = new TextField("基准文件")
            {
                value = EditorPrefs.GetString(BundleReferenceCompareSettings.HistoryBaselineFileKey, string.Empty),
            };
            _baselineFileField.style.minWidth = 260f;
            baselineToolbar.Add(_baselineFileField);
            baselineToolbar.Add(new Button(BrowseBaselineFile) { text = "添加基准" });
            baselineToolbar.Add(new Button(SaveCurrentAsBaseline) { text = "保存为基准" });
            rootVisualElement.Add(baselineToolbar);

            _summaryLabel = new Label(BundleReferenceCompareSettings.InitialSummary);
            _summaryLabel.style.paddingLeft = 8;
            _summaryLabel.style.paddingRight = 8;
            _summaryLabel.style.paddingTop = 6;
            _summaryLabel.style.paddingBottom = 6;
            _summaryLabel.style.marginBottom = 6f;
            _summaryLabel.style.backgroundColor = BundleReferenceCompareSettings.SummaryBackground;
            _summaryLabel.style.color = BundleReferenceCompareSettings.SummaryTextColor;
            rootVisualElement.Add(_summaryLabel);

            var content = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };
            content.style.minHeight = 260f;
            _bundleList = new ListView(_items, 24, MakeBundleItem, BindBundleItem)
            {
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                selectionType = SelectionType.Single
            };
            _bundleList.style.flexShrink = 0f;
            _bundleList.style.width = 240;
            _bundleList.style.maxWidth = 600f;
            _bundleList.style.minWidth = 180f;
            _bundleList.style.backgroundColor = BundleReferenceCompareSettings.PanelBackground;
            _bundleList.selectionChanged += OnBundleSelected;
            content.Add(_bundleList);

            _bundleListResizer = CreateBundleListResizer(content);
            content.Add(_bundleListResizer);

            _detailView = new ScrollView(ScrollViewMode.Vertical);
            _detailView.style.flexGrow = 1;
            _detailView.style.minWidth = 300f;
            _detailView.style.marginLeft = 8f;
            _detailView.style.paddingTop = 8f;
            _detailView.style.paddingLeft = 12;
            _detailView.style.paddingRight = 12;
            _detailView.style.backgroundColor = BundleReferenceCompareSettings.PanelBackground;
            content.Add(_detailView);
            rootVisualElement.Add(content);
        }

        private VisualElement CreateBundleListResizer(VisualElement content)
        {
            var resizer = new VisualElement
            {
                name = "BundleListPanelResizer",
                tooltip = "拖拽调整 Bundle 列表宽度"
            };
            resizer.style.width = 8f;
            resizer.style.minWidth = 8f;
            resizer.style.maxWidth = 8f;
            resizer.style.flexShrink = 0f;
            resizer.style.backgroundColor = new Color(0.22f, 0.25f, 0.3f, 1f);

            resizer.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0)
                    return;

                resizer.CaptureMouse();
                evt.StopPropagation();
            });

            resizer.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (!resizer.HasMouseCapture() || _bundleList == null)
                    return;

                var width = evt.mousePosition.x - content.worldBound.x;
                _bundleList.style.width = Mathf.Clamp(width, 180f, 600f);
                evt.StopPropagation();
            });

            resizer.RegisterCallback<MouseUpEvent>(evt =>
            {
                if (evt.button != 0)
                    return;

                if (resizer.HasMouseCapture())
                    resizer.ReleaseMouse();
                evt.StopPropagation();
            });

            return resizer;
        }

        private void OnBundleSelected(IEnumerable<object> obj)
        {
            var data = obj.FirstOrDefault(o => o is BundleCompareItem) as BundleCompareItem;
            if (data == null)
                return;

            BundleReferenceComparisonService.Analyze(
                data,
                _baseline,
                _builtData);
            ShowDetails(data);
        }

        private VisualElement MakeBundleItem()
        {
            var itemRoot = new VisualElement();
            itemRoot.style.flexDirection = FlexDirection.Row;
            itemRoot.style.minHeight = 42f;
            itemRoot.style.paddingTop = 4f;
            itemRoot.style.paddingBottom = 12f;
            itemRoot.style.paddingLeft = 4f;
            itemRoot.style.paddingRight = 4f;
            itemRoot.style.marginBottom = 2f;
            itemRoot.style.position = Position.Relative;

            var sizeLabel = new Label
            {
                name = "BundleSizeLabel"
            };
            sizeLabel.style.width = 76f;
            sizeLabel.style.flexShrink = 0f;
            sizeLabel.style.unityTextAlign = TextAnchor.UpperRight;
            sizeLabel.style.paddingRight = 6f;
            sizeLabel.style.paddingTop = 3f;
            sizeLabel.style.color = BundleReferenceCompareSettings.MutedTextColor;
            itemRoot.Add(sizeLabel);

            var content = new VisualElement
            {
                name = "BundleContent"
            };
            content.style.flexGrow = 1f;
            content.style.minWidth = 0f;

            var nameLabel = new Label
            {
                name = "BundleNameLabel"
            };
            nameLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            nameLabel.style.overflow = Overflow.Hidden;
            nameLabel.style.textOverflow = TextOverflow.Ellipsis;
            nameLabel.style.color = BundleReferenceCompareSettings.TitleColor;
            content.Add(nameLabel);

            itemRoot.Add(content);

            var sizeBar = new ProgressBar
            {
                name = "BundleSizeProgressBar",
                title = string.Empty,
                lowValue = 0f,
                highValue = 100f
            };
            sizeBar.style.position = Position.Absolute;
            sizeBar.style.left = 0f;
            sizeBar.style.right = 0f;
            sizeBar.style.bottom = 0f;
            sizeBar.style.height = 8f;
            sizeBar.style.backgroundColor = new Color(0f, 0f, 0f, 0.08f);

            var progress = sizeBar.Q<VisualElement>(className: "unity-progress-bar__progress");
            if (progress != null)
                progress.style.backgroundColor = new Color(0.18f, 0.58f, 0.95f, 0.45f);

            itemRoot.Add(sizeBar);

            return itemRoot;
        }

        private void BindBundleItem(VisualElement element, int index)
        {
            var item = _items[index];
            AnalyzeBundleItem(item);
            var sizeLabel = element.Q<Label>("BundleSizeLabel");
            var nameLabel = element.Q<Label>("BundleNameLabel");
            var sizeBar = element.Q<ProgressBar>("BundleSizeProgressBar");
            var percentage = _totalBuiltSize > 0
                ? item.builtSize * 100f / _totalBuiltSize
                : 0f;

            sizeLabel.text = BundleReferenceCompareUtility.FormatSize(item.builtSize);
            var baselineStatusText = _baseline != null && item.hasBaseline && item.baselineStatus != BundleCompareStatus.Same
                ? $"[对比基准{GetStatusText(item.baselineStatus)}]"
                : string.Empty;
            var editorStatusText = item.status != BundleCompareStatus.Same
                ? $"[当前配置{GetStatusText(item.status)}]"
                : string.Empty;
            nameLabel.text = $"{editorStatusText}{baselineStatusText} {item.bundleName}";
            nameLabel.style.color = GetStatusColor(item.status);
            sizeBar.value = Mathf.Clamp(percentage, 0f, 100f);
            sizeBar.tooltip = $"占所有已构建 Bundle 大小的 {percentage:0.##}%";
            element.tooltip = $"已构建：{BundleReferenceCompareUtility.FormatSize(item.builtSize)}，占比：{percentage:0.##}%，当前配置：{item.currentAssets.Count} 个资源";
        }

        private void AnalyzeBundleItem(BundleCompareItem item)
        {
            if (item == null || item.isAnalyzed)
                return;

            BundleReferenceComparisonService.Analyze(
                item,
                _baseline,
                _builtData);
        }

        private static string GetStatusText(BundleCompareStatus status)
        {
            switch (status)
            {
                case BundleCompareStatus.Unanalyzed: return "待分析";
                case BundleCompareStatus.Added: return "新增";
                case BundleCompareStatus.Removed: return "移除";
                case BundleCompareStatus.Changed: return "变化";
                default: return string.Empty;
            }
        }

        private static string GetBaselineStatusText(BundleCompareStatus status)
        {
            return GetStatusText(status);
        }

        private static Color GetStatusColor(BundleCompareStatus status)
        {
            switch (status)
            {
                case BundleCompareStatus.Unanalyzed: return BundleReferenceCompareSettings.MutedTextColor;
                case BundleCompareStatus.Added: return new Color(0.35f, 0.86f, 0.52f, 1f);
                case BundleCompareStatus.Removed: return new Color(1f, 0.42f, 0.42f, 1f);
                case BundleCompareStatus.Changed: return new Color(1f, 0.78f, 0.3f, 1f);
                default: return new Color(0.86f, 0.9f, 0.96f, 1f);
            }
        }

        private void BrowseBuildDirectory()
        {
            var path = EditorUtility.OpenFolderPanel("选择已构建 Bundle 目录", _buildDirectoryField.value, string.Empty);
            if (string.IsNullOrEmpty(path))
                return;
            _buildDirectoryField.value = path;
            if (string.IsNullOrEmpty(_manifestNameField.value))
                _manifestNameField.value = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }

        private void BrowseBaselineFile()
        {
            var path = EditorUtility.OpenFilePanel("选择 Bundle 基准文件", _baselineFileField.value, "bundlebaseline");
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                BundleBuildBaselineUtility.Load(path);
                _baselineFileField.value = path;
                EditorPrefs.SetString(BundleReferenceCompareSettings.HistoryBaselineFileKey, path);
                _summaryLabel.text = "已加载 Bundle 基准文件，点击开始对比查看变化。";
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("基准文件无效", exception.Message, "确定");
            }
        }

        private void SaveCurrentAsBaseline()
        {
            var directory = _buildDirectoryField?.value?.Trim();
            var manifestName = _manifestNameField?.value?.Trim();
            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(manifestName))
            {
                EditorUtility.DisplayDialog("参数不完整", "请选择已构建目录并填写 Manifest 名称。", "确定");
                return;
            }

            BundleReferenceManifest.PrepareManifest(directory, manifestName);
            if (BundleReferenceManifest.manifest == null)
                return;

            try
            {
                var defaultName = $"{manifestName}{BundleBuildBaselineUtility.FileExtension}";
                var path = EditorUtility.SaveFilePanel("保存 Bundle 基准文件", directory, defaultName, "bundlebaseline");
                if (string.IsNullOrEmpty(path))
                    return;

                var records = BundleBuildBaselineUtility.CreateFrom(directory, manifestName, BundleReferenceManifest.manifest);
                BundleBuildBaselineUtility.Save(path, records);
                _baselineFileField.value = path;
                EditorPrefs.SetString(BundleReferenceCompareSettings.HistoryBaselineFileKey, path);
                EditorUtility.DisplayDialog("保存成功", "已保存当前构建 Bundle 基准。", "确定");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("保存基准失败", exception.Message, "确定");
            }
            finally
            {
                BundleReferenceManifest.ClearManifest();
            }
        }

        private void Compare()
        {
            Clear();
            _builtData.Clear();
            _currentBundleNames = null;
            var directory = _buildDirectoryField.value?.Trim();
            var manifestName = _manifestNameField.value?.Trim();
            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(manifestName))
            {
                EditorUtility.DisplayDialog("参数不完整", "请选择已构建目录并填写 Manifest 名称。", "确定");
                return;
            }

            BundleReferenceManifest.PrepareManifest(directory, manifestName);
            if (BundleReferenceManifest.manifest == null)
                return;

            try
            {
                _baseline = null;
                var baselinePath = _baselineFileField.value?.Trim();
                if (!string.IsNullOrEmpty(baselinePath))
                {
                    try
                    {
                        _baseline = BundleBuildBaselineUtility.Load(baselinePath);
                    }
                    catch (Exception exception)
                    {
                        _baseline = null;
                        Debug.LogWarning($"Bundle 基准文件读取失败，将跳过基准比较：{exception.Message}");
                    }
                }
                _comparisonDirectory = directory;
                _comparisonManifestName = manifestName;

                _currentBundleNames = new HashSet<string>(
                    AssetDatabase.GetAllAssetBundleNames(),
                    StringComparer.OrdinalIgnoreCase);

                _items.AddRange(BundleReferenceComparisonService.Compare(_baseline,
                    _currentBundleNames));
                EditorPrefs.SetString(BundleReferenceCompareSettings.HistoryBuildDirectoryKey, directory);
                EditorPrefs.SetString(BundleReferenceCompareSettings.HistoryManifestNameKey, manifestName);
                _totalBuiltSize = _items.Sum(item => item.builtSize);
                _bundleList.Rebuild();
                _summaryLabel.text = BundleReferenceComparisonService.BuildSummary(_items);
                // if (_items.Count > 0)
                //     _bundleList.SetSelection(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Bundle 对比失败", exception.Message, "确定");
            }
        }

        private void ShowDetails(BundleCompareItem item)
        {
            _detailView.Clear();
            if (item == null)
                return;
            var title = new Label(item.bundleName);
            title.style.fontSize = 15f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = GetStatusColor(item.status);
            title.style.marginBottom = 8f;
            _detailView.Add(title);

            var status = new Label($"状态    {GetStatusText(item.status)}");
            status.style.color = GetStatusColor(item.status);
            _detailView.Add(status);
            if (_baseline != null && (item.hasBaseline || item.baselineStatus == BundleCompareStatus.Added))
            {
                var baselineStatus = new Label($"基准状态    {GetBaselineStatusText(item.baselineStatus)}");
                baselineStatus.style.color = GetStatusColor(item.baselineStatus);
                _detailView.Add(baselineStatus);
            }
            _detailView.Add(CreateDetailLabel($"大小: {BundleReferenceCompareUtility.FormatSize(item.builtSize)}"));
            _detailView.Add(CreateDetailLabel($"加载成本: {BundleReferenceCompareUtility.FormatSize(item.loadCost)}（包含自身及全部依赖）"));
            _detailView.Add(CreateDetailLabel($"依赖分包: {item.dependentBundles.Count} 个"));
            if (item.dependentBundles.Count > 0)
                _detailView.Add(CreateDetailLabel("依赖列表: " + string.Join("、", item.dependentBundles)));
            _detailView.Add(CreateDetailLabel($"资源: 已构建 {item.builtAssets.Count}  |  当前配置 {item.currentAssets.Count}  |  新增 {item.addedAssets.Count}  |  移除 {item.removedAssets.Count}"));
            _detailView.Add(CreateDetailLabel("类型: " + BundleReferenceCompareFormatter.FormatTypes(item.builtTypes)));
            if (_baseline != null && item.hasBaseline)
            {
                var sizeDelta = item.builtSize - item.baselineSize;
                var baselineAssets = item.baselineAssets ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var baselineAdded = new HashSet<string>(item.builtAssets, StringComparer.OrdinalIgnoreCase);
                baselineAdded.ExceptWith(baselineAssets);
                var baselineRemoved = new HashSet<string>(baselineAssets, StringComparer.OrdinalIgnoreCase);
                baselineRemoved.ExceptWith(item.builtAssets);
                var baselineDependencies = item.baselineDependentBundles ?? new List<string>();
                var dependencyAdded = new HashSet<string>(item.dependentBundles, StringComparer.OrdinalIgnoreCase);
                dependencyAdded.ExceptWith(baselineDependencies);
                var dependencyRemoved = new HashSet<string>(baselineDependencies, StringComparer.OrdinalIgnoreCase);
                dependencyRemoved.ExceptWith(item.dependentBundles);
                _detailView.Add(CreateDetailLabel($"相对基准: 大小 {BundleReferenceCompareFormatter.FormatSignedSize(sizeDelta)}（基准 {BundleReferenceCompareUtility.FormatSize(item.baselineSize)}），资源新增 {baselineAdded.Count}，移除 {baselineRemoved.Count}，依赖变化 +{dependencyAdded.Count}/-{dependencyRemoved.Count}"));
            }
            _detailView.Add(new VisualElement()
            {
                style =
                {
                    height = 1f,
                    marginTop = 6f,
                    marginBottom = 6f,
                    backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.5f)
                }
            });
            _detailView.Add(new Label("资源列表"));
            AddAssetList(item);
        }

        private static Label CreateDetailLabel(string text)
        {
            var label = new Label(text);
            label.style.marginBottom = 4f;
            label.style.color = BundleReferenceCompareSettings.DetailTextColor;
            return label;
        }

        private void AddAssetList(BundleCompareItem item)
        {
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.marginTop = 4f;
            header.style.marginBottom = 2f;
            header.Add(CreateAssetColumnHeader("已构建 Bundle 资源"));
            header.Add(CreateAssetColumnHeader("当前 Editor 配置资源"));
            _detailView.Add(header);

            var newColor = GetStatusColor(BundleCompareStatus.Added);
            var removeColor = GetStatusColor(BundleCompareStatus.Removed);
            var baselineAddedColor = BundleReferenceCompareSettings.BaselineAddedColor;
            var baselineRemovedColor = BundleReferenceCompareSettings.BaselineRemovedColor;
            var list = new ListView(item.allAssets, 24f, MakeAssetRow, (element, index) =>
            {
                var asset = item.allAssets[index];
                var existsBuilt = item.builtAssets.Contains(asset);
                var existsCurrent = item.currentAssets.Contains(asset);
                var existsBaseline = _baseline != null && item.hasBaseline && item.baselineAssets.Contains(asset);
                var builtLabel = element.Q<Label>("BuiltAssetLabel");
                var currentLabel = element.Q<Label>("CurrentAssetLabel");
                var builtMarker = string.Empty;
                if (_baseline == null)
                {
                    builtMarker = existsBuilt && !existsCurrent ? "【移除】" : string.Empty;
                }
                else
                {
                    if (existsBuilt && !existsBaseline)
                        builtMarker = "【对比基准新增】";
                    else if (!existsBuilt && existsBaseline)
                        builtMarker = "【对比基准移除】";
                    else if (existsBuilt && !existsCurrent)
                        builtMarker = "【移除】";
                }
                var builtColor = existsBuilt && !existsBaseline
                    ? baselineAddedColor
                    : !existsBuilt && existsBaseline
                        ? baselineRemovedColor
                        : existsBuilt && !existsCurrent ? removeColor : newColor;
                BindAssetLabel(builtLabel, existsBuilt || existsBaseline ? asset : null, builtMarker, builtColor);
                BindAssetLabel(currentLabel, existsCurrent ? asset : null, !existsBuilt && existsCurrent ? "【新增】" : string.Empty, newColor);
            });
            list.style.minHeight = 180;
            list.style.flexGrow = 1;
            _detailView.Add(list);
        }

        private static Label CreateAssetColumnHeader(string text)
        {
            var label = new Label();
            label.text = text;
            label.style.flexGrow = 1f;
            label.style.flexBasis = 0f;
            label.style.minWidth = 0f;
            label.style.paddingLeft = 6f;
            label.style.paddingRight = 6f;
            label.style.paddingTop = 4f;
            label.style.paddingBottom = 4f;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = new Color(0.68f, 0.75f, 0.86f, 1f);
            return label;
        }

        private VisualElement MakeAssetRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.minHeight = 24f;
            row.style.borderBottomWidth = 1f;
            row.style.borderBottomColor = new Color(0.3f, 0.32f, 0.36f, 0.35f);

            row.Add(MakeAssetLabel("BuiltAssetLabel"));
            row.Add(MakeAssetLabel("CurrentAssetLabel"));
            return row;
        }

        private static Label MakeAssetLabel(string name)
        {
            var label = new Label
            {
                name = name
            };
            label.style.flexGrow = 1f;
            label.style.flexBasis = 0f;
            label.style.minWidth = 0f;
            label.style.paddingLeft = 6f;
            label.style.paddingRight = 6f;
            label.style.paddingTop = 3f;
            label.style.paddingBottom = 3f;
            label.style.overflow = Overflow.Hidden;
            label.style.textOverflow = TextOverflow.Ellipsis;
            label.RegisterCallback<MouseUpEvent>(OnAssetLabelClicked);
            return label;
        }

        private static void BindAssetLabel(Label label, string asset, string marker, Color color)
        {
            if (label == null)
                return;

            label.userData = asset;
            label.text = string.IsNullOrEmpty(asset)
                ? string.Empty
                : $"{marker}{Path.GetFileName(asset)}";
            // label.tooltip = string.IsNullOrEmpty(asset) ? string.Empty : tooltip;
            label.style.color = !string.IsNullOrEmpty(marker) ? color : BundleReferenceCompareSettings.AssetTextColor;
        }

        private static void OnAssetLabelClicked(MouseUpEvent evt)
        {
            if (evt.button != 0 || evt.currentTarget is not Label label || string.IsNullOrEmpty(label.userData as string))
                return;

            BundleReferenceUtils.PingAsset(label.userData as string);
            evt.StopPropagation();
        }

        private void Clear()
        {
            _items.Clear();
            _builtData.Clear();
            _currentBundleNames = null;
            _baseline = null;
            _comparisonDirectory = null;
            _comparisonManifestName = null;
            _totalBuiltSize = 0;
            _bundleList?.Rebuild();
            _detailView?.Clear();
            if (_summaryLabel != null)
                _summaryLabel.text = BundleReferenceCompareSettings.InitialSummary;
        }

        private void OnDisable()
        {
            Clear();
            BundleReferenceManifest.ClearManifest();
            _bundleListResizer = null;
        }

    }
}
