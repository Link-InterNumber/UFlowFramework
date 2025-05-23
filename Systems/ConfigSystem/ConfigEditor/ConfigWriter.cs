#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;
using UnityEditor;

namespace PowerCellStudio
{
    public class ConfigWriter
    {
        private Dictionary<string, ConfigTypeInfo> _fieldMap;
        // public Dictionary<string, ConfigTypeInfo> fieldMap => _fieldMap;
        private ConfigTypeInfo[] _configTypeInfoList;
        private readonly string _fileName;
        // public string fileName => _fileName;
        private CsWriter _csFile;
        
        // private List<TypeRef> _typeResolvers = new List<TypeRef>();
        private readonly string _path;

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
            
        public ConfigWriter(string path, string fileName, Dictionary<string, ConfigTypeInfo> fieldMap)
        {
            _path = path;
            _fileName = fileName;
            _fieldMap = fieldMap;
            _configTypeInfoList = _fieldMap.Values.ToArray();
        }
        
        public void GenerateCSString()
        {
            var keys = new List<ConfigTypeInfo>();
            foreach (var (key, info) in _fieldMap)
            {
                if (info.isKey) keys.Add(info);
            }

            _csFile = new CsWriter();
            _csFile.WriteLine("//------------------------------------------------------------------------------")
                .WriteLine("// <auto generated>")
                .WriteLine("//\tShould not be edited manually!")
                .WriteLine("// </auto generated>")
                .WriteLine("//------------------------------------------------------------------------------");
            switch (ConstSetting.ConfigConfigSaveMode)
            {
                case ConstSetting.ConfigSaveMode.ScriptableObject:
                    _csFile.WriteUsing("System", 
                        "System.Collections.Generic", 
                        "System.IO", 
                        "System.Linq", 
                        "UnityEngine");
                    break;
                case ConstSetting.ConfigSaveMode.Json:
                    _csFile.WriteUsing("System", 
                        "System.Collections.Generic", 
                        "System.IO", 
                        "System.Linq", 
                        "UnityEngine",
                        "Newtonsoft.Json");
                    break;
                case ConstSetting.ConfigSaveMode.Binary:
                    _csFile.WriteUsing("System", 
                        "System.Collections.Generic", 
                        "System.IO", 
                        "System.Linq", 
                        "UnityEngine",
                        "System.Runtime.Serialization.Formatters.Binary");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _csFile.Space(1);
            _csFile.WriteLine("namespace PowerCellStudio");
            _csFile.StartWriteBody();
            // ConfBase 类
            WriteConfBase();
            // ConfBaseCollections类
            WriteConfCollections(ref keys);
            // ConfBaseData类
            WriteConfBaseData();
            _csFile.EndWriteBody();
        }

        private void WriteConfBaseData()
        {
            _csFile.WriteLine("[Serializable]");
            _csFile.WriteLine("public class " + _fileName + "Data : ConfBaseData");
            _csFile.StartWriteBody();
            _csFile.WriteField(CsWriter.FieldSign.Public, $"List<{_fileName}>", "source", $"new List<{_fileName}>()");
            _csFile.WriteLineWithoutTab("#if UNITY_EDITOR");
            _csFile.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Static, "string", "CreatAsset", "string oldMd5")
                .WriteLine("var excelPath = UnityEditor.EditorPrefs.GetString(ConfigSettingWindow.SaveKey.excelPath);");
            var p = _path.Replace("\\", "/");
            _csFile.WriteLine($"string path = excelPath + \"/{p.Split('/').LastOrDefault()}\";");
            
            _csFile.StartWriteIf("!File.Exists(path)")
                .WriteLine("ConfigLog.LogError(\"Cannot find file \" + path);")
                .WriteLine("return string.Empty;")
                .EndWriteIf();
            
            _csFile.WriteVar("md5", "ConfigMenu.CalFileMD5(path)");
            var assetPath = EditorPrefs.GetString(ConfigSettingWindow.SaveKey.assetFilePath, "Assets/ConfigAsset/");

            _csFile.StartWriteIf($"md5 == oldMd5 && File.Exists(\"{assetPath}{_fileName}Asset.{extensionName}\")")
                .WriteLine("return md5;")
                .EndWriteIf();

            if (ConstSetting.ConfigConfigSaveMode == ConstSetting.ConfigSaveMode.ScriptableObject)
            {
                _csFile.WriteVar("asset", $"ScriptableObject.CreateInstance<{_fileName}Data>()");
            }
            else
            {
                _csFile.WriteVar("asset", $"new {_fileName}Data()");
            }
            // _csFile.Append("\t\t\tvar file = new FileInfo(path);\n");
            _csFile.WriteLine("using (var reader = new ExcelReader(path))");
            _csFile.StartWriteBody();
            // _csFile.Append("\t\t\t\tvar fieldMap = reader.fieldMap;\n");
            _csFile.WriteVar("ws", "reader.workbook.Worksheets[1]");
            _csFile.WriteVar("rowCount", "ws.Dimension.Rows");
            _csFile.WriteLine("for (int raw = 4; raw <= rowCount; raw++)")
                .StartWriteBody();
            var miniColumn = _fieldMap.Values.Select(o => o.columns.Min()).Min();
            _csFile.WriteVar("firstCell", $"ws.Cells[raw, {miniColumn}].Value");
            _csFile.WriteLine("if (firstCell == null || string.IsNullOrEmpty(firstCell.ToString())) continue;");
            _csFile.WriteVar("fileName", $"\"{_fileName}\"");
            for (var i = 0; i < _configTypeInfoList.Length; i++)
            {
                var configTypeInfo = _configTypeInfoList[i];
                if (configTypeInfo.IsList)
                {
                    _csFile.WriteVar($"{configTypeInfo.fieldName.ToLower()}", $"new List<{configTypeInfo.typeName}>()");
                    foreach (var column in configTypeInfo.columns)
                    {
                        _csFile.WriteLine($"if (ws.Cells[raw, {column}].Value != null)");
                        _csFile.WriteLine(
                            $"\t{configTypeInfo.fieldName.ToLower()}.Add({configTypeInfo.refTypeName}.Parse(ws.Cells[raw, {column}].Value?.ToString(), fileName, raw, {column}));");
                    }
                }
                else
                {
                    var column = configTypeInfo.columns[0];
                    _csFile.WriteLine(
                        $"var {configTypeInfo.fieldName.ToLower()} = {configTypeInfo.refTypeName}.Parse(ws.Cells[raw, {column}].Value?.ToString(), fileName, raw, {column});");
                }
            }

            // _csFile.Append($"\t\t\t\t\tdata.SetData(");
            _csFile.WriteWithoutLine($"var data = new {_fileName}(");

            for (var i = 0; i < _configTypeInfoList.Length; i++)
            {
                _csFile.WriteAppend($"{_configTypeInfoList[i].fieldName.ToLower()}");
                if (i < _configTypeInfoList.Length - 1)
                {
                    _csFile.WriteAppend(", ");
                    if (i > 0 && i % 3 == 0) _csFile.WriteAppend("\n\t\t\t\t\t\t");
                }
                else
                {
                    _csFile.WriteAppend(");\n");
                }
            }

            _csFile.WriteLine("asset.source.Add(data);");
            _csFile.EndWriteBody();
            _csFile.EndWriteBody();
            switch (ConstSetting.ConfigConfigSaveMode)
            {
                case ConstSetting.ConfigSaveMode.ScriptableObject:
                    _csFile.WriteLine(
                        $"UnityEditor.AssetDatabase.CreateAsset(asset, \"{assetPath}{_fileName}Asset.{extensionName}\");")
                        .WriteLine("UnityEditor.EditorUtility.SetDirty(asset);")
                        .WriteLine("UnityEditor.AssetDatabase.SaveAssetIfDirty(asset);")
                        .WriteLine($"ConfigLog.Log(\"Config Asset Created => [{_fileName}]\");");
                    break;
                case ConstSetting.ConfigSaveMode.Json:
                    _csFile.WriteLine("string json = JsonConvert.SerializeObject(asset);");
                    _csFile.WriteLine("json = EncryptUtils.AESDecrypt(json, ConstSetting.FileEncryptionKey);"); // 加密配置文件
                    _csFile.WriteLine($"File.WriteAllText(\"{assetPath}{_fileName}Asset.{extensionName}\", json);");
                    _csFile.WriteLine($"ConfigLog.Log(\"Config Asset Created => [{_fileName}]\");");
                    break;
                case ConstSetting.ConfigSaveMode.Binary:
                    _csFile.WriteLine("using (MemoryStream memoryStream = new MemoryStream())");
                    _csFile.StartWriteBody();
                    _csFile.WriteVar("formatter", "new BinaryFormatter()");
                    _csFile.WriteLine("formatter.Serialize(memoryStream, asset);");
                    _csFile.WriteVar("bytes", "memoryStream.ToArray();");
                    _csFile.WriteLine("bytes = EncryptUtils.AESEncrypt(bytes, ConstSetting.FileEncryptionKey);"); // 加密配置文件
                    _csFile.WriteLine($"File.WriteAllBytes(\"{assetPath}{_fileName}Asset.{extensionName}\", bytes);");
                    _csFile.WriteLine($"ConfigLog.Log(\"Config Asset Created => [{_fileName}]\");");
                    _csFile.WriteLine("memoryStream.Close();");
                    _csFile.EndWriteBody();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            _csFile.WriteLine("return md5;");
            _csFile.EndWriteMethod();
            _csFile.WriteLineWithoutTab("#endif");
            _csFile.EndWriteBody();
        }

        private void WriteConfCollections(ref List<ConfigTypeInfo> keys)
        {
            _csFile.WriteLine("public partial class " + _fileName + "Collections : ConfBaseCollections");
            _csFile.StartWriteBody();
            var assetPath = EditorPrefs.GetString(ConfigSettingWindow.SaveKey.assetFilePath, "Assets/ConfigAsset/");
            _csFile.StartWriteMethod(CsWriter.MethodSign.Public,
                    CsWriter.MethodSign.None, 
                    "",
                    _fileName + "Collections")
                .WriteLine($"_assetPath = \"{assetPath}{_fileName}Asset.{extensionName}\";")
                .EndWriteMethod();

            _csFile.WriteField(CsWriter.FieldSign.Public, $"List<{_fileName}>", "rawData", $"new List<{_fileName}>()")
                .WriteField(CsWriter.FieldSign.Private, "ConfAsyncLoadHandle", "_loadHandle")
                .Space();

            _csFile.StartWriteMethod(CsWriter.MethodSign.Partial, CsWriter.MethodSign.None, "void", "OnLoaded");

            _csFile.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Override, "void", "LoadConfAsync<T>", "T handle")
                .StartWriteIf("_loadStatus == AssetLoadStatus.Loaded")
                .WriteLine("_refCount++;")
                .WriteLine("handle.Release();")
                .WriteLine("return;")
                .EndWriteIf()
                .WriteLine($"_loadHandle = handle;")
                .WriteLine($"_loadHandle.Completed += LoadHandler;")
                .WriteLine($"_loadStatus = AssetLoadStatus.Loading;")
                .WriteLine($"_loadHandle.LoadAsync<{_fileName}Data>(_assetPath);")
                .EndWriteMethod();

            _csFile.StartWriteMethod(CsWriter.MethodSign.Private, CsWriter.MethodSign.None, "void", "LoadHandler", "ConfBaseData handle")
                .StartWriteIf("handle == null")
                .WriteLine("_loadStatus = AssetLoadStatus.Unload;")
                .WriteLine("return;")
                .EndWriteIf()
                .WriteLine("_refCount = 1;")
                .WriteLine($"rawData = (handle as {_fileName}Data)?.source;")
                .WriteLine("MapData();")
                .WriteLine("OnLoaded();")
                .WriteLine("_loadStatus = AssetLoadStatus.Loaded;")
                .EndWriteMethod();
            
            _csFile.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Override, "void", "Release")
                .WriteLine("if(_loadStatus != AssetLoadStatus.Loaded) return;")
                .WriteLine("_refCount--;")
                .StartWriteIf("_refCount > 0")
                .WriteLine("return;")
                .EndWriteIf()
                .WriteLine("rawData.Clear();")
                .WriteLine("_dictionary.Clear();")
                .WriteLine("_loadHandle.Release();")
                .WriteLine("_loadHandle = null;")
                .WriteLine("_loadStatus = AssetLoadStatus.Unload;")
                .EndWriteMethod();

