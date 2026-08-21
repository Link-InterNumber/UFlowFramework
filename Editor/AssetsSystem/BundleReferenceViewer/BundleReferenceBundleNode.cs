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
        public const float HeaderHeight = 76f;
        public const float ResourceGap = 5f;

        public string BundleName { get; }
        public float ContainerHeight { get; }
        public IReadOnlyDictionary<string, AssetReferenceNode> AssetNodes => _assetNodes;

        private readonly Dictionary<string, AssetReferenceNode> _assetNodes;

        public BundleReferenceBundleNode(BundleReferenceData data)
        {
            BundleName = data?.bundleName ?? string.Empty;
            _assetNodes = new Dictionary<string, AssetReferenceNode>();
            title = BuildTitle(data);
            style.width = NodeWidth;
            style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
            tooltip = BuildTooltip(data);

            ApplyDefectStyle(data?.defectLevel ?? DefectLevel.None);
            RegisterCallback<MouseDownEvent>(_ => PingBundle(BundleName));

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

        private static string BuildTitle(BundleReferenceData data)
        {
            if (data == null)
                return "Bundle (数据为空)";

            return $"{data.bundleName}  |  引用 {data.bundleReferenced?.Count ?? 0}  被引用 {data.bundleDependent?.Count ?? 0}";
        }

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

            public AssetReferenceNode(AssetReferenceData data)
            {
                AssetPath = data.assetPath;
                title = Path.GetFileName(AssetPath);
                capabilities &= ~Capabilities.Movable;
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

        private static void PingBundle(string bundleName)
        {
            if (string.IsNullOrEmpty(bundleName))
                return;

            var assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);
            if (assetPaths == null || assetPaths.Length == 0)
                return;

            var asset = AssetDatabase.LoadMainAssetAtPath(assetPaths[0]);
            if (asset == null)
                return;

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private static void PingAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return;

            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (asset == null)
                return;

            Selection.activeObject = asset;
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