using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
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
        public IReadOnlyDictionary<string, AssetReferenceNode> AssetNodes => _assetNodes;

        private readonly Dictionary<string, AssetReferenceNode> _assetNodes;
        
        private static Capabilities _capabilities =
            ~(Capabilities.Deletable | Capabilities.Copiable);

        public BundleReferenceBundleNode(BundleReferenceData data)
        {
            BundleName = data?.bundleName ?? string.Empty;
            _assetNodes = new Dictionary<string, AssetReferenceNode>();
            title = data?.bundleName ?? "Bundle (数据为空)"; // BuildTitle(data);
            style.width = NodeWidth;
            style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
            // tooltip = BuildTooltip(data);
            capabilities &= _capabilities;
            
            var label = new Label("引用关系");
            label.text = $"引用 {data.bundleReferenced?.Count ?? 0}  被引用 {data.bundleDependent?.Count ?? 0}";
            contentContainer.Add(label);
            if (data.defectLevel > DefectLevel.None)
            {
                var label2 = new Label("缺陷");
                label2.text = $"缺陷等级: {GetDefectLevelText(data.defectLevel)}";
                var label3 = new Label();
                label3.text = $"缺陷内容: {string.Join("、", data.tags)}";
                contentContainer.Add(label2);
                contentContainer.Add(label3);
            }

            ApplyDefectStyle(data?.defectLevel ?? DefectLevel.None);

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
            var color = GetDefectColor(level);
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
                   $"缺陷等级: {GetDefectLevelText(data.defectLevel)}\n" +
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

                RegisterCallback<MouseDownEvent>(_ => PingAsset(AssetPath));
                RefreshExpandedState();
            }
        }

        private static void PingAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return;

            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (asset == null)
                return;

            // Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private static Color GetDefectColor(DefectLevel level)
        {
            if ((level & DefectLevel.High) != 0)
                return new Color(0.75f, 0.18f, 0.18f, 1f);
            if ((level & DefectLevel.Medium) != 0)
                return new Color(0.85f, 0.52f, 0.12f, 1f);
            if ((level & DefectLevel.Low) != 0)
                return new Color(0.72f, 0.65f, 0.12f, 1f);
            return Color.clear;
        }

        private static string GetDefectLevelText(DefectLevel level)
        {
            if ((level & DefectLevel.High) != 0)
                return "高";
            if ((level & DefectLevel.Medium) != 0)
                return "中";
            if ((level & DefectLevel.Low) != 0)
                return "低";
            return "无";
        }

    }
}