            if (keys.Count == 0) keys.Add(_fieldMap.Values.FirstOrDefault());
            if (keys.Count == 1)
            {
                var isEnumKey = keys[0].typeName.Equals("string");
                _csFile.WriteField(CsWriter.FieldSign.Private,
                        $"Dictionary<{(isEnumKey ? (_fileName + "Key") : keys[0].typeName)}, {_fileName}>",
                        "_dictionary",
                        $"new Dictionary<{(isEnumKey ? (_fileName + "Key") : keys[0].typeName)}, {_fileName}>()")
                    .Space();

                _csFile.StartWriteMethod(CsWriter.MethodSign.Private, CsWriter.MethodSign.None, "void", "MapData")
                    .WriteLine("_dictionary.Clear();")
                    .WriteLine("if (rawData == null) return;")
                    .WriteLine("for (var i = 0; i < rawData.Count; i++)")
                    .StartWriteBody();
                if (isEnumKey)
                {
                    _csFile.WriteVar("keyValue", $"{_fileName}KeyMap.map[rawData[i].{keys[0].fieldName}]");
                }
                else
                {
                    _csFile.WriteVar("keyValue", $"rawData[i].{keys[0].fieldName}");
                }
                _csFile.StartWriteIf($"_dictionary.ContainsKey(keyValue)")
                    .WriteLine("ConfigLog.LogError($\"" + _fileName + " Id 重复，重复值=[{keyValue}]\");")
                    .WriteLine("continue;")
                    .EndWriteIf()
                    .WriteLine($"_dictionary[keyValue] = rawData[i];")
                    .EndWriteBody()
                    .EndWriteMethod();

                if (keys.Count == 1 && keys[0].typeName.Equals("string"))
                {
                    _csFile.StartWriteMethod(CsWriter.MethodSign.Public,
                            CsWriter.MethodSign.None,
                            $"{_fileName}",
                            "Get",
                            $"{_fileName}Key enumKey")
                        .StartWriteIf("_loadStatus != AssetLoadStatus.Loaded")
                        .WriteLine($"ConfigLog.LogError(\"{_fileName} is not loaded yet\");")
                        .WriteLine("return null;")
                        .EndWriteIf()
                        .WriteLine($"return _dictionary.TryGetValue(enumKey, out var conf) ? conf : null;")
                        .EndWriteMethod();
                }
                else
                {
                    _csFile.StartWriteMethod(CsWriter.MethodSign.Public,
                            CsWriter.MethodSign.None,
                            $"{_fileName}",
                            "Get",
                            $"{keys[0].typeName} {keys[0].fieldName}")
                        .StartWriteIf("_loadStatus != AssetLoadStatus.Loaded")
                        .WriteLine($"ConfigLog.LogError(\"{_fileName} is not loaded yet\");")
                        .WriteLine("return null;")
                        .EndWriteIf()
                        .WriteLine($"return _dictionary.TryGetValue({keys[0].fieldName}, out var conf) ? conf : null;")
                        .EndWriteMethod();
                }
            }
            else
            {
                _csFile.WriteField(CsWriter.FieldSign.Private,
                        $"Dictionary<{keys[0].typeName},  Dictionary<string, {_fileName}>>",
                        "_dictionary",
                        $"new Dictionary<{keys[0].typeName},  Dictionary<string, {_fileName}>>()")
                    .Space();

                _csFile.StartWriteMethod(CsWriter.MethodSign.Private,
                        CsWriter.MethodSign.None,
                        "void",
                        "MapData")
                    .WriteLine("_dictionary.Clear();")
                    .WriteLine("for (var i = 0; i < rawData.Count; i++)")
                    .StartWriteBody()
                    .StartWriteIf($"!_dictionary.TryGetValue(rawData[i].{keys[0].fieldName}, out var dic)")
                    .WriteLine($"dic = new Dictionary<string, {_fileName}>();")
                    .WriteLine($"_dictionary[rawData[i].{keys[0].fieldName}] = dic;")
                    .EndWriteIf();
                _csFile.WriteWithoutLine($"string key = $\"");
                for (int i = 1; i < keys.Count; i++)
                {
                    _csFile.WriteAppend("{")
                        .WriteAppend($"rawData[i].{keys[i].fieldName}")
                        .WriteAppend("}");
                    if (i < keys.Count - 1) _csFile.WriteAppend("&");
                    else _csFile.WriteAppend("\";\n");
                }

                _csFile.StartWriteIf($"dic.ContainsKey(key)")
                    .WriteLine($"ConfigLog.LogError($\"{_fileName} Id 重复，重复值= [{{rawData[i].{keys[0].fieldName}}}]-[{{key}}]\");")
                    .WriteLine("continue;")
                    .EndWriteIf();
                
                _csFile.WriteLine($"dic[key] = rawData[i];")
                    .EndWriteBody()
                    .EndWriteMethod();

                var paras = keys.Select(o => $"{o.typeName} {o.fieldName}").ToArray();
                _csFile.StartWriteMethod(CsWriter.MethodSign.Public,
                        CsWriter.MethodSign.None,
                        $"{_fileName}",
                        "Get",
                        paras)
                    .StartWriteIf("_loadStatus != AssetLoadStatus.Loaded")
                    .WriteLine($"ConfigLog.LogError(\"{_fileName} is not loaded yet\");")
                    .WriteLine("return null;")
                    .EndWriteIf()
                    .StartWriteIf($"!_dictionary.TryGetValue({keys[0].fieldName}, out var dic)")
                    .WriteLine("return null;")
                    .EndWriteIf();
                _csFile.WriteWithoutLine($"string key = $\"");
                for (int i = 1; i < keys.Count; i++)
                {
                    _csFile.WriteAppend("{")
                        .WriteAppend($"{keys[i].fieldName}")
                        .WriteAppend("}");
                    if (i < keys.Count - 1) _csFile.WriteAppend("&");
                    else _csFile.WriteAppend("\";\n");
                }
                _csFile.WriteLine("return dic.TryGetValue(key, out var conf) ? conf : null;");
                _csFile.EndWriteMethod();

                _csFile.StartWriteMethod(CsWriter.MethodSign.Public,
                        CsWriter.MethodSign.None,
                        $"{_fileName}[]",
                        "Get",
                        $"{keys[0].typeName} {keys[0].fieldName}")
                    .StartWriteIf("_loadStatus != AssetLoadStatus.Loaded")
                    .WriteLine($"ConfigLog.LogError(\"{_fileName} is not loaded yet\");")
                    .WriteLine("return null;")
                    .EndWriteIf()
                    .StartWriteIf($"!_dictionary.TryGetValue({keys[0].fieldName}, out var dic)")
                    .WriteLine("return null;")
                    .EndWriteIf()
                    .WriteLine("return dic.Values.ToArray();")
                    .EndWriteMethod();
            }
            _csFile.EndWriteBody();
            _csFile.Space();

