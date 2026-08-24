using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerCellStudio.Editor
{
    public sealed class BundleReferenceGraphView : GraphView
    {
        private const float HorizontalSpacing = 360f;
        private const float VerticalGap = 30f;
        private const float VerticalSpacing = 120f;
        private static readonly Color CanvasColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        private readonly Dictionary<string, BundleReferenceBundleNode> _bundleNodeMap =
            new Dictionary<string, BundleReferenceBundleNode>();
        private List<Button> _tipButtons = new List<Button>();

        public BundleReferenceGraphView()
        {
            this.AddManipulator(new ContentZoomer { maxScale = 2f, minScale = 0.2f });
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new LightGridBackground(CanvasColor);
            Insert(0, grid);
            grid.StretchToParentSize();

            graphViewChanged = change => change;
            style.flexGrow = 1f;
        }

        private sealed class LightGridBackground : VisualElement
        {
            public LightGridBackground(Color canvasColor)
            {
                pickingMode = PickingMode.Ignore;
                style.backgroundColor = canvasColor;
            }
        }

        public void ShowBundle(BundleReferenceQueryer queryer, string bundleName,
            BundleDefectDetectorBox defectDetectorBox, bool isSimplifyMode, int referenceDepth)
        {
            var group = queryer?.GetGroupByBundle(bundleName);
            var visibleBundles = group == null
                ? null
                : CollectVisibleBundles(queryer, group, bundleName, referenceDepth);
            ShowBundles(queryer, group, visibleBundles, bundleName, defectDetectorBox, isSimplifyMode);
        }

        public void ShowGroup(BundleReferenceQueryer queryer, string groupName,
            BundleDefectDetectorBox defectDetectorBox, bool isSimplifyMode)
        {
            if (queryer == null || string.IsNullOrEmpty(groupName))
                return;

            if (!queryer.GetAllGroups().TryGetValue(groupName, out var group))
                return;

            ShowBundles(queryer, group, group.bundleNames, null, defectDetectorBox, isSimplifyMode);
        }

        private void ShowBundles(BundleReferenceQueryer queryer, BundleReferenceGroup group,
            IReadOnlyCollection<string> visibleBundles, string focusedBundleName,
            BundleDefectDetectorBox defectDetectorBox, bool isSimplifyMode)
        {
            ClearGraph();
            if (queryer == null || group?.bundleNames == null || visibleBundles == null || visibleBundles.Count == 0)
                return;
            
            // 构建节点
            var nodeMap = new Dictionary<string, BundleReferenceBundleNode>();
            var assetNodeMap = new Dictionary<string, BundleReferenceBundleNode.AssetReferenceNode>();
            group.defectInfos.Clear();
            foreach (var visibleBundle in visibleBundles)
            {
                var bundleInfo = queryer.GetBundleData(visibleBundle);
                defectDetectorBox.DetectBundle(bundleInfo, queryer);
                var node = new BundleReferenceBundleNode(bundleInfo, isSimplifyMode);
                nodeMap.Add(visibleBundle, node);
                _bundleNodeMap[visibleBundle] = node;
                AddElement(node);
                node.LayoutAssets();

                foreach (var assetPair in node.AssetNodes)
                    assetNodeMap[assetPair.Key] = assetPair.Value;
            }

            if (!isSimplifyMode)
            {
                // 建立节点之间的连接
                foreach (var bundleNameInGroup in visibleBundles)
                {
                    var bundleInfo = queryer.GetBundleData(bundleNameInGroup);
                    if (bundleInfo.assets == null)
                        continue;

                    foreach (var asset in bundleInfo.assets)
                    {
                        if (asset == null || !assetNodeMap.TryGetValue(asset.assetPath, out var sourceNode))
                            continue;

                        if (asset.assetDependent == null)
                            continue;
                        foreach (var dependencyPath in asset.assetDependent)
                        {
                            if (!assetNodeMap.TryGetValue(dependencyPath, out var targetNode))
                                continue;
                            AddElement(sourceNode.OutputPort.ConnectTo(targetNode.InputPort));
                        }
                    }
                }
            }
            else
            {
                // 建立节点之间的连接
                foreach (var bundleNameInGroup in visibleBundles)
                {
                    var bundleInfo = queryer.GetBundleData(bundleNameInGroup);
                    if (bundleInfo.bundleDependent == null)
                        continue;

                    foreach (var dependencyBundle in bundleInfo.bundleDependent)
                    {
                        if (!nodeMap.TryGetValue(dependencyBundle, out var targetNode))
                            continue;
                        AddElement(nodeMap[bundleNameInGroup].OutputPort.ConnectTo(targetNode.InputPort));
                    }
                }
            }

            var data = visibleBundles.ToDictionary(name => name, name => queryer.GetBundleData(name));
            Layout(data, nodeMap);
            if (!string.IsNullOrEmpty(focusedBundleName))
                FocusBundle(nodeMap, focusedBundleName);
            
            // 在界面右上角生成 group.defectInfos 的提示按钮
            if (group.defectInfos != null && group.defectInfos.Count > 0)
            {
                var index = 0;
                var topPadding = 10f;
                var rightPadding = 10f;
                var space = 15f;
                foreach (var groupDefectInfo in group.defectInfos)
                {
                    var tag = groupDefectInfo.Key;
                    var info = groupDefectInfo.Value;
                    
                    var defectInfoButton = new Button(null);
                    
                    defectInfoButton.text = tag;
                    var toolTipSb = new StringBuilder();
                    toolTipSb.AppendLine(info.toolTips);
                    toolTipSb.AppendLine("bundle列表：");
                    toolTipSb.Append("    ");
                    toolTipSb.Append(string.Join("\n    ", info.bundleNames));
                    defectInfoButton.tooltip = toolTipSb.ToString();
                    defectInfoButton.style.backgroundColor = BundleReferenceUtils.GetDefectColor(info.level);
                    defectInfoButton.style.position = Position.Absolute;
                    defectInfoButton.style.top = topPadding + (space + topPadding) * index;
                    defectInfoButton.style.right = rightPadding;
                    Add(defectInfoButton);
                    _tipButtons.Add(defectInfoButton);
                    index++;
                }

            }
        }

        private static HashSet<string> CollectVisibleBundles(BundleReferenceQueryer queryer,
            BundleReferenceGroup group, string bundleName, int referenceDepth)
        {
            var result = new HashSet<string> { bundleName };
            var depth = Mathf.Max(0, referenceDepth);
            CollectVisibleBundlesInDirection(queryer, group.bundleNames, bundleName, depth, true, result);
            CollectVisibleBundlesInDirection(queryer, group.bundleNames, bundleName, depth, false, result);
            return result;
        }

        private static void CollectVisibleBundlesInDirection(BundleReferenceQueryer queryer,
            HashSet<string> groupBundles, string startBundle, int maxDepth,
            bool dependencies, HashSet<string> result)
        {
            if (maxDepth <= 0)
                return;

            var queue = new Queue<(string bundleName, int depth)>();
            queue.Enqueue((startBundle, 0));
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current.depth >= maxDepth)
                    continue;

                var data = queryer.GetBundleData(current.bundleName);
                var neighbors = dependencies ? data.bundleDependent : data.bundleReferenced;
                if (neighbors == null)
                    continue;

                foreach (var neighbor in neighbors)
                {
                    if (!groupBundles.Contains(neighbor) || !result.Add(neighbor))
                        continue;
                    queue.Enqueue((neighbor, current.depth + 1));
                }
            }
        }

        public void ClearGraph()
        {
            DeleteElements(edges.ToList());
            DeleteElements(_bundleNodeMap.Values.Cast<GraphElement>().ToList());
            _bundleNodeMap.Clear();
            foreach (var tipButton in _tipButtons)
            {
                Remove(tipButton);
            }
            _tipButtons.Clear();
        }

        public void HighlightDownstream(Port sourcePort)
        {
            ResetAssetNodeHighlight();
            if (sourcePort == null)
                return;

            var visitedPorts = new HashSet<Port>();
            HighlightDownstream(sourcePort, visitedPorts);
        }

        private void HighlightDownstream(Port sourcePort, HashSet<Port> visitedPorts)
        {
            if (sourcePort == null || !visitedPorts.Add(sourcePort))
                return;

            GetAssetNode(sourcePort.node)?.SetHighlight(true);
            foreach (var edge in sourcePort.connections)
            {
                if (edge == null)
                    continue;

                GetAssetNode(edge.input?.node)?.SetHighlight(true);

                HighlightDownstream(GetOutputPort(edge.input?.node), visitedPorts);
            }
        }

        private static BundleReferenceBundleNode.AssetReferenceNode GetAssetNode(Node node)
        {
            return node as BundleReferenceBundleNode.AssetReferenceNode;
        }

        private static Port GetOutputPort(Node node)
        {
            if (node is BundleReferenceBundleNode bundleNode)
                return bundleNode.OutputPort;

            if (node is BundleReferenceBundleNode.AssetReferenceNode assetNode)
                return assetNode.OutputPort;

            return null;
        }

        private void ResetAssetNodeHighlight()
        {
            foreach (var node in _bundleNodeMap.Values)
            {
                foreach (var assetNode in node.AssetNodes.Values)
                    assetNode.SetHighlight(false);
            }
        }

        public void Relayout(BundleReferenceQueryer queryer, string bundleName)
        {
            if (queryer == null || string.IsNullOrEmpty(bundleName))
                return;
            var nodeMap = _bundleNodeMap;
            var group = queryer.GetGroupByBundle(bundleName);
            if (group?.bundleNames == null || !group.bundleNames.Contains(bundleName))
                return;

            var data = group.bundleNames.ToDictionary(name => name, name => queryer.GetBundleData(name));
            Layout(data, nodeMap);
            FocusBundle(nodeMap, bundleName);
        }

        private void FocusBundle(
            IReadOnlyDictionary<string, BundleReferenceBundleNode> nodeMap,
            string bundleName)
        {
            if (!nodeMap.TryGetValue(bundleName, out var node))
                return;

            ClearSelection();
            AddToSelection(node);
            FrameSelection();
            ClearSelection();
        }

        private static HashSet<string> CollectVisibleBundles(
            IReadOnlyDictionary<string, BundleReferenceData> data,
            string bundleName)
        {
            var result = new HashSet<string> { bundleName };
            CollectBundles(data, bundleName, true, result);
            CollectBundles(data, bundleName, false, result);
            return result;
        }

        private static void CollectBundles(
            IReadOnlyDictionary<string, BundleReferenceData> data,
            string start,
            bool referenced,
            HashSet<string> result)
        {
            var queue = new Queue<string>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var neighbors = referenced
                    ? data[current].bundleReferenced
                    : data[current].bundleDependent;
                if (neighbors == null)
                    continue;

                foreach (var neighbor in neighbors)
                {
                    if (!data.ContainsKey(neighbor) || !result.Add(neighbor))
                        continue;
                    queue.Enqueue(neighbor);
                }
            }
        }

        private static void Layout(
            IReadOnlyDictionary<string, BundleReferenceData> data,
            IReadOnlyDictionary<string, BundleReferenceBundleNode> nodeMap)
        {
            if (data == null || data.Count == 0)
                return;

            var levels = new Dictionary<string, int>(data.Count);
            var visited = new HashSet<string>();
            var roots = data.Keys
                .Where(bundleName => IsRoot(data, bundleName))
                .OrderBy(bundleName => bundleName)
                .ToList();

            // A cyclic component has no root. Start it from a stable Bundle name
            // so that the remaining nodes still receive a deterministic layout.
            if (roots.Count == 0)
                roots.Add(data.Keys.OrderBy(bundleName => bundleName).First());

            foreach (var root in roots)
                AssignLevels(data, root, levels, visited);

            // Handle malformed/disconnected input defensively.
            foreach (var bundleName in data.Keys.OrderBy(bundleName => bundleName))
            {
                if (visited.Contains(bundleName))
                    continue;
                AssignLevels(data, bundleName, levels, visited);
            }

            foreach (var level in levels.GroupBy(pair => pair.Value).OrderBy(group => group.Key))
            {
                var y = 60f;
                foreach (var pair in level.OrderBy(item => item.Key))
                {
                    if (nodeMap.TryGetValue(pair.Key, out var node))
                    {
                        var position = new Vector2(
                            80f + pair.Value * HorizontalSpacing,
                            y);
                        node.SetPosition(new Rect(
                            position.x,
                            position.y,
                            BundleReferenceBundleNode.NodeWidth,
                            node.ContainerHeight));
                        y += Mathf.Max(VerticalSpacing, node.ContainerHeight + VerticalGap);
                    }
                }
            }
        }

        private static bool IsRoot(
            IReadOnlyDictionary<string, BundleReferenceData> data,
            string bundleName)
        {
            var referencedBy = data[bundleName].bundleReferenced;
            if (referencedBy == null)
                return true;

            foreach (var bundle in referencedBy)
            {
                if (data.ContainsKey(bundle))
                    return false;
            }

            return true;
        }

        private static void AssignLevels(
            IReadOnlyDictionary<string, BundleReferenceData> data,
            string start,
            Dictionary<string, int> levels,
            HashSet<string> visited)
        {
            var queue = new Queue<string>();
            if (!data.ContainsKey(start) || !visited.Add(start))
                return;

            if (!levels.ContainsKey(start))
                levels[start] = 0;
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var neighbors = data[current].bundleDependent;
                if (neighbors == null)
                    continue;

                foreach (var neighbor in neighbors.OrderBy(item => item))
                {
                    if (!data.ContainsKey(neighbor) || !visited.Add(neighbor))
                        continue;
                    levels[neighbor] = levels[current] + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }
    }
}