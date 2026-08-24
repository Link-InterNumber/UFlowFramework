using System;
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
        private ListView _groupListPanel;
        private Label _groupCountLabel;
        private readonly List<GroupListItem> _groupListItems = new List<GroupListItem>();
        private VisualElement _listPanel;
        private VisualElement _listPanelResizer;
        private TextField _searchField;
        private IntegerField _referenceDepthField;
        private readonly Dictionary<string, bool> _groupExpandedState = new Dictionary<string, bool>();
        private string _selectedBundleName;
        private BundleDefectDetectorBox _defectDetectorBox;
        private int _analysisVersion;
        private int _groupDetectionVersion;
        private Func<bool> _simplifyModeFun;
        public bool isSimplifyMode => this._simplifyModeFun != null && this._simplifyModeFun();

        private void OnEnable()
        {
            _defectDetectorBox = new BundleDefectDetectorBox();
            
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.Add(CreateToolbar());

            var content = new VisualElement();
            content.style.flexDirection = FlexDirection.Row;
            content.style.flexGrow = 1f;

            _listPanel = new VisualElement();
            _listPanel.style.width = 280f;
            _listPanel.style.minWidth = 220f;
            _listPanel.style.maxWidth = 600f;
            _listPanel.style.flexShrink = 0f;
            _listPanel.style.borderRightWidth = 1f;
            _listPanel.style.borderRightColor = new Color(0.25f, 0.25f, 0.25f);

            var listTitle = new Label("AssetBundle");
            listTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            listTitle.style.paddingLeft = 8f;
            listTitle.style.paddingTop = 6f;
            listTitle.style.paddingBottom = 6f;
            _listPanel.Add(listTitle);

            _groupCountLabel = new Label();
            _groupCountLabel.style.paddingLeft = 8f;
            _groupCountLabel.style.paddingTop = 4f;
            _groupCountLabel.style.paddingBottom = 4f;
            _listPanel.Add(_groupCountLabel);

            _groupListPanel = new ListView
            {
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                selectionType = SelectionType.None,
                makeItem = MakeGroupListItem,
                bindItem = BindGroupListItem
            };
            _groupListPanel.style.flexGrow = 1f;
            _listPanel.Add(_groupListPanel);

            _listPanelResizer = CreateListPanelResizer(content);

            _graphView = new BundleReferenceGraphView();
            content.Add(_listPanel);
            content.Add(_listPanelResizer);
            content.Add(_graphView);
            rootVisualElement.Add(content);
        }

        private VisualElement CreateListPanelResizer(VisualElement content)
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
            resizer.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);

            resizer.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0)
                    return;

                resizer.CaptureMouse();
                evt.StopPropagation();
            });

            resizer.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (!resizer.HasMouseCapture() || _listPanel == null)
                    return;

                var width = evt.mousePosition.x - content.worldBound.x;
                _listPanel.style.width = Mathf.Clamp(width, 220f, 600f);
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
        
        private void OnDisable()
        {
            _analysisVersion++;
            DisposeQueryer();
            _defectDetectorBox?.Dispose();
            _defectDetectorBox = null;
            _groupListItems.Clear();
            if (_groupListPanel != null)
                _groupListPanel.itemsSource = null;
            _listPanel = null;
            _groupListPanel = null;
            _groupCountLabel = null;
            _listPanelResizer = null;
        }

        private VisualElement MakeGroupListItem()
        {
            var foldout = new Foldout();
            var btnShowAll = new Button(() =>
            {
                if (foldout.userData is string groupName && _queryer != null)
                    _graphView.ShowGroup(_queryer, groupName, _defectDetectorBox, isSimplifyMode);
            })
            {
                text = "显示所有 Bundle",
                tooltip = "显示此组内所有 Bundle 的引用/被引用链条",
            };
            foldout.Add(btnShowAll);

            foldout.RegisterValueChangedCallback(evt =>
            {
                if (foldout.userData is string groupName)
                    _groupExpandedState[groupName] = evt.newValue;
            });
            return foldout;
        }

        private void BindGroupListItem(VisualElement element, int index)
        {
            var foldout = (Foldout)element;
            var item = _groupListItems[index];
            foldout.userData = item.groupName;
            foldout.text = $"{item.groupName} ({item.bundleNames.Count}/{item.totalBundleCount})";
            foldout.SetValueWithoutNotify(_groupExpandedState[item.groupName]);
            foldout.Clear();
            
            var btnShowAll = new Button(() =>
            {
                if (foldout.userData is string groupName && _queryer != null)
                    _graphView.ShowGroup(_queryer, groupName, _defectDetectorBox, isSimplifyMode);
            })
            {
                text = "显示所有 Bundle",
                tooltip = "显示此组内所有 Bundle 的引用/被引用链条",
            };
            foldout.Add(btnShowAll);
            
            var bundleList = new ListView(item.bundleNames, 22f,
                () =>
                {
                    var button = new Button();
                    button.style.unityTextAlign = TextAnchor.MiddleLeft;
                    button.style.marginLeft = 12f;
                    button.tooltip = "显示此 Bundle 的引用/被引用链条";
                    button.RegisterCallback<MouseDownEvent>(evt => evt.StopPropagation());
                    button.clicked += () =>
                    {
                        if (button.userData is string bundleName)
                            SelectBundle(bundleName);
                    };
                    return button;
                },
                (child, bundleIndex) =>
                {
                    var button = (Button)child;
                    button.text = item.bundleNames[bundleIndex];
                    button.userData = item.bundleNames[bundleIndex];
                });
            bundleList.selectionType = SelectionType.None;
            bundleList.style.height = Mathf.Min(item.bundleNames.Count * 22f, 220f);
            bundleList.style.marginTop = 2f;
            bundleList.style.display = foldout.value ? DisplayStyle.Flex : DisplayStyle.None;
            foldout.RegisterValueChangedCallback(evt =>
            {
                bundleList.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            });
            foldout.Add(bundleList);
        }

        private sealed class GroupListItem
        {
            public string groupName;
            public List<string> bundleNames;
            public int totalBundleCount;
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
            
            var simplifyModeToggle = new Toggle("简化模式")
            {
                tooltip = "启用后，图中只显示 Bundle 节点引用关系，不显示 Asset 节点引用关系",
            };
            _simplifyModeFun = () => simplifyModeToggle.value;
            toolbar.Add(simplifyModeToggle);

            _referenceDepthField = new IntegerField("关系层数")
            {
                value = 8,
                tooltip = "Bundle 模式下，显示当前 Bundle 两侧各 N 层关系"
            };
            _referenceDepthField.style.width = 150f;
            _referenceDepthField.RegisterValueChangedCallback(evt =>
            {
                var depth = Mathf.Max(0, evt.newValue);
                if (evt.newValue != depth)
                    _referenceDepthField.SetValueWithoutNotify(depth);
                if (!string.IsNullOrEmpty(_selectedBundleName))
                    SelectBundle(_selectedBundleName);
            });
            toolbar.Add(_referenceDepthField);

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
            _groupListItems.Clear();
            if (_groupListPanel != null)
            {
                _groupListPanel.itemsSource = _groupListItems;
                _groupListPanel.Rebuild();
            }
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
                _defectDetectorBox.DetectGroups(new[] { groups[i] }, _queryer);
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
            _groupListItems.Clear();
            if (_queryer == null)
            {
                _groupCountLabel.text = string.Empty;
                _groupListPanel.itemsSource = _groupListItems;
                _groupListPanel.Rebuild();
                return;
            }

            _groupCountLabel.text = $"共 {_queryer.bundleCount} 个 Bundle";
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
                _groupListItems.Add(new GroupListItem
                {
                    groupName = groupPair.Key,
                    bundleNames = bundleNames,
                    totalBundleCount = groupPair.Value.bundleNames.Count
                });
            }

            _groupListPanel.itemsSource = _groupListItems;
            _groupListPanel.Rebuild();
        }

        private void SelectBundle(string bundleName)
        {
            _selectedBundleName = bundleName;
            var referenceDepth = _referenceDepthField?.value ?? 1;
            _graphView.ShowBundle(_queryer, _selectedBundleName, _defectDetectorBox,
                isSimplifyMode, Mathf.Max(0, referenceDepth));
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