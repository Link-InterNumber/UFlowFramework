using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Unity.VisualScripting;

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

            // styleSheets.Add(Resources.Load<StyleSheet>("NotifyGraphStyles"));
            AddElement(CreateNode("Root", Vector2.one * 200, true));

            // 右键创建新节点
            this.nodeCreationRequest = context =>
            {
                // 获取鼠标屏幕坐标
                var position = context.screenMousePosition;
                position.x = (position.x - _editorWindow.position.position.x) / scale;
                position.y = (position.y - _editorWindow.position.position.y) / scale;
                
                // 创建节点
                AddNode("NewNode", position);
            };
            // 背景网格
            var grid = new GridBackground();
            grid.AddToClassList("Grid");
            Insert(0, grid);
            grid.StretchToParentSize();
            // 设置初始缩放比例
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

        }

        public void AddNode(string nodeName, Vector2 position)
        {
            var node = CreateNode(nodeName, position);
            AddElement(node);
        }

        public NotifyNodeView CreateNode(string nodeName, Vector2 position, bool isRoot = false)
        {
            var nodeView = new NotifyNodeView(nodeName, this);
            nodeView.SetPosition(new Rect(position, Vector2.zero));
            return nodeView;
        }

        public void SaveGraph()
        {
            var nodes = GetNodes();
            var notifyTypeLines = new List<string>();
            var parentChildLines = new List<string>();

            foreach (var node in nodes)
            {
                if (node is NotifyNodeView notifyNode)
                {
                    notifyTypeLines.Add($"        {notifyNode.GetNodeName()},");
                    foreach (var child in notifyNode.Children())
                    {
                        parentChildLines.Add($"            SetNodeParent(NotifyType.{notifyNode.GetNodeName()}, NotifyType.{(child as NotifyNodeView).GetNodeName()});");
                    }
                }
            }

            var notifyTypeContent = $"namespace PowerCellStudio\n{{\n    public enum NotifyType\n    {{\n        Root = 0,\n{string.Join("\n", notifyTypeLines)}\n    }}\n}}";
            File.WriteAllText("Assets/Scripts/Red/NotifyType.cs", notifyTypeContent);

            var bindingContent = $"private partial void BindNodes()\n{{\n{string.Join("\n", parentChildLines)}\n}}";
            File.WriteAllText("Assets/Scripts/Red/NotifyManager_Binding.cs", bindingContent);
        }

        private IEnumerable<NotifyNodeView> GetNodes()
        {
            return this.Children().OfType<NotifyNodeView>();
        }

        public override List<Port> GetCompatiblePorts(Port startAnchor, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();
            if (startAnchor.capacity == Port.Capacity.Single)
            {

            }
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
                    var size = list[i].GetPosition().size;
                    if (size == Vector2.zero) size = new Vector2(180, 120);
                    list[i].SetPosition(new Rect(new Vector2(x, y), size));
                }
            }

            FrameAll();
        }

    }

}
