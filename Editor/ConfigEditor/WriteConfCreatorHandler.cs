/****************************************************
    public class {{ConfName}}Creator : ConfCreator
    {
        #if UNITY_EDITOR
        public static string CreatAsset(string oldMd5)
        {
            var excelPath = EditorSaveUtils.GetEditorPref(ConfigSettingWindow.SaveKey.excelPath, "");
            string path = excelPath + "/{{ExcelFileName}}";

            if (!File.Exists(path))
            {
                ConfigLog.LogError("Cannot find file " + path);
                return string.Empty;
            }

            var md5 = ConfigMenu.CalFileMD5(path);
            if (md5 == oldMd5 && File.Exists("Assets/ConfigAsset/{{ConfName}}Data.bytes"))
            {
                return md5;
            }

            using (var reader = new ExcelReader(path))
            {
                var ws = reader.workbook.Worksheets[1];
                var rowCount = ws.Dimension.Rows;
                IEnumerable<{{ConfName}}> readYieldInstruction = (Worksheet ws, int rowCount) => 
                {
                    var fileName = "{{ConfName}}";
                    for (int raw = 4; raw <= rowCount; raw++)
                    {
                        var firstCell = ws.Cells[raw, {{MiniColumn}}].Value;
                        if (firstCell == null || string.IsNullOrEmpty(firstCell.ToString())) continue;

#for {{ParseStatements}}
                        {{ParseStatement}}
#forend

                        var data = new {{ConfName}}(
#for {{CtorArguments}}
                            {{ArgumentValue}}
#forend
                        );
                        yield return data;
                    }
                }
        
#if isEnumKey
                var keySelector = new Func<{{ConfName}}, {{KeyType}}>(conf => Enum.TryParse<{{ConfName}}Key>(conf.{{keyName}}, out var enumKey) ? enumKey : 0;
#else if MultiKey
                var keySelector = new Func<{{ConfName}}, {{KeyType}}>(conf =>(
#for {{KeyList}}
                    {{keyName}}: conf.{{keyName}},
#forend
                        ));
#else
                var keySelector = new Func<{{ConfName}}, {{KeyType}}>(conf => conf.{{keyName}});
#endif
				ChunkMaker.StreamWriteSync("Assets/ConfigAsset/", {{ConfName}}, ReadYieldInstruction(), keySelector, 256);
            }
            ConfigLog.Log("Config Asset Created => [{{ConfName}}]");
            return md5;
        }
        #endif
    }
****************************************************/

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PowerCellStudio
{
    public class WriteConfCreatorHandler
    {
        public static void Write(in CsWriter csWriter, in ExcelReader reader, in ConfigTypeInfo[] configTypeInfoList,
            in string confName)
        {
            var keys = new List<ConfigTypeInfo>();
            foreach (var info in configTypeInfoList)
            {
                if (info.isKey) keys.Add(info);
            }
            if (keys.Count == 0) keys.Add(configTypeInfoList.FirstOrDefault());
            var isEnumKey = keys.Count == 1 && keys[0].typeName.Equals("string");
            var isMultiKey = keys.Count > 1;

            var keyType = keys[0].typeName;
            if (isEnumKey)
            {
                keyType = confName + "Key";
            }
            else if (isMultiKey)
            {
                var tempSb = new StringBuilder();
                tempSb.Append("(");
                for (var i = 0; i < keys.Count; i++)
                {
                    var info = keys[i];
                    tempSb.Append($"{info.typeName} {info.fieldName}");
                    if (i < keys.Count - 1)
                        tempSb.Append(", ");
                }
                tempSb.Append(")");
                keyType = tempSb.ToString();
            }

            csWriter.WriteLine($"public class {confName}Creator : ConfCreator");
            csWriter.StartWriteBody();

            csWriter.WriteLineWithoutTab("#if UNITY_EDITOR");
            WriteCreateAssetMethod(csWriter, reader, configTypeInfoList, confName, keys, keyType, isEnumKey, isMultiKey);
            csWriter.WriteLineWithoutTab("#endif");

            csWriter.EndWriteBody();
        }

        private static void WriteCreateAssetMethod(CsWriter csWriter, ExcelReader reader,
            ConfigTypeInfo[] configTypeInfoList, string confName, List<ConfigTypeInfo> keys, string keyType,
            bool isEnumKey, bool isMultiKey)
        {
            var excelFileName = reader.path.Replace("\\", "/").Split('/').LastOrDefault();
            var assetPath = ConfigManager.assetFolderPath;
            var miniColumn = configTypeInfoList.Min(info => info.columns.Min());
            var keySelector = BuildChunkKeySelector(confName, keys, isEnumKey, isMultiKey);

            csWriter.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Static, "string", "CreatAsset", "string oldMd5")
                .WriteLine("var excelPath = EditorSaveUtils.GetEditorPref(ConfigSettingWindow.SaveKey.excelPath, \"\");")
                .WriteLine($"string path = excelPath + \"/{excelFileName}\";")
                .WriteLine($"var assetPath = \"{assetPath}\";")
                .WriteLine($"var dataAssetPath = $\"{{assetPath}}{confName}Data.bytes\";")
                .WriteLine($"var indexAssetPath = $\"{{assetPath}}{confName}Index.bytes\";");

            csWriter.StartWriteIf("!File.Exists(path)")
                .WriteLine("ConfigLog.LogError(\"Cannot find file \" + path);")
                .WriteLine("return string.Empty;")
                .EndWriteIf();

            csWriter.WriteVar("md5", "ConfigMenu.CalFileMD5(path)");

            csWriter.StartWriteIf("md5 == oldMd5 && File.Exists(indexAssetPath) && File.Exists(dataAssetPath)")
                .WriteLine("return md5;")
                .EndWriteIf();

            csWriter.WriteLine("using (var reader = new ExcelReader(path))");
                
            csWriter.StartWriteBody();

            csWriter.WriteVar("ws", "reader.workbook.Worksheets[1]")
                .WriteVar("rowCount", "ws.Dimension.Rows")
                .WriteLine($"IEnumerable<{confName}> ReadYieldInstruction()");
            
            csWriter.StartWriteBody();
            csWriter.WriteVar("fileName", $"\"{confName}\"");
            csWriter.WriteLine("for (int raw = 4; raw <= rowCount; raw++)");
            csWriter.StartWriteBody();

            csWriter.WriteVar("firstCell", $"ws.Cells[raw, {miniColumn}].Value")
                .WriteLine("if (firstCell == null || string.IsNullOrEmpty(firstCell.ToString())) continue;");

            WriteParseStatements(csWriter, configTypeInfoList);
            WriteCreateDataStatement(csWriter, confName, configTypeInfoList);

            csWriter.WriteLine("yield return data;");
            csWriter.EndWriteBody();
            csWriter.EndWriteBody();

            csWriter.WriteLine($"var keySelector = new Func<{confName}, {keyType}>(conf => {keySelector});")
                .WriteLine($"ChunkMaker.StreamWriteSync(assetPath, \"{confName}\", ReadYieldInstruction(), keySelector, 256);");

            csWriter.WriteLine($"ConfigLog.Log(\"Config Asset Created => [{confName}]\");")
                .WriteLine("return md5;")
                .EndWriteBody()
                .EndWriteMethod();
        }

        private static string BuildChunkKeySelector(string confName, List<ConfigTypeInfo> keys, bool isEnumKey, bool isMultiKey)
        {
            if (isMultiKey)
            {
                var tupleBuilder = new StringBuilder();
                tupleBuilder.Append("(");
                for (var i = 0; i < keys.Count; i++)
                {
                    var key = keys[i];
                    tupleBuilder.Append($"{key.fieldName}: conf.{key.fieldName}");
                    if (i < keys.Count - 1)
                    {
                        tupleBuilder.Append(", ");
                    }
                }
                tupleBuilder.Append(")");
                return tupleBuilder.ToString();
            }

            if (isEnumKey)
            {
                return $"Enum.TryParse<{confName}Key>(conf.{keys[0].fieldName}, out var enumKey) ? enumKey : 0";
            }

            return $"conf.{keys[0].fieldName}";
        }

        private static void WriteParseStatements(CsWriter csWriter, ConfigTypeInfo[] configTypeInfoList)
        {
            for (var i = 0; i < configTypeInfoList.Length; i++)
            {
                var configTypeInfo = configTypeInfoList[i];
                var fieldName = configTypeInfo.fieldName.ToLower();
                if (configTypeInfo.IsList)
                {
                    csWriter.WriteVar(fieldName, $"new List<{configTypeInfo.typeName}>()");
                    foreach (var column in configTypeInfo.columns)
                    {
                        csWriter.StartWriteIf($"ws.Cells[raw, {column}].Value != null")
                            .WriteLine($"{fieldName}.Add({configTypeInfo.refTypeName}.Parse(ws.Cells[raw, {column}].Value?.ToString(), fileName, raw, {column}));")
                            .EndWriteIf();
                    }
                }
                else
                {
                    var column = configTypeInfo.columns[0];
                    csWriter.WriteLine(
                        $"var {fieldName} = {configTypeInfo.refTypeName}.Parse(ws.Cells[raw, {column}].Value?.ToString(), fileName, raw, {column});");
                }
            }
        }

        private static void WriteCreateDataStatement(CsWriter csWriter, string confName, ConfigTypeInfo[] configTypeInfoList)
        {
            csWriter.WriteWithoutLine($"var data = new {confName}(");

            for (var i = 0; i < configTypeInfoList.Length; i++)
            {
                csWriter.WriteAppend(configTypeInfoList[i].fieldName.ToLower());
                if (i < configTypeInfoList.Length - 1)
                {
                    csWriter.WriteAppend(", ");
                    if (i > 0 && i % 3 == 0)
                    {
                        csWriter.WriteAppend("\n\t\t\t\t\t\t");
                    }
                }
                else
                {
                    csWriter.WriteAppend(");\n");
                }
            }
        }
    }
}