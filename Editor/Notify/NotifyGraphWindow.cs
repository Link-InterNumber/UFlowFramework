using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.UIElements;

namespace PowerCellStudio
{
    public class NotifyGraphWindow : EditorWindow
    {
        private NotifyGraphView graphView;
        private const string savePath = "Assets/UFlowFramework/Systems/NotifySystem";
        private const string enumPath = "Assets/UFlowFramework/Systems/NotifySystem";

        private const string savePathSaveKey = "NotifyGraphWindow_SavePath";
        private const string enumPathSaveKey = "NotifyGraphWindow_EnumPath";

        private string _currentSavePath;
        private string _currentEnumPath;

        [MenuItem("Tools/Notify/Editor Graph")]
        public static void OpenWindow()
        {
            NotifyGraphWindow window = GetWindow<NotifyGraphWindow>();
            window.titleContent = new GUIContent("Notify Graph");
        }

        private void OnEnable()
        {
            var nodeRelations = new List<(string child, string parent)>();
            _currentSavePath = EditorPrefs.GetString(savePathSaveKey, savePath);
            _currentEnumPath = EditorPrefs.GetString(enumPathSaveKey, enumPath);
            // 读取 NotifyManager_Binding.cs 中的节点关系
            var bindFilePath = Path.Combine(_currentSavePath, "NotifyManager_Binding.cs");
            if (File.Exists(bindFilePath))
            {
                var csLines = File.ReadAllLines(bindFilePath);
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
            }

            var enumFilePath = Path.Combine(_currentEnumPath, "NotifyType.cs");
            if (File.Exists(enumFilePath))
            {
                var csLines = File.ReadAllLines(enumFilePath);
                foreach (var line in csLines)
                {
                    var trimmedLine = line.Trim().TrimEnd(',');
                    if (!string.IsNullOrEmpty(trimmedLine) && !trimmedLine.StartsWith("public") && !trimmedLine.StartsWith("namespace") && !trimmedLine.StartsWith("{") && !trimmedLine.StartsWith("}") && !trimmedLine.StartsWith("//") && !trimmedLine.Contains("enum"))
                    {
                        var nodeName = trimmedLine;
                        if (nodeName != "Root" && !nodeRelations.Any(nr => nr.child == nodeName || nr.parent == nodeName))
                        {
                            nodeRelations.Add((nodeName, string.Empty));
                        }
                    }
                }
            }

            graphView = new NotifyGraphView(this)
            {
                name = "Notify Graph View"
            };
            graphView.StretchToParentSize();
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.Add(graphView);

            Toolbar toolbarText = new Toolbar();
            // enumPath
            var enumTextField = new TextField("Enum Cs Path");
            enumTextField.style.minWidth = 400;
            enumTextField.value = _currentEnumPath;
            enumTextField.RegisterValueChangedCallback(evt =>
            {
                _currentEnumPath = evt.newValue;
            });
            toolbarText.Add(enumTextField);
            // savePath
            var saveTextField = new TextField("| Binding Cs Path");
            // 设置saveTextField输入框长度
            saveTextField.style.minWidth = 400;
            saveTextField.value = _currentSavePath;
            saveTextField.RegisterValueChangedCallback(evt =>
            {
                _currentSavePath = evt.newValue;
            });
            toolbarText.Add(saveTextField);
            rootVisualElement.Add(toolbarText);

            Toolbar toolbar = new Toolbar();
            // 添加保存按钮
            var saveButton = new Button(SaveGraph) { text = "Save Graph" };
            toolbar.Add(saveButton);
            // 添加保存按钮
            var autoLayout = new Button(() => graphView.AutoLayout()) { text = "Auto Layout" };
            toolbar.Add(autoLayout);
            // 检查按钮
            var checkButton = new Button(() => graphView.CheckNodeDuplicate()) { text = "Check Node" };
            toolbar.Add(checkButton);
            rootVisualElement.Add(toolbar);

            // 根据节点关系创建节点并连接
            var nodesDict = new Dictionary<string, NotifyNodeView>();
            nodesDict["Root"] = graphView.nodes.ToList().Find(n => (n as NotifyNodeView)?.GetNodeName() == "Root") as NotifyNodeView;
            foreach (var (child, parent) in nodeRelations)
            {
                if (!string.IsNullOrEmpty(parent) && !nodesDict.ContainsKey(parent))
                {
                    var parentNode = graphView.AddNode(parent, Vector2.zero);
                    graphView.AddElement(parentNode);
                    nodesDict[parent] = parentNode;
                }
                if (!nodesDict.ContainsKey(child))
                {
                    var childNode = graphView.AddNode(child, Vector2.zero);
                    graphView.AddElement(childNode);
                    nodesDict[child] = childNode;
                }
                if (string.IsNullOrEmpty(parent))
                    continue;
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

            // 添加键盘监听（Ctrl/Cmd + S）
            rootVisualElement.focusable = true;
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown);
            rootVisualElement.Focus();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            // Windows: evt.ctrlKey, macOS: evt.commandKey
            if ((evt.ctrlKey || evt.commandKey) && evt.keyCode == KeyCode.S)
            {
                SaveGraph();
                evt.StopPropagation();
                evt.StopImmediatePropagation();
            }
        }

        private void SaveGraph()
        {
            if (graphView.CheckNodeDuplicate())
            {
                EditorUtility.DisplayDialog("Save Graph", "Please resolve duplicate node names before saving.", "OK");
                return; 
            }
            
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
            EditorUtility.DisplayDialog("Save Graph", "Graph saved successfully!", "OK");
            // 保存路径到 EditorPrefs
            EditorPrefs.SetString(savePathSaveKey, _currentSavePath);
            EditorPrefs.SetString(enumPathSaveKey, _currentEnumPath);
        }

        private void SaveEnum(List<string> nodeNames)
        {
            nodeNames.Sort();
            nodeNames.Remove("Root");
            nodeNames.Insert(0, "Root");
            var enumContent = "namespace PowerCellStudio\n{\n    public enum NotifyType\n    {\n";
            foreach (var name in nodeNames)
            {
                enumContent += $"        {name},\n";
            }
            enumContent += "    }\n}\n";
            var enumSavePath = Path.Combine(_currentEnumPath, "NotifyType.cs");
            File.WriteAllText(enumSavePath, enumContent);
        }

        private void SaveBinding(List<(string child, string parent)> relationships)
        {
            var bindingContent = "using System;\n\nnamespace PowerCellStudio\n{\n    public sealed partial class NotifyManager\n    {\n";
            bindingContent += "        private partial void BindNodes()\n        {\n";
            relationships.Sort((a,b) =>
            {
                if (a.parent == "Root") return -1;
                if (b.parent == "Root") return 1;
                int parentComp = string.Compare(a.parent, b.parent);
                if (parentComp == 0)
                    return string.Compare(a.child, b.child);
                return parentComp;
            });
            foreach (var (child, parent) in relationships)
            {
                bindingContent += $"            SetNodeParent(NotifyType.{child}, NotifyType.{parent});\n";
            }

            bindingContent += "        }\n    }\n}\n";
            var bindSavePath = Path.Combine(_currentSavePath, "NotifyManager_Binding.cs");
            File.WriteAllText(bindSavePath, bindingContent);
        }
    }
}