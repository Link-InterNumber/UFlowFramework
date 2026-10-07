using System;
using System.Collections.Generic;
using System.Linq;
using PowerCellStudio;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerCellStudio.Editor
{
    internal sealed class LoaderProfilerModuleViewController : ProfilerModuleViewController
    {
        public struct LoadProfilerFrameDataDisplay
        {
            public string assetPath;
            public string assetBundleName;
            public int frameIndex;
            public int objectHashCode;
            public LoadState state;
            public string[] assetDependencies;
            public string[] bundleDependencies;
            public bool beginThisFrame;
        }
        
        private readonly List<LoadProfilerFrameDataDisplay> _samples =
            new List<LoadProfilerFrameDataDisplay>();
        private readonly Dictionary<string, BundleSummary> _bundles =
            new Dictionary<string, BundleSummary>(StringComparer.Ordinal);

        private VisualElement _root;
        private Label _frameLabel;
        private Label _sampleMetric;
        private Label _bundleMetric;
        private Label _depthMetric;
        private Label _activeMetric;
        private StateDistributionElement _stateDistribution;
        private ListView _bundleList;
        private ListView _sampleList;
        private Toggle _showDependencies;
        private long _selectedFrame = -1;

        public LoaderProfilerModuleViewController(ProfilerWindow profilerWindow)
            : base(profilerWindow)
        {
        }

        protected override VisualElement CreateView()
        {
            _root = new VisualElement();
            _root.style.flexDirection = FlexDirection.Column;
            _root.style.paddingLeft = 10f;
            _root.style.paddingRight = 10f;
            _root.style.paddingTop = 8f;
            _root.style.paddingBottom = 8f;
            _root.style.backgroundColor = new Color(0.105f, 0.11f, 0.125f);

            var toolbar = new UnityEditor.UIElements.Toolbar();
            toolbar.style.height = 28f;
            _frameLabel = new Label();
            _frameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _frameLabel.style.flexGrow = 1f;
            _frameLabel.style.paddingLeft = 4f;
            toolbar.Add(_frameLabel);

            var refreshButton = new UnityEditor.UIElements.ToolbarButton(RefreshData) { text = "Refresh" };
            toolbar.Add(refreshButton);
            _showDependencies = new Toggle("Show Dependencies");
            _showDependencies.RegisterValueChangedCallback(_ => RebuildSampleList());
            toolbar.Add(_showDependencies);
            _root.Add(toolbar);

            _root.Add(CreateDashboard());

            var splitter = new TwoPaneSplitView(0, 280f, TwoPaneSplitViewOrientation.Horizontal);
            splitter.Add(CreateBundlePanel());
            splitter.Add(CreateSamplePanel());
            splitter.style.flexGrow = 1f;
            _root.Add(splitter);

            ProfilerWindow.SelectedFrameIndexChanged += OnSelectedFrameChanged;
            // OnSelectedFrameChanged(ProfilerWindow.selectedFrameIndex);
            return _root;
        }

        private VisualElement CreateDashboard()
        {
            var dashboard = new VisualElement();
            dashboard.style.marginTop = 8f;
            dashboard.style.marginBottom = 8f;
            dashboard.style.paddingLeft = 8f;
            dashboard.style.paddingRight = 8f;
            dashboard.style.paddingTop = 8f;
            dashboard.style.paddingBottom = 8f;
            dashboard.style.backgroundColor = new Color(0.145f, 0.155f, 0.18f);
            dashboard.style.borderTopLeftRadius = 5f;
            dashboard.style.borderTopRightRadius = 5f;
            dashboard.style.borderBottomLeftRadius = 5f;
            dashboard.style.borderBottomRightRadius = 5f;

            var metrics = new VisualElement();
            metrics.style.flexDirection = FlexDirection.Row;
            metrics.style.marginBottom = 8f;
            metrics.Add(CreateMetricCard("SAMPLES", new Color(0.30f, 0.66f, 1f), out _sampleMetric));
            metrics.Add(CreateMetricCard("BUNDLES", new Color(0.66f, 0.48f, 1f), out _bundleMetric));
            metrics.Add(CreateMetricCard("MAX DEPENDENCY", new Color(1f, 0.67f, 0.25f), out _depthMetric));
            metrics.Add(CreateMetricCard("ACTIVE", new Color(0.30f, 0.82f, 0.52f), out _activeMetric));
            dashboard.Add(metrics);

            _stateDistribution = new StateDistributionElement();
            dashboard.Add(_stateDistribution);
            return dashboard;
        }

        private static VisualElement CreateMetricCard(string title, Color accent, out Label valueLabel)
        {
            var card = new VisualElement();
            card.style.flexGrow = 1f;
            card.style.flexBasis = 0f;
            card.style.height = 58f;
            card.style.marginRight = 6f;
            card.style.paddingLeft = 10f;
            card.style.paddingTop = 7f;
            card.style.backgroundColor = new Color(0.19f, 0.20f, 0.23f);
            card.style.borderLeftWidth = 3f;
            card.style.borderLeftColor = accent;
            card.style.borderTopLeftRadius = 3f;
            card.style.borderTopRightRadius = 3f;
            card.style.borderBottomLeftRadius = 3f;
            card.style.borderBottomRightRadius = 3f;

            var titleLabel = new Label(title);
            titleLabel.style.fontSize = 10f;
            titleLabel.style.color = new Color(0.65f, 0.68f, 0.74f);
            card.Add(titleLabel);

            valueLabel = new Label("0");
            valueLabel.style.fontSize = 22f;
            valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            valueLabel.style.color = accent;
            card.Add(valueLabel);
            return card;
        }

        private VisualElement CreateBundlePanel()
        {
            var panel = new VisualElement();
            panel.style.flexDirection = FlexDirection.Column;
            panel.style.marginRight = 6f;
            panel.Add(CreateSectionHeader("ASSET BUNDLES", new Color(0.66f, 0.48f, 1f)));

            _bundleList = new ListView
            {
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                selectionType = SelectionType.None,
                makeItem = () =>
                {
                    var row = new VisualElement();
                    row.style.flexGrow = 0f;
                    row.style.flexShrink = 0f;
                    row.style.paddingBottom = 3f;

                    var label = new Label();
                    label.name = "sample-label";
                    label.style.whiteSpace = WhiteSpace.Normal;
                    label.style.flexGrow = 0f;
                    label.style.flexShrink = 0f;
                    row.Add(label);
                    return row;
                },
                bindItem = (element, index) =>
                {
                    var label = element.Q<Label>("sample-label");
                    var summary = GetBundleAt(index);
                    label.text = $"{summary.Name}  ({summary.SampleCount})\nAssets: {summary.AssetCount}\nMax Dependency Count: {summary.MaxDepth}";
                    label.style.whiteSpace = WhiteSpace.Normal;
                    label.style.paddingLeft = 8f;
                    label.style.paddingRight = 6f;
                    label.style.paddingTop = 6f;
                    label.style.paddingBottom = 6f;
                    label.style.marginBottom = 2f;
                    label.style.backgroundColor = index % 2 == 0
                        ? new Color(0.17f, 0.18f, 0.205f)
                        : new Color(0.145f, 0.155f, 0.18f);
                }
            };
            _bundleList.style.flexGrow = 1f;
            _bundleList.style.paddingBottom = 12f;
            panel.Add(_bundleList);
            return panel;
        }

        private VisualElement CreateSamplePanel()
        {
            var panel = new VisualElement();
            panel.style.flexDirection = FlexDirection.Column;
            panel.Add(CreateSectionHeader("LOAD SAMPLES", new Color(0.30f, 0.66f, 1f)));
            _sampleList = new ListView
            {
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                selectionType = SelectionType.None,
                makeItem = () =>
                {
                    var row = new VisualElement();
                    row.name = "sample-row";
                    row.style.flexGrow = 0f;
                    row.style.flexShrink = 0f;
                    row.style.paddingBottom = 3f;

                    var mainLabel = new Label();
                    mainLabel.name = "sample-main-label";
                    mainLabel.style.whiteSpace = WhiteSpace.Normal;
                    mainLabel.style.flexGrow = 0f;
                    mainLabel.style.flexShrink = 0f;
                    mainLabel.style.paddingLeft = 9f;
                    mainLabel.style.paddingRight = 8f;
                    mainLabel.style.paddingTop = 7f;
                    mainLabel.style.paddingBottom = 2f;
                    row.Add(mainLabel);

                    var dependencyContainer = new VisualElement();
                    dependencyContainer.name = "dependency-container";
                    dependencyContainer.style.flexGrow = 0f;
                    dependencyContainer.style.flexShrink = 0f;
                    dependencyContainer.style.paddingLeft = 9f;
                    dependencyContainer.style.paddingRight = 8f;
                    dependencyContainer.style.paddingBottom = 7f;
                    row.Add(dependencyContainer);
                    return row;
                },
                bindItem = (element, index) =>
                {
                    var sample = _samples[index];
                    var mainLabel = element.Q<Label>("sample-main-label");
                    mainLabel.text = FormatSample(sample);
                    ApplySampleStyle(mainLabel, sample, index);

                    var dependencyContainer = element.Q<VisualElement>("dependency-container");
                    PopulateDependencyLabels(dependencyContainer, sample);
                }
            };
            _sampleList.style.flexGrow = 1f;
            _sampleList.style.flexShrink = 1f;
            _sampleList.style.paddingBottom = 12f;
            panel.Add(_sampleList);
            return panel;
        }

        private static Label CreateSectionHeader(string text, Color accent)
        {
            var label = new Label(text);
            label.style.height = 27f;
            label.style.paddingLeft = 8f;
            label.style.paddingTop = 6f;
            label.style.marginBottom = 4f;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = accent;
            label.style.backgroundColor = new Color(0.145f, 0.155f, 0.18f);
            label.style.borderBottomWidth = 2f;
            label.style.borderBottomColor = accent;
            return label;
        }

        private static void ApplySampleStyle(Label label, LoadProfilerFrameDataDisplay sample, int index)
        {
            var stateColor = sample.beginThisFrame
                ? new Color(0.30f, 0.66f, 1f)
                : (sample.state & LoadState.End) > 0
                    ? new Color(0.30f, 0.82f, 0.52f)
                    : new Color(1f, 0.67f, 0.25f);
            label.style.backgroundColor = index % 2 == 0
                ? new Color(0.17f, 0.18f, 0.205f)
                : new Color(0.145f, 0.155f, 0.18f);
            label.style.borderLeftWidth = 3f;
            label.style.borderLeftColor = stateColor;
        }

        private void OnSelectedFrameChanged(long frameIndex)
        {
            _selectedFrame = frameIndex;
            RefreshData();
        }

        private void RefreshData()
        {
            _samples.Clear();
            _bundles.Clear();

            if (_selectedFrame >= 0)
                ReadSelectedFrameData((int)_selectedFrame);

            _frameLabel.text = _selectedFrame < 0 ? "Selected Frame: Current" : "Selected Frame: " + _selectedFrame;
            UpdateDashboard();
            RebuildSampleList();
            if (_bundleList != null)
            {
                _bundleList.schedule.Execute(() =>
                {
                    if (_bundleList == null)
                        return;
                    _bundleList.itemsSource = _bundles.Keys.ToArray();
                    _bundleList.RefreshItems();
                });
            }
        }

        private void ReadSelectedFrameData(int frameIndex)
        {
            using (var frameDataView = ProfilerDriver.GetRawFrameDataView(frameIndex, 0))
            {
                if (frameDataView == null || !frameDataView.valid)
                    return;

                var frameData = frameDataView.GetFrameMetaData<LoadProfilerFrameData>(
                    LoadSampleCollector.ProfilerGuid,
                    LoadSampleCollector.ProfilerSampleTag);
                if (frameData == null)
                    return;

                foreach (var data in frameData)
                    AddSample(data);
            }
        }

        private void AddSample(LoadProfilerFrameData data)
        {
            var assetBundleName = data.assetBundleName.ToString();
            var assetPath = data.assetPath.ToString();
            var displayData = new LoadProfilerFrameDataDisplay
            {
                frameIndex = data.frameIndex,
                assetBundleName = assetBundleName,
                assetPath = assetPath,
                state = (LoadState)data.state,
                beginThisFrame = data.beginThisFrame != 0,
                assetDependencies = LoadSampleCollector.instance?.dependencyProvider.GetAssetDependencies(assetPath) ?? AssetDatabase.GetDependencies(assetPath, true),
                bundleDependencies = LoadSampleCollector.instance?.dependencyProvider.GetAssetBundleDependencies(assetBundleName) ?? AssetDatabase.GetAssetBundleDependencies(assetBundleName, true)
            };
            _samples.Add(displayData);
            var bundleName = string.IsNullOrEmpty(assetBundleName)
                ? "<No Bundle>"
                : assetBundleName;
            if (!_bundles.TryGetValue(bundleName, out var summary))
            {
                summary = new BundleSummary(bundleName);
                _bundles.Add(bundleName, summary);
            }
            summary.Add(GetMaxDependencyCount(displayData));
        }

        private void RebuildSampleList()
        {
            if (_sampleList == null)
                return;
            _sampleList.schedule.Execute(() =>
            {
                if (_sampleList == null)
                    return;
                _sampleList.itemsSource = _samples;
                _sampleList.RefreshItems();
            });
            // // DynamicHeight 需要在文本重新绑定并完成一次布局后重新测量行高。
            // _sampleList.schedule.Execute(() =>
            // {
            //     if (_sampleList == null)
            //         return;
            //
            //     _sampleList.RefreshItems();
            //     _sampleList.MarkDirtyRepaint();
            // });
        }

        private void UpdateDashboard()
        {
            var begin = 0;
            var loading = 0;
            var ended = 0;
            var maxDepth = 0;
            var activeMetric = 0;
            for (var i = 0; i < _samples.Count; i++)
            {
                var sample = _samples[i];
                maxDepth = Math.Max(maxDepth, GetMaxDependencyCount(sample));
                if (sample.beginThisFrame) begin++;
                if (sample.beginThisFrame || (sample.state & LoadState.End) == 0) activeMetric++;
                if ((sample.state & LoadState.End) > 0) ended++;
                else loading++;
            }
            _sampleMetric.text = _samples.Count.ToString();
            _bundleMetric.text = _bundles.Count.ToString();
            _depthMetric.text = maxDepth.ToString();
            _activeMetric.text = activeMetric.ToString();
            _stateDistribution.SetValues(begin, loading, ended);
        }

        private string FormatSample(LoadProfilerFrameDataDisplay sample)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"{(sample.beginThisFrame ? "[New]" : string.Empty)} [{GetSampleLoadState(sample.state)}] {sample.assetPath}");
            sb.AppendLine($"----Bundle: {sample.assetBundleName}");
            sb.AppendLine($"----Max Dependency Count: {GetMaxDependencyCount(sample)}");
            return sb.ToString();
        }

        private void PopulateDependencyLabels(
            VisualElement container,
            LoadProfilerFrameDataDisplay sample)
        {
            container.Clear();

            if (_showDependencies == null || !_showDependencies.value)
            {
                container.style.display = DisplayStyle.None;
                return;
            }

            container.style.display = DisplayStyle.Flex;
            AddDependencySection(container, $"----Asset Dependencies({sample.assetDependencies?.Length ?? 0})", sample.assetDependencies);
            AddDependencySection(container, $"----Bundle Dependencies({sample.bundleDependencies?.Length ?? 0})", sample.bundleDependencies);
        }

        private static void AddDependencySection(
            VisualElement container,
            string title,
            string[] dependencies)
        {
            var titleLabel = CreateDependencyLabel(title);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginTop = 2f;
            container.Add(titleLabel);

            if (dependencies == null || dependencies.Length == 0)
            {
                container.Add(CreateDependencyLabel("--------None"));
                return;
            }

            var hasValidDependency = false;
            for (var i = 0; i < dependencies.Length; i++)
            {
                var dependency = dependencies[i];
                if (string.IsNullOrEmpty(dependency))
                    continue;

                hasValidDependency = true;
                container.Add(CreateDependencyLabel($"--------{dependency}"));
            }

            if (!hasValidDependency)
                container.Add(CreateDependencyLabel("--------None"));
        }

        private static Label CreateDependencyLabel(string text)
        {
            var label = new Label(text);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.flexGrow = 0f;
            label.style.flexShrink = 0f;
            label.style.paddingLeft = 4f;
            label.style.paddingRight = 2f;
            label.style.marginBottom = 1f;
            label.style.color = new Color(0.72f, 0.75f, 0.82f);
            return label;
        }

        private LoadState GetSampleLoadState(LoadState loadState)
        {
            if ((loadState & LoadState.End) > 0)
                return LoadState.End;
            if ((loadState & LoadState.LoadingAsset) > 0)
                return LoadState.LoadingAsset;
            if ((loadState & LoadState.LoadingBundle) > 0)
                return LoadState.LoadingBundle;
            if ((loadState & LoadState.Begin) > 0)
                return LoadState.Begin;
            return LoadState.Fail;
        }

        private static int GetMaxDependencyCount(LoadProfilerFrameDataDisplay sample)
        {
            return sample.assetDependencies?.Length ?? 0;
            // return Math.Max(sample.assetDependencies?.Length ?? 0,
            //     sample.bundleDependencies?.Length ?? 0);
        }

        private BundleSummary GetBundleAt(int index)
        {
            var current = 0;
            foreach (var pair in _bundles)
            {
                if (current++ == index)
                    return pair.Value;
            }
            return BundleSummary.Empty;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && ProfilerWindow != null)
                ProfilerWindow.SelectedFrameIndexChanged -= OnSelectedFrameChanged;
            _samples.Clear();
            _bundles.Clear();
            base.Dispose(disposing);
        }

        private sealed class BundleSummary
        {
            public static readonly BundleSummary Empty = new BundleSummary(string.Empty);
            public readonly string Name;
            public int SampleCount;
            public int AssetCount;
            public int MaxDepth;

            public BundleSummary(string name) => Name = name;

            public void Add(int depth)
            {
                SampleCount++;
                AssetCount++;
                MaxDepth = Math.Max(MaxDepth, depth);
            }
        }

        private sealed class StateDistributionElement : VisualElement
        {
            private static readonly Color BeginColor = new Color(0.30f, 0.66f, 1f);
            private static readonly Color LoadingColor = new Color(1f, 0.67f, 0.25f);
            private static readonly Color EndColor = new Color(0.30f, 0.82f, 0.52f);

            private readonly VisualElement _beginBar;
            private readonly VisualElement _loadingBar;
            private readonly VisualElement _endBar;
            private readonly Label _legend;

            public StateDistributionElement()
            {
                style.height = 42f;

                var bar = new VisualElement();
                bar.style.height = 12f;
                bar.style.flexDirection = FlexDirection.Row;
                bar.style.backgroundColor = new Color(0.10f, 0.11f, 0.13f);
                bar.style.borderTopLeftRadius = 6f;
                bar.style.borderTopRightRadius = 6f;
                bar.style.borderBottomLeftRadius = 6f;
                bar.style.borderBottomRightRadius = 6f;
                bar.style.overflow = Overflow.Hidden;

                _beginBar = CreateBar(BeginColor);
                _loadingBar = CreateBar(LoadingColor);
                _endBar = CreateBar(EndColor);
                bar.Add(_beginBar);
                bar.Add(_loadingBar);
                bar.Add(_endBar);
                Add(bar);

                _legend = new Label();
                _legend.style.marginTop = 6f;
                _legend.style.fontSize = 11f;
                _legend.style.color = new Color(0.75f, 0.77f, 0.82f);
                Add(_legend);
                SetValues(0, 0, 0);
            }

            public void SetValues(int begin, int loading, int ended)
            {
                var total = loading + ended;
                _beginBar.style.flexGrow = 0f; // total == 0 ? 0f : begin;
                _loadingBar.style.flexGrow = total == 0 ? 0f : loading;
                _endBar.style.flexGrow = total == 0 ? 0f : ended;
                _beginBar.style.display = DisplayStyle.None; //begin == 0 ? DisplayStyle.None : DisplayStyle.Flex;
                _loadingBar.style.display = loading == 0 ? DisplayStyle.None : DisplayStyle.Flex;
                _endBar.style.display = ended == 0 ? DisplayStyle.None : DisplayStyle.Flex;
                _legend.text = $"● Begin  {begin}     ● Loading  {loading}     ● End  {ended}";
            }

            private static VisualElement CreateBar(Color color)
            {
                var element = new VisualElement();
                element.style.backgroundColor = color;
                element.style.minWidth = 2f;
                return element;
            }
        }
    }
}