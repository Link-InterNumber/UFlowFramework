#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PowerCellStudio
{
    public class ConfigWriterByTemp
    {
        private Dictionary<string, ConfigTypeInfo> _fieldMap;
        private ConfigTypeInfo[] _configTypeInfoList;
        private readonly string _fileName;
        private readonly string _path;
        
        // 模板路径配置
        private const string DataTemplatePath = "Assets/UFlowFramework/Editor/ConfigEditor/ConfDataTemp.txt";
        private const string EditorTemplatePath = "Assets/UFlowFramework/Editor/ConfigEditor/ConfEditorTemp.txt";

        private string _generatedCsString;
        private string _generatedEditorCsString;

        private string extensionName
        {
            get
            {
                switch (ConstSetting.ConfigConfigSaveMode)
                {
                    case ConstSetting.ConfigSaveMode.Json:
                        return "json";
                    case ConstSetting.ConfigSaveMode.Binary:
                        return "bytes";
                    case ConstSetting.ConfigSaveMode.ScriptableObject:
                    default:
                        return "asset";
                }
            }
        }

        public ConfigWriterByTemp(string path, string fileName, Dictionary<string, ConfigTypeInfo> fieldMap)
        {
            _path = path;
            _fileName = fileName;
            _fieldMap = fieldMap;
            _configTypeInfoList = _fieldMap.Values.ToArray();
        }

        public void Clear()
        {
            _generatedCsString = string.Empty;
            _generatedEditorCsString = string.Empty;
        }

        public void GenerateCSString()
        {
            if (!File.Exists(DataTemplatePath))
            {
                ConfigLogger.LogError($"找不到数据类模板: {DataTemplatePath}");
                return;
            }

            var keys = _fieldMap.Values.Where(info => info.isKey).ToList();
            if (keys.Count == 0) keys.Add(_fieldMap.Values.FirstOrDefault());
            
            var template = File.ReadAllText(DataTemplatePath);

            // 构造参数、初始化代码、字段定义
            var stringBuilder = new System.Text.StringBuilder();
            for (var i = 0; i < _configTypeInfoList.Length; i++)
            {
                var info = _configTypeInfoList[i];
                stringBuilder.Append(info.IsList ? $"List<{info.typeName}>" : info.typeName);
                stringBuilder.Append($" {info.fieldName.ToLower()}");
                if (i < _configTypeInfoList.Length - 1)
                {
                    stringBuilder.Append(", ");
                }

                if (i % 4 == 0)
                    stringBuilder.Append("\n\t\t");
            }
            var inputParameters = stringBuilder.ToString();
            
            var initCode = string.Join("\n", _configTypeInfoList.Select(o => $"\t\t\tthis._{o.fieldName} = {o.fieldName.ToLower()};"));
            
            var fieldDefines = string.Join("\n", _configTypeInfoList.Select(o => 
            {
                var typeName = o.IsList ? $"List<{o.typeName}>" : o.typeName;
                var serializationAttr = (o.IsList || o.typeName.Contains("[]")) ? "\t\t[SerializeField, SerializeReference]\n" : "\t\t[SerializeField]\n";
                return $"{serializationAttr}\t\tprivate {typeName} _{o.fieldName};\n\t\t/// {o.comment}\n\t\tpublic {typeName} {o.fieldName} => _{o.fieldName};\n";
            }));

            var keyType = keys.Count > 1
                ? $"({string.Join(", ", keys.Select(k => $"{k.typeName} {k.fieldName.ToLower()}"))})"
                : keys[0].typeName;
            
            var keyName = keys.Count > 1
                ? $"keyTuple"
                : keys[0].fieldName.ToLower();

            var firstKeyType = keys[0].typeName;
            var firstKeyName = keys[0].fieldName;
            if (firstKeyType == "string")
            {
                // 如果需要生成枚举类型的Key，可以在此处注入枚举前缀，暂按原字符串类型处理
                 WriteConfEnumKeys(keys[0]); 
            }

            // 处理联级查找 (LookUp)
            var lookupCode = string.Empty;
            var lookupKey = string.Empty;
            var lookupParameter = string.Empty;

            if (keys.Count >= 2)
            {
                lookupCode = $"_indexByFirstKey = rawData.ToLookup(v => v.{keys[0].fieldName});";
                lookupKey = keys[0].fieldName.ToLower();
                lookupParameter = $"{keys[0].typeName} {lookupKey}";
            }

            template = template.Replace("{{ConfName}}", _fileName)
                .Replace("{{InputParameter}}", inputParameters)
                .Replace("{{InitCode}}", initCode)
                .Replace("{{FieldDefine}}", fieldDefines)
                .Replace("{{KeyType}}", keyType)
                .Replace("{{KeyName}}", keyName)
                .Replace("{{LookupCode}}", lookupCode)
                .Replace("{{LookupKey}}", lookupKey)
                .Replace("{{LookupParameter}}", lookupParameter);

            _generatedCsString = template;
        }

        public void GenerateEditorCsFile()
        {
            if (!File.Exists(EditorTemplatePath))
            {
                ConfigLogger.LogError($"找不到编辑器类模板: {EditorTemplatePath}");
                return;
            }

            var excelName = _path.Replace("\\", "/").Split('/').LastOrDefault()?.Replace(".xlsx", "");
            var template = File.ReadAllText(EditorTemplatePath);

            var initData = string.Empty;
            var miniColumn = _fieldMap.Values.Select(o => o.columns.Min()).Min();

            foreach (var configTypeInfo in _configTypeInfoList)
            {
                if (configTypeInfo.IsList)
                {
                    initData += $"\t\t\t\t\tvar {configTypeInfo.fieldName.ToLower()} = new List<{configTypeInfo.typeName}>();\n";
                    foreach (var column in configTypeInfo.columns)
                    {
                        initData += $"\t\t\t\t\tif (ws.Cells[raw, {column}].Value != null)\n";
                        initData += $"\t\t\t\t\t\t{configTypeInfo.fieldName.ToLower()}.Add({configTypeInfo.refTypeName}.Parse(ws.Cells[raw, {column}].Value?.ToString(), fileName, raw, {column}));\n";
                    }
                }
                else
                {
                    var column = configTypeInfo.columns[0];
                    initData += $"\t\t\t\t\tvar {configTypeInfo.fieldName.ToLower()} = {configTypeInfo.refTypeName}.Parse(ws.Cells[raw, {column}].Value?.ToString(), fileName, raw, {column});\n";
                }
            }

            var inputParameterValue = string.Join(", ", _configTypeInfoList.Select(o => o.fieldName.ToLower()));

            template = template.Replace("{{ConfName}}", _fileName)
                               .Replace("{{ExcelName}}", excelName)
                               .Replace("｛｛InitData｝}", initData)
                               .Replace("{{InputParameterValue}}", inputParameterValue);

            _generatedEditorCsString = template;
        }

        public string GetCSFileString()
        {
            return _generatedCsString;
        }

        public string GetEditorCSFileString() // 提供对外的Editor生成方法
        {
            return _generatedEditorCsString;
        }

        private void WriteConfEnumKeys(ConfigTypeInfo keyInfo)
        {
            // 如果需要枚举导出逻辑，可在此处把之前的拼接逻辑以追加形式加到 _generatedCsString 尾部或使用模板替换
            // 为了保持和之前一样的功能：
            var p = _path.Replace("\\", "/");
            var excelPath = EditorSaveUtils.GetEditorPref(ConfigSettingLogic.SaveKey.excelPath, "");
            string path = Path.Combine(excelPath, p.Split('/').LastOrDefault() ?? string.Empty);
            var enumValues = new HashSet<string>();
            var list = new List<string>();
            using (var reader = new ExcelReader(path))
            {
                var ws = reader.workbook.Worksheets[1];
                var rowCount = ws.Dimension.Rows;
                var keyColumn = keyInfo.columns[0];
                for (int raw = 4; raw <= rowCount; raw++)
                {
                    var keyCell = ws.Cells[raw, keyColumn].Value;
                    if (keyCell == null || string.IsNullOrEmpty(keyCell.ToString())) continue;
                    var valueString  = keyCell.ToString();
                    if (enumValues.Contains(valueString)) continue;
                    enumValues.Add(valueString);
                    list.Add(valueString);
                }
            }
            if(enumValues.Count == 0) return;
            
            var enumStr = $"\n\tpublic enum {_fileName}Key\n\t{{\n\t\t{string.Join(",\n\t\t", list)}\n\t}}\n";
            var mapStr = $"\n\tpublic class {_fileName}KeyMap\n\t{{\n\t\tpublic static Dictionary<string, {_fileName}Key> map = new Dictionary<string, {_fileName}Key>\n\t\t{{\n";
            foreach(var v in list) mapStr += $"\t\t\t{{\"{v}\", {_fileName}Key.{v}}},\n";
            mapStr += "\t\t};\n\t}\n";
            
            _generatedCsString += $"\nnamespace PowerCellStudio\n{{{enumStr}{mapStr}}}\n";
        }

        public static string GenerateManagerCSString(List<string> confCollections)
        {
            // 此处保持硬编码或同样改成读模板处理皆可。此处保持原样。
            var csFile = new CsWriter();
            csFile.WriteLine("//------------------------------------------------------------------------------")
                .WriteLine("// <auto generated>")
                .WriteLine("//\tShould not be edited manually!")
                .WriteLine("// </auto generated>")
                .WriteLine("//------------------------------------------------------------------------------")
                .Space();
            csFile.WriteLine("namespace PowerCellStudio");
            csFile.StartWriteBody();
            csFile.WriteLine("public partial class ConfigManager");
            csFile.StartWriteBody();
            for (var i = 0; i < confCollections.Count; i++)
            {
                var fieldName = confCollections[i].Replace("Collections", "");
                fieldName = fieldName[0].ToString().ToLower() + fieldName.Substring(1);
                csFile.WriteLine($"private readonly {confCollections[i]} _{fieldName} = new {confCollections[i]}();")
                    .WriteLine($"public {confCollections[i]} {fieldName} => _{fieldName};")
                    .Space();
            }

            csFile.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.None, "ConfigGroup<CommonConfigLoader>", "GetGroupOfAllConfig");
            csFile.WriteVar("configGroup",  "new ConfigGroup<CommonConfigLoader>()");
            for (var i = 0; i < confCollections.Count; i++)
            {
                var fieldName = confCollections[i].Replace("Collections", "");
                fieldName = fieldName[0].ToString().ToLower() + fieldName.Substring(1);
                csFile.WriteLine($"configGroup.Append(_{fieldName});");
            }
            csFile.WriteLine("return configGroup;");
            csFile.EndWriteMethod();

            csFile.EndWriteBody();
            csFile.EndWriteBody();
            return csFile.ToString();
        }
    }
}

#endif