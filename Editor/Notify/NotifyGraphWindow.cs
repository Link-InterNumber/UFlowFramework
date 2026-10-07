using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.UIElements;

namespace PowerCellStudio.Editor
{
    public class NotifyGraphWindow : EditorWindow
    {
        private NotifyGraphView _graphView;
        private const string _savePath = "Assets/UFlowFramework/Systems/NotifySystem";
        private const string _enumPath = "Assets/UFlowFramework/Systems/NotifySystem";

        private const string _savePathSaveKey = "NotifyGraphWindow_SavePath";
        private const string _enumPathSaveKey = "NotifyGraphWindow_EnumPath";
        private const string _enumTypeSaveKey = "NotifyGraphWindow_EnumType";
        private const string _namespaceSaveKey = "NotifyGraphWindow_Namespace";
        private const string _bindingCsPathSaveKey = "NotifyGraphWindow_BindingCsPath";

        private string _currentSavePath;
        private string _currentEnumPath;
        private string _currentEnumType;
        private string _currentNamespace;

        public static void OpenWindow()
        {
            NotifyGraphWindow window = GetWindow<NotifyGraphWindow>();
            window.titleContent = new GUIContent("Notify Graph");
            window.minSize = new Vector2(760f, 480f);
        }

        void OnDestroy()
        {
            _bindingCsFileField = null;
            _graphView = null;
            _currentSavePath = null;
            _currentEnumPath = null;
        }

        private string _findNodeName = "";
        private string _currentBindingCsPath = "";
        private ObjectField _bindingCsFileField;
        private TextField _namespaceField;
        private TextField _enumTypeField;

