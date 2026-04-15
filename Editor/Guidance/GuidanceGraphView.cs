using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Codice.Client.BaseCommands;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerCellStudio
{
    public class GuidanceGraphView : GraphView
    {
        public GuidanceGraphView(GuidanceGraphWindow editorWindow)
        {
            _editorWindow = editorWindow;
            this.AddManipulator(new ContentZoomer() { maxScale = 2f });
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // 背景网格
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            // 右键创建新节点
            this.nodeCreationRequest = context =>
            {
                // 获取鼠标屏幕坐标
                var position = context.screenMousePosition;
                position.x = (position.x - _editorWindow.position.position.x);
                position.y = (position.y - _editorWindow.position.position.y);
                var finalPos = viewTransform.matrix.inverse.MultiplyPoint(position);

                // 创建节点
                AddNode("NewNode", finalPos);
            };

            var providerTypes = ReflectionUtils.GetInstantiableSubtype(typeof(IGuidanceGraphConfigProvider), typeof(GuidanceGraphWindow).Assembly);
            if (providerTypes.Count == 0)
            {
                Debug.LogError("No GuidanceGraphConfigProvider found.");
                return;
            }
            var providerType = providerTypes[0];
            var provider = ReflectionUtils.CreateInstance(providerType) as IGuidanceGraphConfigProvider;
            if (provider == null)
            {
                Debug.LogError("Failed to create GuidanceGraphConfigProvider instance.");
                return;
            }
            _configProvider = provider;
        }

        public Func<int, IGuidanceConfig> confProvider => GetConfig;

        public class TestConfig : IGuidanceConfig
        {
            public int id => 133;

            public int nextGuidance => 233;

            public LocalizationStringRef decs => new LocalizationStringRef() { rawString = "This is a test guidance description." };

            public bool touchScreenToSkip => false;

            public bool blockInteraction => false;

            public GameObjectRef uiPrefab => new GameObjectRef() { assetName = "Assets/Query-Chan-SD/PQAssets/Prefabs/Query-Chan-SD_Gifu.prefab" };
        }

        private IGuidanceGraphConfigProvider _configProvider;

        private IGuidanceConfig GetConfig(int id)
        {
            return _configProvider?.Get(id) ?? null;
        }

        public void ClearGraph()
        {
            if (nodes.Count() == 0)
                return;
            foreach (var node in nodes.ToList())
            {
                if (node.outputContainer.Query<Port>().AtIndex(0).connected)
                {
                    var connections = node.outputContainer.Query<Port>().AtIndex(0).connections.ToList();
                    foreach (var connection in connections)
                    {
                        RemoveElement(connection);
                    }
                }
                RemoveElement(node);
            }
        }

        public void ReadFromConfigs(int configId)
        {
            // 清空现有节点
            ClearGraph();
            if (configId <= 0)
                return;
            var newNodes = new List<GuidanceNodeView>();
            var currentId = configId;
            while (currentId > 0)
            {
                var config = confProvider(currentId);
                if (config == null)
                    break;
                var nodeView = new GuidanceNodeView(config.id, config.uiPrefab.assetName, string.Empty, this);
                nodeView.SetPosition(new Rect(new Vector2(100 * newNodes.Count, 100 * newNodes.Count), Vector2.zero));
                AddElement(nodeView);
                newNodes.Add(nodeView);
                currentId = config.nextGuidance;
            }

            // 连接节点
            for (int i = 0; i < newNodes.Count - 1; i++)
            {
                var outputPort = newNodes[i].outputContainer.Query<Port>().AtIndex(0);
                var inputPort = newNodes[i + 1].inputContainer.Query<Port>().AtIndex(0);
                var edge = outputPort.ConnectTo(inputPort);
                Add(edge);
            }

            AutoLayout();
        }

        public void ReadFromAsset(GuidanceGraphAsset asset)
        {
            // 清空现有节点
            ClearGraph();
            if (asset == null || asset.guidanceIds.Count == 0)
                return;
            var newNodes = new List<GuidanceNodeView>();
            for (int i = 0; i < asset.guidanceIds.Count; i++)
            {
                var nodeView = new GuidanceNodeView(asset.guidanceIds[i], asset.prefabGuids[i], asset.targetNodes[i], this);
                nodeView.SetPosition(new Rect(new Vector2(100 * i, 100 * i), Vector2.zero));
                AddElement(nodeView);
                newNodes.Add(nodeView);
            }
            // 连接节点
            for (int i = 0; i < newNodes.Count - 1; i++)
            {
                var outputPort = newNodes[i].outputContainer.Query<Port>().AtIndex(0);
                var inputPort = newNodes[i + 1].inputContainer.Query<Port>().AtIndex(0);
                var edge = outputPort.ConnectTo(inputPort);
                Add(edge);
            }

            AutoLayout();
        }

        public void WriteAsset(ref GuidanceGraphAsset asset, in IGuidanceGraphWriteHandler writeHandler, string savePath)
        {
            if (asset == null)
                asset = ScriptableObject.CreateInstance<GuidanceGraphAsset>();

            // 检查重复节点
            var names = new HashSet<int>();
            foreach (var node in nodes)
            {
                if (node is GuidanceNodeView guidanceNode)
                {
                    if (names.Contains(guidanceNode.GetGuidanceId()))
                    {
                        EditorUtility.DisplayDialog("GuidanceGraphWindow",
                         $"Duplicate guidance ID found: {guidanceNode.GetGuidanceId()}. Please ensure all guidance IDs are unique.",
                         "OK");
                        return;
                    }
                    if (guidanceNode.GetGuidanceId() <= 0)
                    {
                        EditorUtility.DisplayDialog("GuidanceGraphWindow",
                         $"Invalid guidance ID found: {guidanceNode.GetGuidanceId()}. Please ensure all guidance IDs are greater than zero.",
                         "OK");
                        return;
                    }
                    names.Add(guidanceNode.GetGuidanceId());
                }
            }

            var nodesWithNoInput = nodes.Where(n =>
            {
                var inputPorts = n.inputContainer.Query<Port>().AtIndex(0);
                return !inputPorts.connected;
            }).ToArray();

            for (int i = 0; i < nodesWithNoInput.Length; i++)
            {
                var node = nodesWithNoInput[i];
                var graphAsset = i == 0 ? asset : ScriptableObject.CreateInstance<GuidanceGraphAsset>();
                if (i == 0)
                {
                    graphAsset = asset;
                }
                else
                {
                    graphAsset = ScriptableObject.CreateInstance<GuidanceGraphAsset>();
                    graphAsset.name = $"GuidanceGraphAsset_{(node as GuidanceNodeView).GetGuidanceId()}";
                }
                graphAsset.guidanceIds.Clear();
                graphAsset.prefabGuids.Clear();
                graphAsset.targetNodes.Clear();
                while (node != null)
                {
                    if (node is GuidanceNodeView guidanceNode)
                    {
                        graphAsset.guidanceIds.Add(guidanceNode.GetGuidanceId());
                        graphAsset.prefabGuids.Add(guidanceNode.GetPrefabGuid());
                        graphAsset.targetNodes.Add(guidanceNode.GetTargetNodePath());
                        guidanceNode.AddTagToTarget();
                        writeHandler.Write(guidanceNode);
                    }
                    if (node.outputContainer.Query<Port>().AtIndex(0).connections.Count() == 0)
                    {
                        break;
                    }
                    var nextPort = node.outputContainer.Query<Port>().AtIndex(0);
                    var nextNode = nextPort.connections.First().input.node;
                    node = nextNode;
                }
                var assetSavePath = Path.Combine(savePath, $"{graphAsset.name}.asset");
                if (graphAsset.prefabGuids.Contains(string.Empty))
                {
                    Debug.LogError($"Cannot save GuidanceGraphAsset at: {assetSavePath} because some nodes have empty prefab GUIDs.");
                }
                if (graphAsset.targetNodes.Count == 0)
                {
                    Debug.LogError($"Cannot save GuidanceGraphAsset at: {assetSavePath} because it contains no guidance nodes.");
                }
                // 判断graphAsset是否已经存在于AssetDatabase中，若存在则使用AssetDatabase.UpdateAsset而非CreateAsset
                if (AssetDatabase.Contains(graphAsset))
                {
                    EditorUtility.SetDirty(graphAsset);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"Updated GuidanceGraphAsset at: {assetSavePath}");
                    continue;
                }
                AssetDatabase.CreateAsset(graphAsset, assetSavePath);
                Debug.Log($"Saved GuidanceGraphAsset at: {assetSavePath}");
            }
        }

        private GuidanceGraphWindow _editorWindow;

        public GuidanceNodeView AddNode(string nodeName, Vector2 position)
        {
            var nodeView = new GuidanceNodeView(10086, string.Empty, string.Empty, this);
            nodeView.SetPosition(new Rect(position, Vector2.zero));
            AddElement(nodeView);
            return nodeView;
        }

        public override List<Port> GetCompatiblePorts(Port startAnchor, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();
            foreach (var port in ports.ToList())
            {
                if (startAnchor.node == port.node ||
                    startAnchor.direction == port.direction ||
                    startAnchor.portType != port.portType)
                {
                    continue;
                }
                compatiblePorts.Add(port);
            }
            return compatiblePorts;
        }

        /// <summary>
        /// 简单树形自动布局（按层、等间距排列）。
        /// horizontalSpacing / verticalSpacing 调整间距，startOffset 为起始偏移。
        /// </summary>
        public void AutoLayout(float horizontalSpacing = 350f, float verticalSpacing = 400f, Vector2 startOffset = default)
        {
            if (nodes.Count() == 0)
                return;
            if (startOffset == default) startOffset = new Vector2(100, 100);

            // List<List<Node>> levels = new List<List<Node>>();
            // var layer1 = new List<Node>();
            var nodesWithoutInput = nodes.Where(n =>
            {
                var inputPorts = n.inputContainer.Query<Port>().AtIndex(0);
                return !inputPorts.connected;
            }).ToList();

            // var totalHeight = 0f;
            for (int i = 0; i < nodesWithoutInput.Count; i++)
            {
                // var lineCount = 0;
                var node = nodesWithoutInput[i];
                var count = 0;
                while (node != null)
                {
                    float x = startOffset.x + (count) * horizontalSpacing;
                    float y = startOffset.y + i * verticalSpacing;
                    var size = node.GetPosition().size;
                    if (size == Vector2.zero) size = new Vector2(180, 120);
                    node.SetPosition(new Rect(new Vector2(x, y), size));
                    count++;
                    // lineCount = count / 3;
                    if (node.outputContainer.Query<Port>().AtIndex(0).connections.Count() == 0)
                    {
                        break;
                    }
                    var nextPort = node.outputContainer.Query<Port>().AtIndex(0);
                    var nextNode = nextPort.connections.First().input.node;
                    node = nextNode;
                }
                // totalHeight += verticalSpacing;
            }

            FrameAll();
        }

        
    }
}