using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Compilation;
using UnityEngine.UI;
using Assembly = System.Reflection.Assembly;

namespace PowerCellStudio
{
    public class UICodeGenerator 
    {
        private static GameObject _CurrentPrefab;
        private static List<CompInfo> _CompInfos;
        private static string _scriptPath;
        private static string _prefabPath;
        private static string _namespace;

        private class CompInfo
        {
            // public string gameObjectName;
            public Type compType;
            public string fieldName;
            public string methodName;
            public string relativePath;
        }

        [MenuItem("Assets/UFlow/Create UI Script", true, 1)]
        private static bool ValidateCreateUIScript()
        {
            GameObject selected = Selection.activeObject as GameObject;
            return selected != null && PrefabUtility.GetPrefabAssetType(selected) != PrefabAssetType.NotAPrefab;
        }

        private static Type[] _findType = new[]
            { typeof(Button), typeof(Toggle), typeof(Slider), typeof(InputField), typeof(IListUpdater) };

        private static string[] _prefixes = { "btn", "button", "tgl", "toggle", "sld", "slider", "ipf", "lst", "list", "inputfield" };

        private static Dictionary<string, string> _componentPrefixes = new Dictionary<string, string>()
        {
            { "Button", "Btn" },
            { "Toggle", "Tgl" },
            { "Slider", "Sld" },
            { "InputField", "Ipf" },
            { "IListUpdater", "Lst" },
        };

        [MenuItem("Assets/UFlow/Create UI Script", false, 1)]
        private static void CreateUIScript()
        {
            _CurrentPrefab = Selection.activeObject as GameObject;
            if (_CurrentPrefab == null)
            {
                Dispose();
                return;
            }

            _prefabPath = AssetDatabase.GetAssetPath(_CurrentPrefab);
            string prefabName = _CurrentPrefab.name.Replace(" ", "");
            string scriptName = prefabName;
            string defaultPath = Path.GetDirectoryName(_prefabPath);
            
            _scriptPath = EditorUtility.SaveFilePanelInProject(
                "Save UI Script",
                scriptName,
                "cs",
                "Select save location",
                defaultPath
            );

            if (string.IsNullOrEmpty(_scriptPath))
            {
                Dispose();
                return;
            }

            if (File.Exists(_scriptPath))
            {
                Debug.LogError($"{scriptName} exists At {_scriptPath}");
                Dispose();
                return;
            }

            // 递归收集所有按钮组件
            _CompInfos = new List<CompInfo>();
            FindAllTargetComps(_findType, _CurrentPrefab.transform, "", _CompInfos);
            
            // 处理重复名称
            ResolveDuplicateNames();
            
            // 显示命名空间输入窗口
            ContentInputEditorWindow.ShowWindow(GenerateScript, "Define Namespace", "Namespace", "");
        }

        private static void FindAllTargetComps(Type[] targets, Transform parent, string currentPath, List<CompInfo> compInfos)
        {
            foreach (Transform child in parent)
            {
                // 生成相对路径（相对于预制体根节点）
                string newPath = string.IsNullOrEmpty(currentPath)
                    ? child.name
                    : $"{currentPath}/{child.name}";

                var hasList = false;
                for (var i = 0; i < targets.Length; i++)
                {
                    var target = targets[i];
                    var comp = child.GetComponent(target);
                    if (comp == null) continue;
                    compInfos.Add(new CompInfo
                    {
                        // gameObjectName = comp.gameObject.name,
                        compType = target,
                        relativePath = newPath,
                        fieldName = GetPrefixByType(target, true) + MakeValidVariableName(child.name),
                        methodName = GetPrefixByType(target, false) + MakeValidVariableName(child.name),
                    });
                    if (i == targets.Length - 1) hasList = true;
                    break;
                }
                if (hasList) continue;

                // 递归查找子节点
                if (child.childCount > 0)
                {
                    FindAllTargetComps(_findType, child, newPath, compInfos);
                }
            }
        }
        
        private static string GetPrefixByType(Type type, bool toLower)
        {
            if (_componentPrefixes.TryGetValue(type.Name, out var prefix))
            {
                return toLower ? prefix.ToLower() : prefix;
            }
            return string.Empty;
        }

        // 处理重复的字段名
        private static void ResolveDuplicateNames()
        {
            // 按字段名分组
            var groups = _CompInfos.GroupBy(b => b.fieldName).Where(g => g.Count() > 1);
            
            foreach (var group in groups)
            {
                int index = 0;
                foreach (var CompInfo in group)
                {
                    // 添加索引解决冲突
                    CompInfo.fieldName = $"{CompInfo.fieldName}{index + 1}";
                    CompInfo.methodName = $"{CompInfo.methodName}{index + 1}";
                    index++;
                }
            }
        }

        // 从路径中提取父节点名称
        // private static string GetParentNameFromPath(string path)
        // {
        //     int lastSlash = path.LastIndexOf('/');
        //     if (lastSlash <= 0) return null;
            
