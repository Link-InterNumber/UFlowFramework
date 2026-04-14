using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Unity.VisualScripting;
using System;
using UFlowFramework.DataStructure;

namespace PowerCellStudio
{
    public class NotifyGraphView : GraphView
    {
        private EditorWindow _editorWindow;

        public NotifyGraphView(EditorWindow editorWindow)
        {
            _editorWindow = editorWindow;
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            // this.deleteSelection += OnDeleteSelection;
            // this.graphViewChanged += OnGraphViewChanged;
            // 背景网格
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            // styleSheets.Add(Resources.Load<StyleSheet>("NotifyGraphStyles"));
            AddNode("Root", Vector2.one * 200);

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

            // 设置初始缩放比例
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
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
            AddNode("Root", Vector2.one * 200);
        }

        // private void OnDeleteSelection(string operationName, AskUser askUser)
        // {
        //     CheckNodeDuplicate();
        // }

        // private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        // {
        //     if (graphViewChange.elementsToRemove != null)
        //     {
        //         var allNodes = nodes.ToList();
        //         foreach (var item in graphViewChange.elementsToRemove)
        //         {
        //             var index = allNodes.FindIndex(o => o.title == item.title);
        //             if (index >= 0)
        //             {
        //                 allNodes.RemoveAt(index);
        //             }
        //         }
        //         var nameSet = new HashSet<string>(nodes.Select(n => n.title));
        //         var duplicateNames = new HashSet<string>();
        //         foreach (var node in allNodes)
        //         {
        //             var name = node.title;
        //             if (nameSet.Contains(name))
        //             {
        //                 nameSet.Remove(name);
        //             }
        //             else
        //             {
        //                 duplicateNames.Add(name);
        //             }
        //         }
        //         foreach (var node in allNodes)
        //         {
        //             var notifyNode = node as NotifyNodeView;
        //             if (notifyNode == null) continue;
        //             var name = notifyNode.GetNodeName();
        //             if (duplicateNames.Contains(name))
        //             {
        //                 // 重复时背景变红
        //                 notifyNode.style.backgroundColor = new StyleColor(Color.red);
        //             }
        //             else
        //             {
        //                 notifyNode.style.backgroundColor = new StyleColor(Color.clear);
        //             }
        //         }
        //     }
        //     return graphViewChange;
        // }

        public bool CheckNodeDuplicate()
        {
            var allNodes = nodes.ToList();
            var nameSet = new HashSet<string>(nodes.Select(n => n.title));
            var duplicateNames = new HashSet<string>();
            foreach (var node in allNodes)
            {
                var name = node.title;
                if (nameSet.Contains(name))
                {
                    nameSet.Remove(name);
                }
                else
                {
                    duplicateNames.Add(name);
                }
            }
            foreach (var node in allNodes)
            {
                var notifyNode = node as NotifyNodeView;
                if (notifyNode == null) continue;
                var name = notifyNode.GetNodeName();
                if (duplicateNames.Contains(name))
                {
                    // 重复时背景变红
                    notifyNode.style.backgroundColor = new StyleColor(Color.red);
                }
                else
                {
                    notifyNode.style.backgroundColor = new StyleColor(Color.clear);
                }
            }
            return duplicateNames.Count > 0;
        }

        public void FindNodeByNamePrompt(string nodeName)
        {
            if (string.IsNullOrEmpty(nodeName)) return;
            var lowerName = nodeName.ToLower();
            var targetNode = nodes.ToList().Find(n =>
            {
                var notifyNode = n as NotifyNodeView;
                if (notifyNode == null) return false;
                return notifyNode.GetNodeName().ToLower() == lowerName;
            });

            if (targetNode != null)
            {
                ClearSelection();
                AddToSelection(targetNode);
                FrameSelection();
            }
            else
            {
                EditorUtility.DisplayDialog("Find Node", $"Node with name '{nodeName}' not found.", "OK");
            }
        }

