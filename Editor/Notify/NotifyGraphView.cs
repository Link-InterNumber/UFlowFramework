using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Unity.VisualScripting;
using System;

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
        public void AutoLayout(float horizontalSpacing = 220f, float verticalSpacing = 150f, Vector2 startOffset = default)
        {
            if (startOffset == default) startOffset = new Vector2(100, 100);

            List<List<Node>> levels = new List<List<Node>>();
            var layer1 = new List<Node>();
            var nodesWithNoInput = nodes.Where(n =>
            {
                if ((n as PowerCellStudio.NotifyNodeView)?.GetNodeName() == "Root") return true;
                var inputPorts = n.inputContainer.Query<Port>().AtIndex(0);
                return !inputPorts.connected;
            }).ToList();
            layer1.AddRange(nodesWithNoInput);

            levels.Add(layer1);
            var buffer = new List<Node>();
            var index = 0;
            while (true)
            {
                foreach (var l in levels[index])
                {
                    var node = l;
                    var connects = node.outputContainer.Query<Port>().AtIndex(0).connections;
                    foreach (var item in connects)
                    {
                        buffer.Add(item.input.node);
                    }
                }
                if (buffer.Count == 0) break;
                levels.Add(buffer);
                buffer = new List<Node>();
                index++;
            }

            for (int i = 0; i < levels.Count; i++)
            {
                int depth = i;
                var list = levels[i];
                int count = list.Count;
                float totalHeight = (count - 1) * verticalSpacing;
                for (int j = 0; j < count; j++)
                {
                    float x = startOffset.x + (i * horizontalSpacing);
                    float y = startOffset.y + j * verticalSpacing - totalHeight / 2f;
                    // 使用节点当前大小（如果为 0 则使用默认）
                    var size = list[j].GetPosition().size;
                    if (size == Vector2.zero) size = new Vector2(180, 120);
                    list[j].SetPosition(new Rect(new Vector2(x, y), size));
                }
            }

            FrameAll();
        }

    }

}