        //     // 获取父节点路径部分
        //     string parentPath = path.Substring(0, lastSlash);
            
        //     // 提取最后一个父节点名称
        //     int prevSlash = parentPath.LastIndexOf('/');
        //     return prevSlash >= 0 
        //         ? parentPath.Substring(prevSlash + 1) 
        //         : parentPath;
        // }

        // 生成脚本的核心方法
        public static void GenerateScript(string namespaceName)
        {
            if (_CurrentPrefab == null || _CompInfos == null || string.IsNullOrEmpty(_scriptPath))
            {
                Debug.LogError("UI Script generation failed: Missing parameters");
                Dispose();
                return;
            }
            
            string prefabName = _CurrentPrefab.name.Replace(" ", "");
            // string fileName = Path.Combine(_scriptPath, $"{prefabName}.cs");

            // 构建脚本内容
            var sb = new CsWriter();

            sb.WriteLine("using UnityEngine;");
            sb.WriteLine("using UnityEngine.UI;");
            sb.WriteLine("using PowerCellStudio;");
            sb.Space();

            _namespace = namespaceName;
            // 添加命名空间
            if (!string.IsNullOrEmpty(_namespace))
            {
                sb.WriteLine($"namespace {_namespace}");
                sb.StartWriteBody();
            }

            sb.WriteLine($"[WindowInfo(\"{AssetDatabase.GetAssetPath(_CurrentPrefab)}\")]")
                .WriteLine($"public class {prefabName} : UIWindow")
                .StartWriteBody();

            // 添加按钮字段和路径注释
            foreach (var CompInfo in _CompInfos)
            {
                if (CompInfo.compType == typeof(Button) && CompInfo.fieldName.Contains("close", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                sb.WriteLine($"// Path: {CompInfo.relativePath}")
                    .WriteLine($"public {CompInfo.compType.Name} {CompInfo.fieldName};")
                    .Space();
            }

            sb.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Override, "void", "RegisterEvent")
                .WriteLine("base.RegisterEvent();");
            foreach (var CompInfo in _CompInfos)
            {
                AddListenerStr(CompInfo, sb);
            }
            sb.EndWriteMethod();

            sb.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Override, "void", "DeregisterEvent")
                .WriteLine("base.DeregisterEvent();");
            foreach (var CompInfo in _CompInfos)
            {
                RemoveListenerStr(CompInfo, sb);
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

            foreach (var CompInfo in _CompInfos)
            {
                AddListenerMethod(CompInfo, sb);
            }

            sb.EndWriteBody();
            if (!string.IsNullOrEmpty(_namespace))
            {
                sb.EndWriteBody();
            }

            // 写入文件
            File.WriteAllText(_scriptPath, sb.ToString());
            AssetDatabase.Refresh();
            
            // CompilationPipeline.compilationFinished += AddUIComponentToPrefab;
            // CompilationPipeline.RequestScriptCompilation();
            

            // 清理静态变量
            Debug.Log($"UI script generated at: {_scriptPath}");
            Dispose();
        }
        

        private static void AddListenerStr(CompInfo info, CsWriter sb)
        {
            if (info.compType == typeof(Button) && info.fieldName.Contains("close", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            if (info.compType == typeof(Button))
                sb.WriteLine($"{info.fieldName}.onClick.AddListener(On{info.methodName}Clicked);");
            else if (info.compType == typeof(Toggle))
                sb.WriteLine($"{info.fieldName}.onValueChanged.AddListener(On{info.methodName}ValueChanged);");
            else if (info.compType == typeof(Slider))
                sb.WriteLine($"{info.fieldName}.onValueChanged.AddListener(On{info.methodName}ValueChanged);");
            else if (info.compType == typeof(InputField))
                sb.WriteLine($"{info.fieldName}.onValueChanged.AddListener(On{info.methodName}ValueChanged);");
            else if (info.compType == typeof(IListUpdater))
                sb.WriteLine($"{info.fieldName}.AddInteractionListener(On{info.methodName}Interaction);");
            // 其他类型可根据需要扩展
        }

        private static void RemoveListenerStr(CompInfo info, CsWriter sb)
        {
            if (info.compType == typeof(Button) && info.fieldName.Contains("close", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            if (info.compType == typeof(Button))
                sb.WriteLine($"{info.fieldName}.onClick.RemoveListener(On{info.methodName}Clicked);");
            else if (info.compType == typeof(Toggle))
                sb.WriteLine($"{info.fieldName}.onValueChanged.RemoveListener(On{info.methodName}ValueChanged);");
            else if (info.compType == typeof(Slider))
                sb.WriteLine($"{info.fieldName}.onValueChanged.RemoveListener(On{info.methodName}ValueChanged);");
            else if (info.compType == typeof(InputField))
                sb.WriteLine($"{info.fieldName}.onValueChanged.RemoveListener(On{info.methodName}ValueChanged);");
            else if (info.compType == typeof(IListUpdater))
                sb.WriteLine($"{info.fieldName}.RemoveInteractionListener(On{info.methodName}Interaction);");
            // 其他类型可根据需要扩展
        }

        private static void AddListenerMethod(CompInfo info, CsWriter sb)
        {
            if (info.compType == typeof(Button) && info.fieldName.Contains("close", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            if (info.compType == typeof(Button))
                sb.StartWriteMethod(CsWriter.MethodSign.Private, CsWriter.MethodSign.None, "void", $"On{info.methodName}Clicked")
                    .Space()
                    .EndWriteMethod();
            else if (info.compType == typeof(Toggle))
                sb.StartWriteMethod(CsWriter.MethodSign.Private, CsWriter.MethodSign.None, "void", $"On{info.methodName}ValueChanged", "bool isOn")
                    .Space()
                    .EndWriteMethod();
            else if (info.compType == typeof(Slider))
                sb.StartWriteMethod(CsWriter.MethodSign.Private, CsWriter.MethodSign.None, "void", $"On{info.methodName}ValueChanged", "float sliderValue")
                    .Space()
                    .EndWriteMethod();
            else if (info.compType == typeof(InputField))
                sb.StartWriteMethod(CsWriter.MethodSign.Private, CsWriter.MethodSign.None, "void", $"On{info.methodName}ValueChanged", "string input")
                    .Space()
                    .EndWriteMethod();
            else if (info.compType == typeof(IListUpdater))
                sb.StartWriteMethod(CsWriter.MethodSign.Private, CsWriter.MethodSign.None, "void", $"On{info.methodName}Interaction", "IListItem item", "int index", "object passData")
                    .Space()
                    .EndWriteMethod();
        }

        // 生成有效的变量名
        private static string MakeValidVariableName(string input)
        {
            // 移除input开头的"btn", "tgl", "sld", "ipf", "lst"
            foreach (var prefix in _prefixes)
            {
                if (input.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    input = input.Substring(prefix.Length);
                    break;
                }
            }
            // 移除非字母数字字符，首字母大写
            StringBuilder sb = new StringBuilder();
            bool capitalizeNext = true;

            foreach (char c in input)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(capitalizeNext ? char.ToUpper(c) : c);
                    capitalizeNext = false;
                }
                else
                {
                    capitalizeNext = true;
                }
            }

            return sb.ToString();
        }

        [MenuItem("Assets/UFlow/Add UI Component", false, 2)]
        private static void AddUIComponentToPrefab()
        {
            var prefab = Selection.activeObject as GameObject;
            if (prefab == null) return;
            var scriptName = prefab.name;
            
            var compInfos = new List<CompInfo>();
            FindAllTargetComps(_findType, prefab.transform, "", compInfos);
            var baseType = typeof(UIWindow);
            var types = baseType.Assembly.GetTypes();
            Type uiType = null;// Assembly.Load("Assembly-CSharp").GetType($"Test.{scriptName}");

            foreach (var type in types)
            {
                if (type.IsSubclassOf(baseType) && type.Name == scriptName)
                {
                    uiType = type;
                    break;
                }
            }
            
            if (uiType != null)
            {
                var uiComp = prefab.AddComponent(uiType);
                var closeBtns = new List<Button>();
                foreach (var compInfo in compInfos)
                {
                    if (compInfo.compType == typeof(Button) && compInfo.fieldName.Contains("close", StringComparison.OrdinalIgnoreCase))
                    {
                        var findPath = compInfo.relativePath.Split('/');
                        var btnNode = prefab.transform;
                        foreach (var node in findPath)
                        {
                            btnNode = btnNode.Find(node);
                        }
                        var btnComp = btnNode.gameObject.GetComponent<Button>();
                        if (btnComp) closeBtns.Add(btnComp);
                        continue;
                    }
                    FieldInfo fields = uiType.GetField(compInfo.fieldName, BindingFlags.Public | BindingFlags.Instance);
                    if (fields == null)
                    {
                        continue;
                    }

                    var nodes = compInfo.relativePath.Split('/');
                    var currentNode = prefab.transform;
                    foreach (var node in nodes)
                    {
                        currentNode = currentNode.Find(node);
                    }
                    var compValue = currentNode.gameObject.GetComponent(compInfo.compType);
                    if(compValue) fields.SetValue(uiComp, compValue);
                }
                if (closeBtns.Count > 0)
                {
                    FieldInfo closeField = uiType.GetField("closeBtn", BindingFlags.Public | BindingFlags.Instance);
                    if (closeField != null)
                    {
                        closeField.SetValue(uiComp, closeBtns.ToArray());
                    }
                }
            }
            else
            {
                Debug.LogError($"Type[{scriptName}] Not Found!");
            }
            PrefabUtility.SavePrefabAsset(prefab);
            AssetDatabase.Refresh();
            Dispose();
        }

        public static void Dispose()
        {
            _CurrentPrefab = null;
            _CompInfos = null;
            _scriptPath = null;
            _prefabPath = null;
            _namespace = null;
        }
    }
}

