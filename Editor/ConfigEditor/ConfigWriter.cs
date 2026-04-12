#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace PowerCellStudio
{
    public class ConfigWriter : IDisposable
    {
        private CsWriter _csFile;

        private string extensionName => "bytes";
        // {
        //     get
        //     {
        //         switch (ConstSetting.ConfigConfigSaveMode)
        //         {
        //             case ConstSetting.ConfigSaveMode.Json:
        //                 return "json";
        //             case ConstSetting.ConfigSaveMode.Binary:
        //                 return "bytes";
        //             case ConstSetting.ConfigSaveMode.ScriptableObject:
        //             default:
        //                 return "asset";
        //         }
        //     }
        // }
            
        public ConfigWriter()
        {
            _csFile = new CsWriter();
        }

        public void Clear()
        {
            _csFile.Clear();
        }
        
        public void GenerateRuntimeCsString(ExcelReader reader)
        {
            var confName = reader.fileName;
            var configTypeInfoList = reader.fieldMap.Values.ToArray();
            var keys = new List<ConfigTypeInfo>();
            foreach (var info in configTypeInfoList)
            {
                if (info.isKey) keys.Add(info);
            }

            _csFile.WriteLine("//------------------------------------------------------------------------------")
                .WriteLine("// <auto generated>")
                .WriteLine("//\tShould not be edited manually!")
                .WriteLine("// </auto generated>")
                .WriteLine("//------------------------------------------------------------------------------");
            
            _csFile.WriteUsing(
                "UnityEngine",
                "System.Linq",
                "System.Collections.Generic",
                "System");

            _csFile.Space(1);
            _csFile.WriteLine("namespace PowerCellStudio");
            _csFile.StartWriteBody();
            // ConfBase 类
            WriteConfBaseHandler.Write(_csFile, reader, configTypeInfoList, confName);
            // ConfBaseCollections类
            WriteConfBaseCollectionsHandler.Write(_csFile, reader, configTypeInfoList, confName);
            // ConfBaseData类
            WriteConfBaseDataHandler.Write(_csFile, reader, configTypeInfoList, confName);
            _csFile.EndWriteBody();
        }
        
        public void GenerateEditorCsString(ExcelReader reader)
        {
            var path = reader.path;
            var confName = reader.fileName;
            var configTypeInfoList = reader.fieldMap.Values.ToArray();
            
            _csFile.WriteUsing("System", "System.IO", "System.Collections.Generic");
            _csFile.Space(1);
            _csFile.WriteLine("namespace PowerCellStudio");
            _csFile.StartWriteBody();

            _csFile.WriteLine($"public class {confName}Creator : ConfCreator");
            _csFile.StartWriteBody();
            
            _csFile.WriteLineWithoutTab("#if UNITY_EDITOR");
            _csFile.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Static, "string", "CreatAsset", "string oldMd5")
                .WriteLine("var excelPath = EditorSaveUtils.GetEditorPref(ConfigSettingWindow.SaveKey.excelPath, \"\");");
            var p = path.Replace("\\", "/");
            _csFile.WriteLine($"string path = excelPath + \"/{p.Split('/').LastOrDefault()}\";");
            
            _csFile.StartWriteIf("!File.Exists(path)")
                .WriteLine("ConfigLog.LogError(\"Cannot find file \" + path);")
                .WriteLine("return string.Empty;")
                .EndWriteIf();
            
            _csFile.WriteVar("md5", "ConfigMenu.CalFileMD5(path)");
            var assetPath = EditorSaveUtils.GetEditorPref(ConfigSettingWindow.SaveKey.assetFilePath, "Assets/ConfigAsset/");

            _csFile.StartWriteIf($"md5 == oldMd5 && File.Exists(\"{assetPath}{confName}Asset.{extensionName}\")")
                .WriteLine("return md5;")
                .EndWriteIf();

            // if (ConstSetting.ConfigConfigSaveMode == ConstSetting.ConfigSaveMode.ScriptableObject)
            // {
            //     _csFile.WriteVar("asset", $"ScriptableObject.CreateInstance<{_confName}Data>()");
            // }
            // else
            // {
            //     _csFile.WriteVar("asset", $"new {_confName}Data()");
            // }
            
            _csFile.WriteVar("asset", $"new {confName}Data()");
            // _csFile.Append("\t\t\tvar file = new FileInfo(path);\n");
            _csFile.WriteLine("using (var reader = new ExcelReader(path))");
            _csFile.StartWriteBody();
            // _csFile.Append("\t\t\t\tvar fieldMap = reader.fieldMap;\n");
            _csFile.WriteVar("ws", "reader.workbook.Worksheets[1]");
            _csFile.WriteVar("rowCount", "ws.Dimension.Rows");
            _csFile.WriteLine("for (int raw = 4; raw <= rowCount; raw++)")
                .StartWriteBody();
            var miniColumn = configTypeInfoList.Select(o => o.columns.Min()).Min();
            _csFile.WriteVar("firstCell", $"ws.Cells[raw, {miniColumn}].Value");
            _csFile.WriteLine("if (firstCell == null || string.IsNullOrEmpty(firstCell.ToString())) continue;");
            _csFile.WriteVar("fileName", $"\"{confName}\"");
            for (var i = 0; i < configTypeInfoList.Length; i++)
            {
                var configTypeInfo = configTypeInfoList[i];
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
            _csFile.WriteWithoutLine($"var data = new {confName}(");

            for (var i = 0; i < configTypeInfoList.Length; i++)
            {
                _csFile.WriteAppend($"{configTypeInfoList[i].fieldName.ToLower()}");
                if (i < configTypeInfoList.Length - 1)
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
            // switch (ConstSetting.ConfigConfigSaveMode)
            // {
            //     case ConstSetting.ConfigSaveMode.ScriptableObject:
            //         _csFile.WriteLine(
            //             $"UnityEditor.AssetDatabase.CreateAsset(asset, \"{assetPath}{_confName}Asset.{extensionName}\");")
            //             .WriteLine("UnityEditor.EditorUtility.SetDirty(asset);")
            //             .WriteLine("UnityEditor.AssetDatabase.SaveAssetIfDirty(asset);")
            //             .WriteLine($"ConfigLog.Log(\"Config Asset Created => [{_confName}]\");");
            //         break;
            //     case ConstSetting.ConfigSaveMode.Json:
            //         _csFile.WriteLine("string json = SerializeUtils.SerializeToJson(asset);");
            //         _csFile.WriteLine("json = EncryptUtils.AESEncrypt(json, ConstSetting.FileEncryptionKey);"); // 加密配置文件
            //         _csFile.WriteLine($"File.WriteAllText(\"{assetPath}{_confName}Asset.{extensionName}\", json);");
            //         _csFile.WriteLine($"ConfigLog.Log(\"Config Asset Created => [{_confName}]\");");
            //         break;
            //     case ConstSetting.ConfigSaveMode.Binary:
            //         _csFile.WriteLine("var bytes = SerializeUtils.SerializeToBinary(asset);");
            //         _csFile.WriteLine("bytes = EncryptUtils.AESEncrypt(bytes, ConstSetting.FileEncryptionKey);");
            //         _csFile.WriteLine($"File.WriteAllBytes(\"{assetPath}{_confName}Asset.{extensionName}\", bytes);");
            //         _csFile.WriteLine($"ConfigLog.Log(\"Config Asset Created => [{_confName}]\");");
            //         break;
            //     default:
            //         throw new ArgumentOutOfRangeException();
            // }
            
            _csFile.WriteLine("var bytes = SerializeUtils.SerializeToBinary(asset);");
            _csFile.WriteLine("bytes = EncryptUtils.AESEncrypt(bytes, ConstSetting.FileEncryptionKey);");
            _csFile.WriteLine($"File.WriteAllBytes(\"{assetPath}{confName}Asset.{extensionName}\", bytes);");
            _csFile.WriteLine($"ConfigLog.Log(\"Config Asset Created => [{confName}]\");");
            
            _csFile.WriteLine("return md5;");
            _csFile.EndWriteMethod();
            _csFile.WriteLineWithoutTab("#endif");
            
            _csFile.EndWriteMethod();
            _csFile.EndWriteMethod();
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

        public void Dispose()
        {
            _csFile?.Dispose();
        }
    }
}

#endif