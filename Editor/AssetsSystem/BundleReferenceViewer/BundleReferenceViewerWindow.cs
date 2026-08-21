using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerCellStudio.Editor
{
    public class BundleReferenceViewerWindow : EditorWindow
    {
        [MenuItem("Tools/UFlow/Bundle Reference")]
        private static void ShowWindow()
        {
            var window = GetWindow<BundleReferenceViewerWindow>();
            window.titleContent = new GUIContent("Bundle Reference Viewer");
            window.Show();
        }

        private static string _analysisFileDirectoty = "Analysis/";
        private BundleReferenceGraphView _graphView;
        private BundleReferenceQueryer _queryer;
        private VisualElement _groupListPanel;
        private TextField _searchField;
        private readonly Dictionary<string, bool> _groupExpandedState = new Dictionary<string, bool>();
        private string _selectedBundleName;
        private BundleDefectDetectorBox _defectDetectorBox;
        private int _analysisVersion;
        private int _groupDetectionVersion;

        private void OnEnable()
        {
            _defectDetectorBox = new BundleDefectDetectorBox();
            
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.Add(CreateToolbar());

            var content = new VisualElement();
            content.style.flexDirection = FlexDirection.Row;
            content.style.flexGrow = 1f;

            var listPanel = new VisualElement();
            listPanel.style.width = 280f;
            listPanel.style.minWidth = 220f;
            listPanel.style.borderRightWidth = 1f;
            listPanel.style.borderRightColor = new Color(0.25f, 0.25f, 0.25f);

            var listTitle = new Label("AssetBundle");
            listTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            listTitle.style.paddingLeft = 8f;
            listTitle.style.paddingTop = 6f;
            listTitle.style.paddingBottom = 6f;
            listPanel.Add(listTitle);

            _groupListPanel = new ScrollView(ScrollViewMode.Vertical);
            _groupListPanel.style.flexGrow = 1f;
            listPanel.Add(_groupListPanel);

            _graphView = new BundleReferenceGraphView();
            content.Add(listPanel);
            content.Add(_graphView);
            rootVisualElement.Add(content);
        }
        
        private void OnDisable()
        {
            _analysisVersion++;
            DisposeQueryer();
            _defectDetectorBox?.Dispose();
            _defectDetectorBox = null;
        }

        private VisualElement CreateToolbar()
        {
            var toolbar = new  UnityEditor.UIElements.Toolbar();
            _searchField = new TextField("搜索 Bundle")
            {
                tooltip = "输入 Bundle 名称进行模糊搜索"
            };
            _searchField.style.minWidth = 260f;
            _searchField.RegisterValueChangedCallback(_ => RebuildGroupList());
            toolbar.Add(_searchField);

            var generateButton = new Button(GenerateGraph)
            {
                text = "生成分析"
            };
            toolbar.Add(generateButton);

            var clearButton = new Button(ClearGraph)
            {
                text = "清空"
            };
            toolbar.Add(clearButton);

            var relayoutButton = new Button(Relayout)
            {
                text = "重新布局"
            };
            toolbar.Add(relayoutButton);
            return toolbar;
        }

        private void GenerateGraph()
        {
            var analysisVersion = ++_analysisVersion;
            DisposeQueryer();
            if (_defectDetectorBox == null)
                return;

            _queryer = QueryerFactory.GenerateQueryerByCurrentProjectSync();
            if (_queryer == null || analysisVersion != _analysisVersion)
                return;

            RebuildGroupList();
            DetectGroups();
            RebuildGroupList();
            SelectFirstBundle(analysisVersion);
        }

        private void ClearGraph()
        {
            _analysisVersion++;
            _groupDetectionVersion++;
            DisposeQueryer();
            _groupListPanel?.Clear();
            _selectedBundleName = null;
            _graphView.ClearGraph();
        }

        private void Relayout()
        {
            _graphView.Relayout(_queryer, _selectedBundleName);
        }

        private void DisposeQueryer()
        {
            if (_queryer == null)
                return;
            _queryer.Dispose();
            _queryer = null;
        }

        private void DetectGroups()
        {
            var groups = _queryer?.GetAllGroups()?.Values?.ToArray();
            if (groups == null || groups.Length == 0)
                return;

            foreach (var bundleData in _queryer.GetAllBundleData().Values)
                _queryer.EnsureAssets(bundleData.bundleName);

            for (var i = 0; i < groups.Length; i++)
                _defectDetectorBox.DetectGroup(new[] { groups[i] }, _queryer);
        }

        private void SelectFirstBundle(int analysisVersion)
        {
            if (analysisVersion != _analysisVersion || _queryer == null)
                return;

            var firstBundle = _queryer.GetAllBundleData().Keys.OrderBy(name => name).FirstOrDefault();
            if (!string.IsNullOrEmpty(firstBundle))
            {
                // SelectBundle(firstBundle);
                return;
            }

            _selectedBundleName = null;
            _graphView.ClearGraph();
        }

        private void RebuildGroupList()
        {
            if (_groupListPanel == null)
                return;
            _groupListPanel.Clear();
            if (_queryer == null)
                return;
            var countLabel = new Label($"共 {_queryer.bundleCount} 个 Bundle");
            _groupListPanel.Add(countLabel);
            var search = _searchField?.value?.Trim() ?? string.Empty;
            foreach (var groupPair in _queryer.GetAllGroups().OrderByDescending(pair => pair.Value.bundleNames.Count))
            {
                var bundleNames = groupPair.Value.bundleNames
                    .Where(name => string.IsNullOrEmpty(search) ||
                                   name.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(name => name)
                    .ToList();
                if (bundleNames.Count == 0)
                    continue;
                if (!_groupExpandedState.ContainsKey(groupPair.Key))
                    _groupExpandedState[groupPair.Key] = true;
                var foldout = new Foldout
                {
                    text = $"{groupPair.Key} ({bundleNames.Count}/{groupPair.Value.bundleNames.Count})",
                    value = _groupExpandedState[groupPair.Key]
                };
                foldout.RegisterValueChangedCallback(evt => _groupExpandedState[groupPair.Key] = evt.newValue);

                var bundleList = new ListView(bundleNames, 22f,
                    () =>
                    {
                        var button = new Button();
                        button.style.unityTextAlign = TextAnchor.MiddleLeft;
                        button.style.marginLeft = 12f;
                        button.tooltip = "显示此 Bundle 的完整引用关系";
                        button.clicked += () =>
                        {
                            if (button.userData is string bundleName)
                                SelectBundle(bundleName);
                        };
                        return button;
                    },
                    (element, index) =>
                    {
                        var button = (Button)element;
                        var bundleName = bundleNames[index];
                        button.text = bundleName;
                        button.userData = bundleName;
                    });
                bundleList.selectionType = SelectionType.None;
                bundleList.style.height = Mathf.Min(bundleNames.Count * 22f, 220f);
                bundleList.style.marginTop = 2f;
                bundleList.style.display = foldout.value ? DisplayStyle.Flex : DisplayStyle.None;
                foldout.RegisterValueChangedCallback(evt =>
                {
                    bundleList.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                });
                foldout.Add(bundleList);
                _groupListPanel.Add(foldout);
            }
        }

        private void SelectBundle(string bundleName)
        {
            _selectedBundleName = bundleName;
            _graphView.ShowBundle(_queryer, _selectedBundleName, _defectDetectorBox);
        }

        private void ReadAnalysisFile()
        {
            var path = EditorUtility.OpenFilePanelWithFilters(
                "选择分析文件",
                _analysisFileDirectoty,
                new[] { "AssetBundle Reference Analysis", "BRAnalysis" });
            if (!string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"分析文件读取尚未接入: {path}");
            }
        }

        private void CreateGUI()
        {
            if (_graphView != null)
            {
                return;
            }
            OnEnable();
        }
    }
}