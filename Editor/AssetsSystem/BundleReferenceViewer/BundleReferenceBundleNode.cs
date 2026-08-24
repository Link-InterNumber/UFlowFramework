using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace PowerCellStudio.Editor
{
    public sealed class BundleReferenceBundleNode : Node
    {
        public const float NodeWidth = 300f;
        public const float ResourceNodeWidth = 270f;
        public const float ResourceNodeHeight = 70f;
        public const float HeaderHeight = 90f;
        public const float ResourceGap = 5f;

        public string BundleName { get; }
        public float ContainerHeight { get; }
        
        public Port InputPort { get; }
        public Port OutputPort { get; }
        
        public IReadOnlyDictionary<string, AssetReferenceNode> AssetNodes => _assetNodes;

        private readonly Dictionary<string, AssetReferenceNode> _assetNodes;
        
        private static Capabilities _capabilities =
            ~(Capabilities.Deletable | Capabilities.Copiable);

        public BundleReferenceBundleNode(BundleReferenceData data, bool isSimplifyMode)
        {
            BundleName = data?.bundleName ?? string.Empty;
            _assetNodes = new Dictionary<string, AssetReferenceNode>();
            title = data?.bundleName ?? "Bundle (数据为空)"; // BuildTitle(data);
            titleContainer.style.flexWrap = Wrap.Wrap;
            var titleLabel = titleContainer.Q<Label>();
            if (titleLabel != null)
            {
                titleLabel.style.whiteSpace = WhiteSpace.Normal;
                titleLabel.style.flexGrow = 1f;
                titleLabel.style.flexShrink = 1f;
                titleLabel.style.minWidth = 0f;
            }

            style.width = NodeWidth;
            style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
            // tooltip = BuildTooltip(data);
            capabilities &= _capabilities;
            
            var label = new Label("引用关系");
            label.text = $"引用 {data.bundleDependent?.Count ?? 0}  被引用 {data.bundleReferenced?.Count ?? 0}";
            contentContainer.Add(label);
            if (data.defectLevel > DefectLevel.None)
            {
                var label2 = new Label("缺陷");
                label2.text = $"缺陷等级: {BundleReferenceUtils.GetDefectLevelText(data.defectLevel)}";
                var label3 = new Label();
                label3.text = $"缺陷内容: {string.Join("、", data.tags)}";
                contentContainer.Add(label2);
                contentContainer.Add(label3);
            }

            ApplyDefectStyle(data?.defectLevel ?? DefectLevel.None);

            if (isSimplifyMode)
            {
                InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input,
                    Port.Capacity.Multi, typeof(bool));
                InputPort.portName = "被引用";
                inputContainer.Add(InputPort);

                OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output,
                    Port.Capacity.Multi, typeof(bool));
                OutputPort.portName = "引用";
                outputContainer.Add(OutputPort);
                
                var scrollView = new ScrollView(ScrollViewMode.Vertical);
                scrollView.style.flexDirection = FlexDirection.Column;
                scrollView.style.height = 150f;
                scrollView.style.width = ResourceNodeWidth;
                
                var assets = data?.assets;
                if (assets != null)
                {
                    for (var i = 0; i < assets.Count; i++)
                    {
                        var asset = assets[i];
                        var assetLabel = new Label();
                        var assetPath = asset.assetPath;
                        assetLabel.text = assetPath;
                        assetLabel.RegisterCallback<MouseDownEvent>(_ => BundleReferenceUtils.PingAsset(assetPath));
                        scrollView.Add(assetLabel);
                    }
                }
                contentContainer.Add(scrollView);
                ContainerHeight = 200f + HeaderHeight;
                style.height = ContainerHeight;
                style.minHeight = ContainerHeight;
                style.maxHeight = ContainerHeight;
            }
            else
            {
                var assets = data?.assets;
                var assetCount = 0;
                if (assets != null)
                {
                    for (var i = 0; i < assets.Count; i++)
                    {
                        var asset = assets[i];
                        if (asset == null || string.IsNullOrEmpty(asset.assetPath))
                            continue;

                        var assetNode = new AssetReferenceNode(asset);
                        _assetNodes[asset.assetPath] = assetNode;
                        contentContainer.Add(assetNode);
                        assetCount++;
                    }
                }

                ContainerHeight = Mathf.Max(HeaderHeight + 16f,
                    HeaderHeight + assetCount * (ResourceNodeHeight + ResourceGap) + 10f);
                style.height = ContainerHeight;
                style.minHeight = ContainerHeight;
                style.maxHeight = ContainerHeight;
            }
            RefreshExpandedState();
        }

        // private static string BuildTitle(BundleReferenceData data)
        // {
        //     if (data == null)
        //         return "Bundle (数据为空)";
        //
        //     return $"{data.bundleName}  |  引用 {data.bundleReferenced?.Count ?? 0}  被引用 {data.bundleDependent?.Count ?? 0}";
        // }

        private void ApplyDefectStyle(DefectLevel level)
        {
            var color = BundleReferenceUtils.GetDefectColor(level);
            if (color.a <= 0f)
                return;

            style.borderLeftWidth = 4f;
            style.borderLeftColor = color;
        }

        private static string BuildTooltip(BundleReferenceData data)
        {
            if (data == null)
                return "Bundle 数据为空";

            var defects = data.tags == null || data.tags.Count == 0
                ? "无"
                : string.Join("、", data.tags);
            return $"Bundle: {data.bundleName}\n" +
                   $"依赖: {data.bundleDependent?.Count ?? 0}\n" +
                   $"被引用: {data.bundleReferenced?.Count ?? 0}\n" +
                   $"缺陷等级: {BundleReferenceUtils.GetDefectLevelText(data.defectLevel)}\n" +
                   $"缺陷内容: {defects}";
        }

        public void LayoutAssets()
        {
            var index = 0;
            foreach (var assetNode in _assetNodes.Values)
            {
                assetNode.SetPosition(new Rect(
                    15f,
                    HeaderHeight + index * (ResourceNodeHeight + ResourceGap),
                    ResourceNodeWidth,
                    ResourceNodeHeight));
                index++;
            }
        }

        public sealed class AssetReferenceNode : Node
        {
            public string AssetPath { get; }
            public Port InputPort { get; }
            public Port OutputPort { get; }

            private static Capabilities _capabilities =
                ~(Capabilities.Movable | Capabilities.Deletable | Capabilities.Copiable);

            private const float HighlightOutlineWidth = 2f;

            public AssetReferenceNode(AssetReferenceData data)
            {
                AssetPath = data.assetPath;
                title = Path.GetFileName(AssetPath);
                capabilities &= _capabilities;
                tooltip = AssetPath;
                style.position = Position.Absolute;
                style.width = ResourceNodeWidth;
                style.height = ResourceNodeHeight;
                style.minHeight = ResourceNodeHeight;
                style.maxHeight = ResourceNodeHeight;

                InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input,
                    Port.Capacity.Multi, typeof(bool));
                InputPort.portName = "被引用";
                inputContainer.Add(InputPort);

                OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output,
                    Port.Capacity.Multi, typeof(bool));
                OutputPort.portName = "引用";
                outputContainer.Add(OutputPort);

                RegisterCallback<MouseDownEvent>(OnMouseDown);
                RefreshExpandedState();
            }

            private void OnMouseDown(MouseDownEvent evt)
            {
                BundleReferenceUtils.PingAsset(AssetPath);

                var graphView = GetFirstAncestorOfType<BundleReferenceGraphView>();
                graphView?.HighlightDownstream(OutputPort);
            }

            public void SetHighlight(bool highlighted)
            {
                style.borderLeftWidth = highlighted ? HighlightOutlineWidth : 0f;
                style.borderLeftColor = highlighted ? Color.green : Color.clear;
            }
        }

    }
}