        private void OnEnable()
        {
            minSize = new Vector2(760f, 480f);
            _currentSavePath = EditorSaveUtils.GetEditorPref(_savePathSaveKey, _savePath);
            _currentEnumPath = EditorSaveUtils.GetEditorPref(_enumPathSaveKey, _enumPath);
            _currentEnumType = EditorSaveUtils.GetEditorPref(_enumTypeSaveKey, "");
            _currentNamespace = EditorSaveUtils.GetEditorPref(_namespaceSaveKey, "");
            _currentBindingCsPath = EditorSaveUtils.GetEditorPref(_bindingCsPathSaveKey, "");

            rootVisualElement.Clear();
            EditorUIStyle.ApplyRoot(rootVisualElement);
            rootVisualElement.Add(EditorUIStyle.CreateHeader(
                "Notify Graph",
                "Design notification hierarchies, validate node names, and generate binding code."));

            _graphView = new NotifyGraphView(this)
            {
                name = "Notify Graph View"
            };

            Toolbar toolbarText = new Toolbar();
            EditorUIStyle.ApplyToolbar(toolbarText);
            
            // namespace
            _namespaceField = new TextField("Namespace");
            ConfigureFlexibleField(_namespaceField, 180f);
            _namespaceField.value = _currentNamespace;
            _namespaceField.RegisterValueChangedCallback(evt =>
            {
                _currentNamespace = evt.newValue;
            });
            toolbarText.Add(_namespaceField);
            
            // enumType
            _enumTypeField = new TextField("Enum Type");
            ConfigureFlexibleField(_enumTypeField, 180f);
            _enumTypeField.value = _currentEnumType;
            _enumTypeField.RegisterValueChangedCallback(evt =>
            {
                _currentEnumType = evt.newValue;
            });
            toolbarText.Add(_enumTypeField);
            rootVisualElement.Add(toolbarText);

            Toolbar pathToolbar = new Toolbar();
            EditorUIStyle.ApplyToolbar(pathToolbar);
            
            // enumPath
            var enumTextField = new TextField("Enum Cs Path");
            ConfigureFlexibleField(enumTextField, 240f);
            enumTextField.value = _currentEnumPath;
            enumTextField.RegisterValueChangedCallback(evt =>
            {
                _currentEnumPath = evt.newValue;
            });
            pathToolbar.Add(enumTextField);
            // savePath
            var saveTextField = new TextField("Binding Cs Path");
            ConfigureFlexibleField(saveTextField, 240f);
            saveTextField.value = _currentSavePath;
            saveTextField.RegisterValueChangedCallback(evt =>
            {
                _currentSavePath = evt.newValue;
            });
            pathToolbar.Add(saveTextField);
            rootVisualElement.Add(pathToolbar);

            Toolbar toolbar = new Toolbar();
            EditorUIStyle.ApplyToolbar(toolbar, 8f);
            // 脚本文件
            _bindingCsFileField = new ObjectField("Binding Cs File");
            _bindingCsFileField.objectType = typeof(UnityEngine.TextAsset);
            ConfigureFlexibleField(_bindingCsFileField, 220f);
            _bindingCsFileField.value = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(_currentBindingCsPath);
            _bindingCsFileField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == null)
                {
                    _graphView.ClearGraph();
                    return;
                }
                var path = AssetDatabase.GetAssetPath(evt.newValue);
                var fileName = Path.GetFileNameWithoutExtension(path);
                if (!fileName.StartsWith("NotifyPreset_"))
                {
                    EditorUtility.DisplayDialog("Binding Cs File", "Binding Cs file name must start with 'NotifyPreset_'.", "OK");
                    _bindingCsFileField.SetValueWithoutNotify(evt.previousValue);
                    return;
                }
                _currentBindingCsPath = path;
            });
            toolbar.Add(_bindingCsFileField);
            // 读取按钮
            var loadButton = new Button(() =>
            {
                if (!string.IsNullOrEmpty(_currentBindingCsPath) && File.Exists(_currentBindingCsPath))
                {
                    _graphView.ClearGraph();
                    ReadBindingCs(_currentBindingCsPath);
                }
                else
                {
                    EditorUtility.DisplayDialog("Load Binding Cs", "Binding Cs file not found!", "OK");
                }
            }){ text = "Load Binding Cs" };
            ConfigureToolbarButton(loadButton, 105f);
            toolbar.Add(loadButton);
            // 添加保存按钮
            var saveButton = new Button(SaveGraph) { text = "Save Graph" };
            ConfigurePrimaryButton(saveButton, 92f);
            toolbar.Add(saveButton);
            // 添加保存按钮
            var autoLayout = new Button(() => _graphView.AutoLayout()) { text = "Auto Layout" };
            ConfigureToolbarButton(autoLayout, 86f);
            toolbar.Add(autoLayout);
            // 检查按钮
            var checkButton = new Button(() => _graphView.CheckNodeDuplicate()) { text = "Check Node" };
            ConfigureToolbarButton(checkButton, 88f);
            toolbar.Add(checkButton);
            // 查找按钮
            var findButton = new Button(() => _graphView.FindNodeByNamePrompt(_findNodeName)) { text = "Find Node" };
            ConfigureToolbarButton(findButton, 82f);
            toolbar.Add(findButton);
            var findTextField = new TextField("Search");
            ConfigureFlexibleField(findTextField, 120f);
            findTextField.RegisterValueChangedCallback(evt =>
            {
                _findNodeName = evt.newValue;
            });
            toolbar.Add(findTextField);
            rootVisualElement.Add(toolbar);

            _graphView.style.flexGrow = 1f;
            _graphView.style.minHeight = 220f;
            rootVisualElement.Add(_graphView);

            // 添加键盘监听（Ctrl/Cmd + S）
            rootVisualElement.focusable = true;
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown);
            rootVisualElement.Focus();

            if (!string.IsNullOrEmpty(_currentBindingCsPath) && File.Exists(_currentBindingCsPath))
            {
                _graphView.ClearGraph();
                ReadBindingCs(_currentBindingCsPath);
            }
        }

        private static void ConfigureFlexibleField(VisualElement field, float minWidth)
        {
            field.style.flexGrow = 1f;
            field.style.flexShrink = 1f;
            field.style.minWidth = minWidth;
            field.style.marginRight = 6f;
        }

        private static void ConfigureToolbarButton(Button button, float width)
        {
            button.style.width = width;
            button.style.height = EditorUIStyle.SecondaryButtonHeight;
        }

        private static void ConfigurePrimaryButton(Button button, float width)
        {
            ConfigureToolbarButton(button, width);
            button.style.backgroundColor = EditorUIStyle.AccentColor;
            button.style.color = Color.white;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
        }

        private void ReadBindingCs(string path)
        {
            // 读取 NotifyPreset_Binding.cs 中的节点关系
            var nodeRelations = new List<(string child, string parent)>();
            var bindFilePath = path;
            if (File.Exists(bindFilePath))
            {
                var csLines = File.ReadAllLines(bindFilePath);
                foreach (var line in csLines)
                {
                    if (line.Contains("namespace"))
                    {
                        _currentNamespace = line.Trim().Replace("namespace", "").Trim();
                        _namespaceField.SetValueWithoutNotify(_currentNamespace);
                    }
                    if (line.Contains("manager.SetNodeParent"))
                    {
                        var trimLine = line.Trim();
                        if (trimLine.StartsWith("//")) continue;
                        var parts = trimLine.Replace("manager.SetNodeParent(", "").Replace(");", "").Split(',');
                        if (parts.Length == 2)
                        {
                            var enumType = parts[0].Trim().Split('.')[0];
                            if (!string.Equals(enumType, _currentEnumType))
                            {
                                _currentEnumType = enumType;
                                _enumTypeField.SetValueWithoutNotify(_currentEnumType);
                            }
                            var child = parts[0].Trim().Replace($"{_currentEnumType}.", "");
                            var parent = parts[1].Trim().Replace($"{_currentEnumType}.", "");
                            nodeRelations.Add((child, parent));
                        }
                    }
                }
            }

            // 根据节点关系创建节点并连接
            var nodesDict = new Dictionary<string, NotifyNodeView>();
            nodesDict["Root"] = _graphView.nodes.ToList().Find(n => (n as NotifyNodeView)?.GetNodeName() == "Root") as NotifyNodeView;
            foreach (var (child, parent) in nodeRelations)
            {
                if (!string.IsNullOrEmpty(parent) && !nodesDict.ContainsKey(parent))
                {
                    var parentNode = _graphView.AddNode(parent, Vector2.zero);
                    _graphView.AddElement(parentNode);
                    nodesDict[parent] = parentNode;
                }
                if (!nodesDict.ContainsKey(child))
                {
                    var childNode = _graphView.AddNode(child, Vector2.zero);
                    _graphView.AddElement(childNode);
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
                    _graphView.Add(edge);
                }
            }
            // 自动布局
            rootVisualElement.schedule.Execute(() => _graphView.AutoLayout()).StartingIn(300);
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
            if (_graphView.CheckNodeDuplicate())
            {
                EditorUtility.DisplayDialog("Save Graph", "Please resolve duplicate node names before saving.", "OK");
                return; 
            }
            if (string.IsNullOrEmpty(_currentEnumType))
            {
                EditorUtility.DisplayDialog("Save Graph", "Please specify the Enum Type before saving.", "OK");
                return;
            }
            
            var nodes = _graphView.nodes.ToList();
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

            if (!SaveBinding(relationships)) return;
            SaveEnum(nodeNames);
            EditorUtility.DisplayDialog("Save Graph", "Graph saved successfully!", "OK");
            // 保存路径到 EditorPrefs
            EditorSaveUtils.SetEditorPref(_savePathSaveKey, _currentSavePath);
            EditorSaveUtils.SetEditorPref(_enumPathSaveKey, _currentEnumPath);
            EditorSaveUtils.SetEditorPref(_bindingCsPathSaveKey, _currentBindingCsPath);
            EditorSaveUtils.SetEditorPref(_enumTypeSaveKey, _currentEnumType);
            EditorSaveUtils.SetEditorPref(_namespaceSaveKey, _currentNamespace);
            AssetDatabase.Refresh();
        }

        private void SaveEnum(List<string> nodeNames)
        {
            var enumSavePath = Path.Combine(_currentEnumPath, $"{_currentEnumType}.cs");
            string currentFileContent = null;
            if (File.Exists(enumSavePath))
            {
                currentFileContent = File.ReadAllText(enumSavePath);
            }
            var currentEnumMembers = new HashSet<string>();
            if (!string.IsNullOrEmpty(currentFileContent))
            {
                var lines = currentFileContent.Split('\n');
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim().TrimEnd(',');
                    if (!string.IsNullOrEmpty(trimmedLine) && !trimmedLine.StartsWith("public") && !trimmedLine.StartsWith("namespace") && !trimmedLine.StartsWith("{") && !trimmedLine.StartsWith("}") && !trimmedLine.StartsWith("//") && !trimmedLine.Contains("enum"))
                    {
                        currentEnumMembers.Add(trimmedLine);
                    }
                }
            }
            currentEnumMembers.UnionWith(nodeNames);
            var nodeNamesFinal = currentEnumMembers.ToList();

            nodeNamesFinal.Sort();
            nodeNamesFinal.Remove("Root");
            nodeNamesFinal.Insert(0, "Root");

            using CsWriter csWriter = new CsWriter();
            if (!string.IsNullOrEmpty(_currentNamespace))
            {
                csWriter.WriteLine($"namespace {_currentNamespace}");
                csWriter.StartWriteBody();
            }

            csWriter.WriteLine($"public enum {_currentEnumType}");
            csWriter.StartWriteBody();
            foreach (var name in nodeNamesFinal)
            {
                csWriter.WriteLine(name + ",");
            }
            csWriter.EndWriteBody();
            if (!string.IsNullOrEmpty(_currentNamespace))
                csWriter.EndWriteBody();
            File.WriteAllText(enumSavePath, csWriter.ToString());
        }

        private bool SaveBinding(List<(string child, string parent)> relationships)
        {
            if (string.IsNullOrEmpty(_currentBindingCsPath) || _bindingCsFileField.value == null)
            {
                var defaultPath = $"NotifyPreset_{_currentEnumType}";
                _currentBindingCsPath = EditorUtility.SaveFilePanel("Save Binding Cs, The filename must start with \"NotifyPreset_\"", _currentSavePath, defaultPath, "cs");
                if (string.IsNullOrEmpty(_currentBindingCsPath)) return false;
                // 处理为项目内路径, 去掉Application.dataPath
                _currentBindingCsPath = Path.GetRelativePath(Application.dataPath, _currentBindingCsPath);
                _currentBindingCsPath = Path.Combine("Assets", _currentBindingCsPath);
                Debug.Log($"Set Binding Cs Path: {_currentBindingCsPath}");
            }
            var fileName = Path.GetFileNameWithoutExtension(_currentBindingCsPath);
            string partialName = null;
            if (fileName.StartsWith("NotifyPreset_"))
            {
                partialName = fileName.Substring("NotifyPreset_".Length);
            }
            if (string.IsNullOrEmpty(partialName))
            {
                EditorUtility.DisplayDialog("Save Binding Cs", "Binding Cs file name must start with 'NotifyPreset_'.", "OK");
                return false;
            }
            using CsWriter csWriter = new CsWriter();
            csWriter.WriteUsing("System");
            csWriter.WriteUsing("PowerCellStudio");
            csWriter.Space(2);
            if (!string.IsNullOrEmpty(_currentNamespace))
            {
                csWriter.WriteLine($"namespace {_currentNamespace}");
                csWriter.StartWriteBody();
            }
            csWriter.WriteLine($"public sealed class {fileName} : INotifyBindPreset");
            csWriter.StartWriteBody();
            csWriter.WriteLine($"public void BindNodes(NotifyManager manager)");
            csWriter.StartWriteBody();
            csWriter.WriteLine($"manager.SetNotifyGroup<{_currentEnumType}>();");
            relationships.Sort((a, b) =>
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
                csWriter.WriteLine($"manager.SetNodeParent({_currentEnumType}.{child}, {_currentEnumType}.{parent});");
            }
            csWriter.EndWriteBody();
            csWriter.EndWriteBody();
            if (!string.IsNullOrEmpty(_currentNamespace))
                csWriter.EndWriteBody();
            File.WriteAllText(_currentBindingCsPath, csWriter.ToString());
            return true;
        }
    }
}