            if (keys.Count == 1 && keys[0].typeName.Equals("string"))
            {
                WriteConfEnumKeys(keys[0]);
            }
        }

        private void WriteConfEnumKeys(ConfigTypeInfo keyInfo)
        {
            var p = _path.Replace("\\", "/");
            var excelPath = EditorPrefs.GetString(ConfigSettingWindow.SaveKey.excelPath);
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
                    if (enumValues.Contains(valueString))
                    {
                        ConfigLog.LogError($"配置的Key {valueString} 字段不能重复");
                        continue;
                    }
                    enumValues.Add(valueString);
                    list.Add(valueString);
                }
            }
            if(enumValues.Count == 0) return;
            _csFile.WriteLine("public enum " + _fileName + "Key");
            _csFile.StartWriteBody();
            foreach (var enumValue in list)
            {
                _csFile.WriteLine(enumValue + ",");
            }
            _csFile.EndWriteBody();
            _csFile.Space();
            
            _csFile.WriteLine("public class " + _fileName + "KeyMap");
            _csFile.StartWriteBody();
            _csFile.WriteLine("public static Dictionary<string, " + _fileName + "Key> map = new Dictionary<string, " + _fileName + "Key>");
            _csFile.StartWriteBody();
            foreach (var enumValue in list)
            {
                _csFile.WriteLine($"{{\"{enumValue}\", {_fileName}Key.{enumValue}}},");
            }
            _csFile.EndWriteBody();
            _csFile.WriteLine(";\n");
            _csFile.EndWriteBody();
            _csFile.Space();
        }

        private void WriteConfBase()
        {
            _csFile.WriteLine("[Serializable]")
                .WriteLine("public partial class " + _fileName + " : ConfBase")
                .StartWriteBody();

            var paras = _configTypeInfoList.Select(o =>
            {
                var type = o.typeName;
                if (o.IsList)
                {
                    type = $"List<{type}>";
                }
                return $"{type} {o.fieldName.ToLower()}";
            }).ToArray();
            _csFile.StartWriteMethod(CsWriter.MethodSign.Public,
                CsWriter.MethodSign.None,
                "",
                _fileName,
                paras);
            for (var i = 0; i < _configTypeInfoList.Length; i++)
            {
                _csFile.WriteLine($"this._{_configTypeInfoList[i].fieldName} = {_configTypeInfoList[i].fieldName.ToLower()};");
            }
            _csFile.EndWriteMethod();

            foreach (var (key, info) in _fieldMap)
            {
                var typeName = info.IsList ? $"List<{info.typeName}>" : info.typeName;
                _csFile.WriteLine($"[SerializeField]")
                    .WriteField(CsWriter.FieldSign.Private,
                        typeName,
                        $"_{info.fieldName}")
                    .WriteLine($"///{info.comment}")
                    .WriteLine($"public {typeName} {info.fieldName} => _{info.fieldName};")
                    .Space();
            }
            _csFile.EndWriteBody().Space();
        }

        public string GetCSFileString()
        {
            return _csFile.ToString();
        }

        public static string GenerateManagerCSString(List<string> confCollections)
        {
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