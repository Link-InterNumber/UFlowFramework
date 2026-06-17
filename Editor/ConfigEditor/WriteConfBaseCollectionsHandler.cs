/****************************************************
    public partial class {{ConfName}}Collections : ConfBaseCollections<{{KeyType}}, {{ConfName}}>
    {
    
    	public {{ConfName}}Collections()
		{
#if UNITY_EDITOR
			_assetPath = $"{ConfigManager.assetFolderPath}/{{ConfName}}Data.bytes";
			_idxFilePath = $"{ConfigManager.assetFolderPath}/{{ConfName}}Index.bytes";
#else
			_assetPath = Path.Combine(Application.persistentDataPath, "ConfigAsset", "{{ConfName}}Data.bytes");
			_idxFilePath = Path.Combine(Application.persistentDataPath, "ConfigAsset", "{{ConfName}}Index.bytes");
#endif
		}

		protected override {{KeyType}} GetKey({{ConfName}} data)
		{
#if isEnumKey
            return Enum.TryParse<RolePropConfKey>(data.{{keyName}}, out var enumKey) ? enumKey : 0;
#else if MultiKey
            return (
#for {{KeyList}}
                    {{keyName}}: data.{{keyName}}
#forend
                    );
#else
            return data.{{KeyName}};
#endif
		}
		
		partial void OnLoaded({{ConfName}} data);

		protected override void OnAddData({{ConfName}} data)
		{
			OnLoaded(data);
		}
		
		partial void OnUnloaded(IEnumerable<{{ConfName}}> data);
		
		protected override void OnRemoveData(IEnumerable<{{ConfName}}> data)
		{
		    OnUnloaded(data);
		}
		
    }
****************************************************/


using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PowerCellStudio
{
    public class WriteConfBaseCollectionsHandler
    {
        public static void Write(in CsWriter csWriter, in ExcelReader reader, in ConfigTypeInfo[] configTypeInfoList, in string confName)
        {
            var keys = new List<ConfigTypeInfo>();
            foreach (var info in configTypeInfoList
            )
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
            
            csWriter.WriteLine("public partial class " + confName + $"Collections : ConfBaseCollections<{keyType}, {confName}>");
            csWriter.StartWriteBody();
            
            WriteConstructor(csWriter, confName);

            WriteGetKeyMethod(csWriter, confName, isEnumKey, isMultiKey, keys, keyType);
            
            csWriter.WriteLine($"partial void OnLoaded({confName} data);");
            csWriter.Space();
            
            csWriter.StartWriteMethod(CsWriter.MethodSign.Protected, CsWriter.MethodSign.Override, "void", "OnAddData", $"{confName} data");
            csWriter.WriteLine("OnLoaded(data);");
            csWriter.EndWriteMethod();
            
            csWriter.WriteLine($"partial void OnUnloaded(IEnumerable<{confName}> data);");
            csWriter.Space();
            
            csWriter.StartWriteMethod(CsWriter.MethodSign.Protected, CsWriter.MethodSign.Override, "void", "OnRemoveData", $"IEnumerable<{confName}> data");
            csWriter.WriteLine("OnUnloaded(data);");
            csWriter.EndWriteMethod();
            
            csWriter.EndWriteBody();
            csWriter.Space();

            if (keys.Count == 1 && keys[0].typeName.Equals("string"))
            {
                WriteConfEnumKeys(csWriter, reader, keys[0], confName);
            }
        }

        private static void WriteConstructor(CsWriter csWriter, string confName)
        {
            var assetPath = ConfigManager.assetFolderPath;
            csWriter.StartWriteMethod(CsWriter.MethodSign.Public,
                    CsWriter.MethodSign.None, 
                    "",
                    confName + "Collections")
                .WriteLineWithoutTab("#if UNITY_EDITOR")
                .WriteLine($"_assetPath = \"{assetPath}{confName}Data.bytes\";")
                .WriteLine($"_idxFilePath = \"{assetPath}{confName}Index.bytes\";")
                .WriteLineWithoutTab("#else")
                .WriteLine($"_assetPath = Path.Combine(Application.persistentDataPath, \"ConfigAsset\", \"{confName}Data.bytes\");")
                .WriteLine($"_idxFilePath = Path.Combine(Application.persistentDataPath, \"ConfigAsset\", \"{confName}Index.bytes\");")
                .WriteLineWithoutTab("#endif")
                .EndWriteMethod();
        }

        private static void WriteGetKeyMethod(CsWriter csWriter, string confName, bool isEnumKey, bool isMultiKey, List<ConfigTypeInfo> keys, string keyType)
        {
            csWriter.StartWriteMethod(CsWriter.MethodSign.Protected, CsWriter.MethodSign.Override, keyType, "GetKey", $"{confName} data");
            if (isEnumKey)
            {
                csWriter.WriteLine($"return Enum.TryParse<{confName}Key>(data.{keys[0].fieldName}, out var enumKey) ? enumKey : 0;");
            }
            else if (isMultiKey)
            {
                var tempSb = new StringBuilder();
                tempSb.Append("return (");
                for (var i = 0; i < keys.Count; i++)
                {
                    var info = keys[i];
                    tempSb.Append($"{info.fieldName}: data.{info.fieldName}");
                    if (i < keys.Count - 1)
                        tempSb.Append(",");
                }
                tempSb.Append(");");
                csWriter.WriteLine(tempSb.ToString());
            }
            else
            {
                csWriter.WriteLine($"return data.{keys[0].fieldName};");
            }
            csWriter.EndWriteMethod();
        }


        private static void WriteConfEnumKeys(CsWriter csWriter, ExcelReader excelReader, ConfigTypeInfo keyInfo, string confName)
        {
            var p = excelReader.path.Replace("\\", "/");
            var excelPath = EditorSaveUtils.GetEditorPref(ConfigSettingLogic.SaveKey.excelPath, "");
            string path = Path.Combine(excelPath, p.Split('/').LastOrDefault() ?? string.Empty);
            var enumValues = new HashSet<string>();
            var list = new List<string>();

            var ws = excelReader.workbook.Worksheets[1];
            var rowCount = ws.Dimension.Rows;
            var keyColumn = keyInfo.columns[0];
            for (int raw = 4; raw <= rowCount; raw++)
            {
                var keyCell = ws.Cells[raw, keyColumn].Value;
                if (keyCell == null || string.IsNullOrEmpty(keyCell.ToString())) continue;
                var valueString  = keyCell.ToString();
                if (enumValues.Contains(valueString))
                {
                    ConfigLogger.LogError($"配置的Key {valueString} 字段不能重复");
                    continue;
                }
                enumValues.Add(valueString);
                list.Add(valueString);
            }
            
            if(enumValues.Count == 0) return;
            csWriter.WriteLine("public enum " + confName + "Key");
            csWriter.StartWriteBody();
            foreach (var enumValue in list)
            {
                csWriter.WriteLine(enumValue + ",");
            }
            csWriter.EndWriteBody();
            csWriter.Space();
            
            csWriter.WriteLine("public class " + confName + "KeyMap");
            csWriter.StartWriteBody();
            csWriter.WriteLine("public static Dictionary<string, " + confName + "Key> map = new Dictionary<string, " + confName + "Key>");
            csWriter.StartWriteBody();
            foreach (var enumValue in list)
            {
                csWriter.WriteLine($"{{\"{enumValue}\", {confName}Key.{enumValue}}},");
            }
            csWriter.EndWriteBody();
            csWriter.WriteLine(";\n");
            csWriter.EndWriteBody();
            csWriter.Space();
        }

    }
}