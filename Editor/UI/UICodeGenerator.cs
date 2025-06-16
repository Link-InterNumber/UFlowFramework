using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace PowerCellStudio
{
    public class UICodeGenerator
    {
        private static GameObject _CurrentPrefab;
        private static List<CompInfo> _CompInfos;
        private static string _scriptPath;

        private class CompInfo
        {
            public string gameObjectName;
            public Type compType;
            public string fieldName;
            public string relativePath;
        }

        [MenuItem("Assets/Create UI Script", true)]
        private static bool ValidateCreateUIScript()
        {
            GameObject selected = Selection.activeObject as GameObject;
            return selected != null && PrefabUtility.GetPrefabAssetType(selected) != PrefabAssetType.NotAPrefab;
        }

        private static Type[] _findType = new []{typeof(Button), typeof(Toggle), typeof(Slider), typeof(InputField), typeof(ListUpdater)}

        [MenuItem("Assets/Create UI Script")]
        private static void CreateUIScript()
        {
            _CurrentPrefab = Selection.activeObject as GameObject;
            if (_CurrentPrefab == null) return;

            string prefabName = _CurrentPrefab.name.Replace(" ", "");
            string scriptName = prefabName + "Script";
            string defaultPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(_CurrentPrefab));
            
            _scriptPath = EditorUtility.SaveFilePanelInProject(
                "Save UI Script",
                scriptName,
                "cs",
                "Select save location",
                defaultPath
            );

            if (string.IsNullOrEmpty(_scriptPath)) return;

            // 递归收集所有按钮组件
            _CompInfos = new List<CompInfo>();
            FindAllTargetComps(_findType, _CurrentPrefab.transform, "", _CurrentPrefab.transform.name);
            
            // 处理重复名称
            ResolveDuplicateNames();
            
            // 显示命名空间输入窗口
            ContentInputWindow.ShowWindow(GenerateScript, "Define Namespace", "");
        }

        private static void FindAllTargetComps(Type[] targets, Transform parent, string currentPath, string rootName)
        {
            foreach (Transform child in parent)
            {
                // 生成相对路径（相对于预制体根节点）
                string newPath = string.IsNullOrEmpty(currentPath) 
                    ? child.name 
                    : $"{currentPath}/{child.name}";

                var hasList = false;
                for (var i = 0; i < targets.Length; i ++)
                {
                    var target = targets[i];
                    var comp = child.GetComponent(target);
                    if (comp == null) continue;
                    _CompInfos.Add(new CompInfo
                    {
                        gameObjectName = comp.gameObject.name;
                        compType = target;
                        relativePath = newPath,
                        fieldName = target.Name.ToLower() + MakeValidVariableName(child.name)
                    });
                    if (i == targets.Length -1) hasList = true;
                    break;
                }
                if (hasList) continue;
                
                // 递归查找子节点
                if (child.childCount > 0)
                {
                    FindAllTargetComps(child, newPath, rootName);
                }
            }
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
                return;
            }
            
            string prefabName = _CurrentPrefab.name.Replace(" ", "");
            string fileName = Path.Combine(_scriptPath, $"{prefabName}.cs");

            // 构建脚本内容
            var sb = new CsWriter();

            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using UnityEngine.UI;");
            sb.AppendLine("using PowerCellStudio;");
            sb.Space();
            
            // 添加命名空间
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine($"namespace {namespaceName}");
                sb.StartWriteBody();
            }

            sb.AppendLine($"[WindowInfo(\"{AssetDatabase.GetAssetPath(_CurrentPrefab)}\")]")
            sb.AppendLine($"public class {prefabName} : UIWindow");
            sb.StartWriteBody();

            // 添加按钮字段和路径注释
            foreach (var CompInfo in _CompInfos)
            {
                sb.AppendLine($"[Header(\"Path: {CompInfo.relativePath}\")]")
                    .AppendLine($"public {CompInfo.compType.Name} {CompInfo.fieldName};")
                    .Space();
            }

            sb.StartWriteMethod(MethodSign.Public, MethodSign.Override, "void", "RegisterEvent")
                .AppendLine("base.RegisterEvent();");
            foreach (var CompInfo in _CompInfos)
            {
                AddListenerStr(CompInfo, sb);
            }
            sb.EndWriteMethod();

            sb.StartWriteMethod(MethodSign.Public, MethodSign.Override, "void", "DeregisterEvent")
                .AppendLine("base.DeregisterEvent();")
            foreach (var CompInfo in _CompInfos)
            {
                RemoveListenerStr(CompInfo, sb)
            }
            sb.EndWriteMethod();

            sb.StartWriteMethod(MethodSign.Public, MethodSign.Override, "void", "OnOpen", "object data")
                .Space()
                .EndWriteMethod();
            
            sb.StartWriteMethod(MethodSign.Public, MethodSign.Override, "void", "OnClose")
                .Space()
                .EndWriteMethod();

            sb.StartWriteMethod(MethodSign.Public, MethodSign.Override, "void", "OnClose")
                .Space()
                .EndWriteMethod();

            foreach (var CompInfo in _CompInfos)
            {
                AddListenerMethod(CompInfo, sb);
            }

            sb.EndWriteBody();
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.EndWriteBody();
            }

            // 写入文件
            File.WriteAllText(fileName, sb.ToString());
            AssetDatabase.Refresh();
            
            Debug.Log($"UI script generated at: {_scriptPath}");
            
            // 清理静态变量
            _CurrentPrefab = null;
            _CompInfos = null;
            _scriptPath = null;
        }

        private static void AddListenerStr(CompInfo info, CsWriter sb)
        {
            if (info.compType == typeof(Button))
                sb.AppendLine($"{info.fieldName}.onClick.AddListener(On{info.fieldName}Clicked);");
            else if (info.compType == typeof(Toggle))
                sb.AppendLine($"{info.fieldName}.onValueChanged.AddListener(On{info.fieldName}ValueChanged);");
            else if (info.compType == typeof(Slider))
                sb.AppendLine($"{info.fieldName}.onValueChanged.AddListener(On{info.fieldName}ValueChanged);");
            else if (info.compType == typeof(InputField))
                sb.AppendLine($"{info.fieldName}.onValueChanged.AddListener(On{info.fieldName}ValueChanged);");
            else if (info.compType == typeof(ListUpdater))
                sb.AppendLine($"{info.fieldName}.onItemInteraction += On{info.fieldName}Invoke;");
            // 其他类型可根据需要扩展
        }

        private static void RemoveListenerStr(CompInfo info, CsWriter sb)
        {
            if (info.compType == typeof(Button))
                sb.AppendLine($"{info.fieldName}.onClick.RemoveListener(On{info.fieldName}Clicked);");
            else if (info.compType == typeof(Toggle))
                sb.AppendLine($"{info.fieldName}.onValueChanged.RemoveListener(On{info.fieldName}ValueChanged);");
            else if (info.compType == typeof(Slider))
                sb.AppendLine($"{info.fieldName}.onValueChanged.RemoveListener(On{info.fieldName}ValueChanged);");
            else if (info.compType == typeof(InputField))
                sb.AppendLine($"{info.fieldName}.onValueChanged.RemoveListener(On{info.fieldName}ValueChanged);");
            else if (info.compType == typeof(ListUpdater))
                sb.AppendLine($"{info.fieldName}.onItemInteraction -= On{info.fieldName}Invoke;");
            // 其他类型可根据需要扩展
            return $"// No listener for {info.compType.Name}";
        }

        private static void AddListenerMethod(CompInfo info, CsWriter sb)
        {
            if (info.compType == typeof(Button))
                sb.StartWriteMethod(MethodSign.Private, MethodSign.None, "void", $"On{info.fieldName}Clicked")
                    .Space()
                    .EndWriteMethod();
            else if (info.compType == typeof(Toggle))
                sb.StartWriteMethod(MethodSign.Private, MethodSign.None, "void", $"On{info.fieldName}ValueChanged", "bool isOn")
                    .Space()
                    .EndWriteMethod();
            else if (info.compType == typeof(Slider))
                sb.StartWriteMethod(MethodSign.Private, MethodSign.None, "void", $"On{info.fieldName}ValueChanged", "float sliderValue")
                    .Space()
                    .EndWriteMethod();
            else if (info.compType == typeof(InputField))
                sb.StartWriteMethod(MethodSign.Private, MethodSign.None, "void", $"On{info.fieldName}ValueChanged", "string input")
                    .Space()
                    .EndWriteMethod();
            else if (info.compType == typeof(ListUpdater))
                sb.StartWriteMethod(MethodSign.Private, MethodSign.None, "void", $"On{info.fieldName}Invoke", "IListItem item", "int index", "object passData")
                    .Space()
                    .EndWriteMethod();
        }

        // 生成有效的变量名
        private static string MakeValidVariableName(string input)
        {
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
    }
}