        public NotifyNodeView AddNode(string nodeName, Vector2 position)
        {
            var nodeView = new NotifyNodeView(nodeName, this);
            nodeView.SetPosition(new Rect(position, Vector2.zero));
            AddElement(nodeView);
            CheckNodeDuplicate();
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
        public void AutoLayout(float horizontalSpacing = 220f, float verticalSpacing = 80f, Vector2 startOffset = default)
        {
            if (startOffset == default) startOffset = new Vector2(100, 100);

            var allNodes = nodes.ToList();
            if (allNodes.Count == 0) return;

            Vector2 GetNodeSize(Node node)
            {
                var size = node.GetPosition().size;
                return size == Vector2.zero ? new Vector2(180, 120) : size;
            }

            Port GetInputPort(Node node)
            {
                return node.inputContainer.Query<Port>().ToList().FirstOrDefault();
            }

            Port GetOutputPort(Node node)
            {
                return node.outputContainer.Query<Port>().ToList().FirstOrDefault();
            }

            bool HasConnectedInput(Node node)
            {
                var inputPort = GetInputPort(node);
                return inputPort != null && inputPort.connected;
            }

            bool IsRootNode(Node node)
            {
                return (node as NotifyNodeView)?.GetNodeName() == "Root";
            }

            List<Node> GetChildren(Node node)
            {
                var outputPort = GetOutputPort(node);
                if (outputPort == null) return new List<Node>();

                return outputPort.connections
                    .Select(connection => connection.input?.node)
                    .Where(child => child != null)
                    .Distinct()
                    .OrderBy(child => child.GetPosition().y)
                    .ThenBy(child => child.title)
                    .ToList();
            }

            var orderedNodes = allNodes
                .OrderBy(node => IsRootNode(node) ? 0 : 1)
                .ThenBy(node => node.GetPosition().y)
                .ThenBy(node => node.title)
                .ToList();

            var rootNodes = orderedNodes
                .Where(node => IsRootNode(node) || !HasConnectedInput(node))
                .Distinct()
                .ToList();

            if (rootNodes.Count == 0)
            {
                rootNodes = orderedNodes;
            }

            var defaultNodeSize = orderedNodes
                .Select(GetNodeSize)
                .Aggregate(new Vector2(180f, 120f), (current, size) =>
                    new Vector2(Mathf.Max(current.x, size.x), Mathf.Max(current.y, size.y)));

            var nodeLookup = orderedNodes.ToDictionary(node => node, node => new TreeNode<Node>(node));
            var forestRoots = new List<TreeNode<Node>>();

            void BuildSubtree(TreeNode<Node> current, HashSet<Node> visiting)
            {
                if (!visiting.Add(current.Value))
                {
                    return;
                }

                var children = GetChildren(current.Value);
                for (int i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    if (!nodeLookup.TryGetValue(child, out var childNode))
                    {
                        continue;
                    }

                    if (visiting.Contains(child) || childNode.Parent != null)
                    {
                        continue;
                    }

                    if (!current.AddChild(childNode))
                    {
                        continue;
                    }

                    BuildSubtree(childNode, visiting);
                }

                visiting.Remove(current.Value);
            }

            var layoutRoots = rootNodes.Concat(orderedNodes).Distinct().ToList();
            for (int i = 0; i < layoutRoots.Count; i++)
            {
                var root = nodeLookup[layoutRoots[i]];
                if (root.Parent != null || forestRoots.Contains(root))
                {
                    continue;
                }

                forestRoots.Add(root);
                BuildSubtree(root, new HashSet<Node>());
            }

            float ApplyLayout(TreeNode<Node> current, HashSet<TreeNode<Node>> visiting)
            {
                if (!visiting.Add(current))
                {
                    return float.MinValue;
                }

                var node = current.Value;
                var size = GetNodeSize(node);
                node.SetPosition(new Rect(current.Position, size));
                var maxBottom = current.Position.y + size.y;

                for (int i = 0; i < current.Child.Count; i++)
                {
                    maxBottom = Mathf.Max(maxBottom, ApplyLayout(current.Child[i], visiting));
                }

                visiting.Remove(current);
                return maxBottom;
            }

            var currentTop = startOffset.y;
            for (int i = 0; i < forestRoots.Count; i++)
            {
                var root = forestRoots[i];
                var layoutSettings = new TreeLayoutUtility.LayoutSettings
                {
                    horizontalSpacing = horizontalSpacing,
                    verticalSpacing = verticalSpacing,
                    startOffset = new Vector2(startOffset.x, currentTop),
                    defaultNodeSize = defaultNodeSize,
                    Direction = TreeLayoutUtility.LayoutDirection.Horizontal
                };

                TreeLayoutUtility.CalculateLayout(root, layoutSettings);
                var subtreeBottom = ApplyLayout(root, new HashSet<TreeNode<Node>>());
                currentTop = subtreeBottom + verticalSpacing;
            }

            FrameAll();
        }

    }

}
