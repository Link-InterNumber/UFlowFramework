using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PowerCellStudio
{
    public class NotifyGraphWindow : EditorWindow
    {
        private NotifyGraphView graphView;
        private const string savePath = "Assets/Script/Red/NotifyManager_Binding.cs";
        private const string enumPath = "Assets/Script/Red/NotifyType.cs";

        [MenuItem("Window/Notify Graph")]
        public static void OpenWindow()
        {
            NotifyGraphWindow window = GetWindow<NotifyGraphWindow>();
            window.titleContent = new GUIContent("Notify Graph");
        }

        private void OnEnable()
        {
            // 读取 NotifyManager_Binding.cs 中的节点关系
            var csLines = File.ReadAllLines(savePath);
            var nodeRelations = new List<(string child, string parent)>();
            foreach (var line in csLines)
            {
                if (line.Contains("SetNodeParent"))
                {
                    var parts = line.Trim().Replace("SetNodeParent(", "").Replace(");", "").Split(',');
                    if (parts.Length == 2)
                    {
                        var child = parts[0].Trim().Replace("NotifyType.", "");
                        var parent = parts[1].Trim().Replace("NotifyType.", "");
                        nodeRelations.Add((child, parent));
                    }
                }
            }
            
            graphView = new NotifyGraphView
            {
                name = "Notify Graph View"
            };
            graphView.StretchToParentSize();
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.Add(graphView);
            // 在底部添加保存按钮
            // 底部 footer，用来固定显示 Save 按钮
            var footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.height = 36;
            footer.style.alignItems = Align.Center;
            footer.style.justifyContent = Justify.Center;
            footer.style.paddingLeft = 4;
            footer.style.paddingRight = 4;
            var saveButton = new Button(() => SaveGraph()) { text = "Save Graph" };
            footer.Add(saveButton);
            rootVisualElement.Add(footer);

            // 根据节点关系创建节点并连接
            var nodesDict = new Dictionary<string, NotifyNodeView>();
            nodesDict["Root"] = graphView.nodes.ToList().Find(n => (n as NotifyNodeView)?.GetNodeName() == "Root") as NotifyNodeView;
            foreach (var (child, parent) in nodeRelations)
            {
                if (!nodesDict.ContainsKey(parent))
                {
                    var parentNode = graphView.CreateNode(parent, Vector2.zero);
                    graphView.AddElement(parentNode);
                    nodesDict[parent] = parentNode;
                }
                if (!nodesDict.ContainsKey(child))
                {
                    var childNode = graphView.CreateNode(child, Vector2.zero);
                    graphView.AddElement(childNode);
                    nodesDict[child] = childNode;
                }
                // 创建连接
                var parentPort = nodesDict[parent].outputContainer.Query<Port>().AtIndex(0);
                var childPort = nodesDict[child].inputContainer.Query<Port>().AtIndex(0);
                if (parentPort != null && childPort != null)
                {
                    var edge = parentPort.ConnectTo(childPort);
                    graphView.Add(edge);
                }
            }
            // 自动布局
            graphView.AutoLayout();
        }

        private void SaveGraph()
        {
            var nodes = graphView.nodes.ToList();
            List<string> nodeNames = new List<string>();
            List<(string child, string parent)> relationships = new List<(string, string)>();

            foreach (var node in nodes)
            {
                var notifyNode = node as NotifyNodeView;
                if (notifyNode == null) continue;
                nodeNames.Add(notifyNode.GetNodeName());
                var ports = notifyNode.outputContainer.Query<Port>().ToList();
                foreach (var port in ports)
                {
                    foreach (var edge in port.connections)
                    {
                        var targetNode = edge.input.node as NotifyNodeView;
                        if (targetNode == null) continue;
                        relationships.Add((targetNode.GetNodeName(), notifyNode.GetNodeName()));
                    }
                }
            }

            SaveEnum(nodeNames);
            SaveBinding(relationships);
        }

        private void SaveEnum(List<string> nodeNames)
        {
            var enumContent = "namespace PowerCellStudio\n{\n    public enum NotifyType\n    {\n";
            foreach (var name in nodeNames)
            {
                enumContent += $"        {name},\n";
            }
            enumContent += "    }\n}\n";

            File.WriteAllText(enumPath, enumContent);
        }

        private void SaveBinding(List<(string child, string parent)> relationships)
        {
            var bindingContent = "using System;\n\nnamespace PowerCellStudio\n{\n    public sealed partial class NotifyManager\n    {\n";
            bindingContent += "        private partial void BindNodes()\n        {\n";

            foreach (var (child, parent) in relationships)
            {
                bindingContent += $"            SetNodeParent(NotifyType.{child}, NotifyType.{parent});\n";
            }

            bindingContent += "        }\n    }\n}\n";
            File.WriteAllText(savePath, bindingContent);
        }
    }
}