using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UI;

namespace PowerCellStudio.Editor
{
    public partial class AdvancedUICodeGeneratorWindow
    {
        private bool CanGenerate()
        {
            return _prefab != null && !string.IsNullOrEmpty(_className) && !string.IsNullOrEmpty(_outputFolder) && _nodes.Any(node => node.HasSelectedField);
        }

        private void GenerateScripts()
        {
            if (!AssetDatabase.IsValidFolder(_outputFolder))
            {
                EditorUtility.DisplayDialog("Invalid Folder", "Output folder is not a valid Unity asset folder.", "OK");
                return;
            }

            var selectedFields = GetSelectedFields();
            if (selectedFields.Count == 0)
            {
                EditorUtility.DisplayDialog("No Component Selected", "Please select at least one component.", "OK");
                return;
            }

            var mainClassName = MakeValidTypeName(_className);
            var scriptFiles = new List<ScriptFileInfo>();

            if (_generateVariableWindow)
            {
                scriptFiles.Add(CreateScriptFileInfo(mainClassName, GenerateUIVariableWindowScript(mainClassName, selectedFields), true));
                scriptFiles.Add(CreateScriptFileInfo($"{mainClassName}Ctrl", GenerateUIVariableCtrlScript(mainClassName, selectedFields), false));
            }
            else
            {
                scriptFiles.Add(CreateScriptFileInfo(mainClassName, GenerateUIWindowScript(mainClassName, selectedFields), true));

                if (_generateVirtualWindow)
                {
                    var virtualClassName = $"{mainClassName}VirtualWindow";
                    scriptFiles.Add(CreateScriptFileInfo(virtualClassName, GenerateUIVirtualWindowScript(virtualClassName, mainClassName, selectedFields), false));
                }
            }

            _pendingBindInfo = CreateBindInfo(mainClassName, selectedFields);
            TryWriteScriptFiles(scriptFiles);
        }

        private string GenerateUIWindowScript(string className, List<GeneratedFieldInfo> selectedFields)
        {
            var sb = CreateWriterWithUsings();
            StartNamespace(sb);

            sb.WriteLine($"[WindowInfo(\"{_prefabPath}\")]")
                .WriteLine($"public class {className} : UIWindow")
                .StartWriteBody();

            WriteFields(sb, selectedFields);

            if (_generateVirtualWindow)
            {
                WriteWindowLifecycleWithoutEvents(sb);
            }
            else
            {
                WriteWindowLifecycle(sb, selectedFields);
                WriteEventMethods(sb, selectedFields, string.Empty);
            }

            sb.EndWriteBody();
            EndNamespace(sb);
            return sb.ToString();
        }

        private string GenerateUIVariableWindowScript(string className, List<GeneratedFieldInfo> selectedFields)
        {
            var sb = CreateWriterWithUsings();
            StartNamespace(sb);

            sb.WriteLine($"[WindowInfo(\"{_prefabPath}\")]")
                .WriteLine($"public partial class {className} : UIVariableWindow")
                .StartWriteBody();

            WriteFields(sb, selectedFields);

            sb.StartWriteMethod(CsWriter.MethodSign.Protected, CsWriter.MethodSign.Override, "Type", "GetCtrlType", "object data")
                .WriteLine("return typeof(DefaultCtrl);")
                .EndWriteMethod();

            sb.EndWriteBody();
            EndNamespace(sb);
            return sb.ToString();
        }

        private string GenerateUIVariableCtrlScript(string className, List<GeneratedFieldInfo> selectedFields)
        {
            var sb = CreateWriterWithUsings();
            StartNamespace(sb);

            sb.WriteLine($"public partial class {className}")
                .StartWriteBody()
                .WriteLine("public class DefaultCtrl : UIVariableCtrl<" + className + ">")
                .StartWriteBody()
                .WriteLine("public DefaultCtrl(IUIComponent ui) : base(ui)")
                .StartWriteBody()
                .EndWriteBody()
                .Space();

            sb.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Override, "void", "BindUIEvent");
            foreach (var node in selectedFields)
            {
                WriteAddListener(sb, node, "ctrlUI.");
            }
            sb.EndWriteMethod();

            sb.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Override, "void", "DisbindUIEvent");
            foreach (var node in selectedFields)
            {
                WriteRemoveListener(sb, node, "ctrlUI.");
            }
            sb.EndWriteMethod();

            sb.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Override, "void", "OnOpen", "object data")
                .Space()
                .EndWriteMethod();
            sb.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Override, "void", "OnFocus")
                .Space()
                .EndWriteMethod();
            sb.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Override, "void", "OnClose")
                .Space()
                .EndWriteMethod();

            sb.EndWriteBody();
            sb.EndWriteBody();
            EndNamespace(sb);
            return sb.ToString();
        }

        private string GenerateUIVirtualWindowScript(string className, string windowClassName, List<GeneratedFieldInfo> selectedFields)
        {
            var sb = CreateWriterWithUsings();
            StartNamespace(sb);

            sb.WriteLine($"public class {className} : UIVirtualWindow<{windowClassName}>")
                .StartWriteBody();

            sb.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Override, "void", "RegisterEvent")
                .WriteLine("base.RegisterEvent();");
            foreach (var node in selectedFields)
            {
                WriteAddListener(sb, node, "window.");
            }
            sb.EndWriteMethod();

            sb.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Override, "void", "DeregisterEvent")
                .WriteLine("base.DeregisterEvent();");
            foreach (var node in selectedFields)
            {
                WriteRemoveListener(sb, node, "window.");
            }
            sb.EndWriteMethod();

            sb.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Override, "void", "OnOpen", "object data")
                .Space()
                .EndWriteMethod();
            sb.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Override, "void", "OnFocus")
                .Space()
                .EndWriteMethod();
            sb.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Override, "void", "OnClose")
                .Space()
                .EndWriteMethod();
            WriteEventMethods(sb, selectedFields, string.Empty);

            sb.EndWriteBody();
            EndNamespace(sb);
            return sb.ToString();
        }

        private CsWriter CreateWriterWithUsings()
        {
            var sb = new CsWriter();
            sb.WriteLine("using System;");
            sb.WriteLine("using UnityEngine;");
            sb.WriteLine("using UnityEngine.UI;");
            sb.WriteLine("using TMPro;");
            sb.WriteLine("using PowerCellStudio;");
            sb.Space();
            return sb;
        }

        private void StartNamespace(CsWriter sb)
        {
            if (string.IsNullOrEmpty(_namespaceName)) return;
            sb.WriteLine($"namespace {_namespaceName}");
            sb.StartWriteBody();
        }

        private void EndNamespace(CsWriter sb)
        {
            if (!string.IsNullOrEmpty(_namespaceName)) sb.EndWriteBody();
        }

        private void WriteFields(CsWriter sb, List<GeneratedFieldInfo> selectedFields)
        {
            foreach (var node in selectedFields)
            {
                sb.WriteLine($"// Path: {node.relativePath}")
                    .WriteLine($"public {GetTypeName(node.fieldType)} {node.fieldName};")
                    .Space();
            }
        }

        private void WriteWindowLifecycle(CsWriter sb, List<GeneratedFieldInfo> selectedFields)
        {
            sb.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Override, "void", "RegisterEvent")
                .WriteLine("base.RegisterEvent();");
            foreach (var node in selectedFields)
            {
                WriteAddListener(sb, node, string.Empty);
            }
            sb.EndWriteMethod();

            sb.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Override, "void", "DeregisterEvent")
                .WriteLine("base.DeregisterEvent();");
            foreach (var node in selectedFields)
            {
                WriteRemoveListener(sb, node, string.Empty);
            }
            sb.EndWriteMethod();

            WriteWindowLifecycleWithoutEvents(sb);
        }

        private void WriteWindowLifecycleWithoutEvents(CsWriter sb)
        {
            sb.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Override, "void", "OnOpen", "object data")
                .Space()
                .EndWriteMethod();
            sb.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Override, "void", "OnFocus")
                .Space()
                .EndWriteMethod();
            sb.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Override, "void", "OnClose")
                .Space()
                .EndWriteMethod();
        }

        private void WriteAddListener(CsWriter sb, GeneratedFieldInfo node, string fieldPrefix)
        {
            if (node.interactionComponentType == null) return;
            if (IsCloseButton(node)) return;
            if (node.interactionComponentType == typeof(Button))
                sb.WriteLine($"{fieldPrefix}{node.fieldName}.onClick.AddListener(On{node.methodName}Clicked);");
            else if (node.interactionComponentType == typeof(Toggle))
                sb.WriteLine($"{fieldPrefix}{node.fieldName}.onValueChanged.AddListener(On{node.methodName}ValueChanged);");
            else if (node.interactionComponentType == typeof(Slider))
                sb.WriteLine($"{fieldPrefix}{node.fieldName}.onValueChanged.AddListener(On{node.methodName}ValueChanged);");
            else if (node.interactionComponentType == typeof(InputField))
                sb.WriteLine($"{fieldPrefix}{node.fieldName}.onValueChanged.AddListener(On{node.methodName}ValueChanged);");
            else if (IsListUpdaterType(node.interactionComponentType))
                sb.WriteLine($"{fieldPrefix}{node.fieldName}.AddInteractionListener(On{node.methodName}Interaction);");
        }

        private void WriteRemoveListener(CsWriter sb, GeneratedFieldInfo node, string fieldPrefix)
        {
            if (node.interactionComponentType == null) return;
            if (IsCloseButton(node)) return;
            if (node.interactionComponentType == typeof(Button))
                sb.WriteLine($"{fieldPrefix}{node.fieldName}.onClick.RemoveListener(On{node.methodName}Clicked);");
            else if (node.interactionComponentType == typeof(Toggle))
                sb.WriteLine($"{fieldPrefix}{node.fieldName}.onValueChanged.RemoveListener(On{node.methodName}ValueChanged);");
            else if (node.interactionComponentType == typeof(Slider))
                sb.WriteLine($"{fieldPrefix}{node.fieldName}.onValueChanged.RemoveListener(On{node.methodName}ValueChanged);");
            else if (node.interactionComponentType == typeof(InputField))
                sb.WriteLine($"{fieldPrefix}{node.fieldName}.onValueChanged.RemoveListener(On{node.methodName}ValueChanged);");
            else if (IsListUpdaterType(node.interactionComponentType))
                sb.WriteLine($"{fieldPrefix}{node.fieldName}.RemoveInteractionListener(On{node.methodName}Interaction);");
        }

        private void WriteEventMethods(CsWriter sb, List<GeneratedFieldInfo> selectedFields, string methodPrefix)
        {
            foreach (var node in selectedFields)
            {
                if (node.interactionComponentType == null) continue;
                if (IsCloseButton(node)) continue;
                if (node.interactionComponentType == typeof(Button))
                    sb.StartWriteMethod(CsWriter.MethodSign.Private, CsWriter.MethodSign.None, "void", $"{methodPrefix}On{node.methodName}Clicked")
                        .Space()
                        .EndWriteMethod();
                else if (node.interactionComponentType == typeof(Toggle))
                    sb.StartWriteMethod(CsWriter.MethodSign.Private, CsWriter.MethodSign.None, "void", $"{methodPrefix}On{node.methodName}ValueChanged", "bool isOn")
                        .Space()
                        .EndWriteMethod();
                else if (node.interactionComponentType == typeof(Slider))
                    sb.StartWriteMethod(CsWriter.MethodSign.Private, CsWriter.MethodSign.None, "void", $"{methodPrefix}On{node.methodName}ValueChanged", "float sliderValue")
                        .Space()
                        .EndWriteMethod();
                else if (node.interactionComponentType == typeof(InputField))
                    sb.StartWriteMethod(CsWriter.MethodSign.Private, CsWriter.MethodSign.None, "void", $"{methodPrefix}On{node.methodName}ValueChanged", "string input")
                        .Space()
                        .EndWriteMethod();
                else if (IsListUpdaterType(node.interactionComponentType))
                    sb.StartWriteMethod(CsWriter.MethodSign.Private, CsWriter.MethodSign.None, "void", $"{methodPrefix}On{node.methodName}Interaction", "IListItem item", "int index", "object passData")
                        .Space()
                        .EndWriteMethod();
            }
        }
    }
}