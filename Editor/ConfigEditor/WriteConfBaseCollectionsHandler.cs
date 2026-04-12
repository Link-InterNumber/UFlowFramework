/****************************************************
	public partial class {{ConfName}}Collections : ConfBaseCollections
	{
		public  {{ConfName}}Collections()
		{
			_assetPath = "Assets/ConfigAsset/{{ConfName}}Asset.bytes";
		}

		public List<{{ConfName}}> rawData = new List<{{ConfName}}>();
		private ConfAsyncLoadHandle _loadHandle;

		partial void OnLoaded();

		public override void LoadConfAsync<T>(T handle)
		{
			if (_loadStatus == AssetLoadStatus.Loaded)
			{
				_refCount++;
				handle.Release();
				return;
			}
			_loadHandle = handle;
			_loadHandle.Completed += LoadHandler;
			_loadStatus = AssetLoadStatus.Loading;
			_loadHandle.LoadAsync<{{ConfName}}Data>(_assetPath);
		}

		private void LoadHandler(ConfBaseData configData)
		{
			if (configData == null)
			{
				_loadStatus = AssetLoadStatus.Unload;
				return;
			}
			_refCount = 1;
			rawData = (configData as {{ConfName}}Data)?.source;
			MapData();
			_loadStatus = AssetLoadStatus.Loaded;
			OnLoaded();
			_loadHandle.Release();
			_loadHandle = null;
		}

		public override void Release()
		{
			if(_loadStatus != AssetLoadStatus.Loaded) return;
			_refCount--;
			if (_refCount > 0)
			{
				return;
			}
			rawData.Clear();
			_dictionary.Clear();
			_loadStatus = AssetLoadStatus.Unload;
		}

		private Dictionary<{{KeyType}}, {{ConfName}}> _dictionary = new Dictionary<{{KeyType}}, {{ConfName}}>();

		private void MapData()
		{
			_dictionary.Clear();
			if (rawData == null) return;
			for (var i = 0; i < rawData.Count; i++)
			{
#if isEnumKey
				var keyValue = Enum.TryParse<RolePropConfKey>(rawData[i].id, out var enumKey) ? enumKey : 0;
#else if MultiKey
				var keyValue = (
#for {{KeyList}}
					{{keyName}}: rawData[i].{{keyName}}
#forend
					);
#else
				var keyValue = rawData[i].{{KeyName}};
#endif
				if (_dictionary.ContainsKey(keyValue))
				{
					ConfigLog.LogError($"{{ConfName}} {{KeyName}} 重复，重复值=[{keyValue}]");
					continue;
				}
				_dictionary[keyValue] = rawData[i];
			}
			
#for {{LookupCode}}
            _{{lookupName}} = rawData.ToLookup(conf => ({{LookupKey}}));
#forend
		}
		
		public {{ConfName}} Get({{KeyType}} {{KeyName}})
		{
			if (_loadStatus != AssetLoadStatus.Loaded)
			{
				ConfigLog.LogError("{{ConfName}} is not loaded yet");
				return null;
			}
			return _dictionary.TryGetValue({{KeyName}}, out var conf) ? conf : null;
		}

#for {{LookupCode}}
		private ILookup<{{LookupKey}}, {{ConfName}}> _{{lookupName}};
		
        public IEnumerable<{{ConfName}}> Get({{LookupParameter}})
        {
            if (_loadStatus != AssetLoadStatus.Loaded) return Enumerable.Empty<{{ConfName}}>();
            // ILookup 的索引器如果找不到键，会返回空集合而不是抛出异常
            return _{{lookupName}}[{{LookupKey}}]; 
        }
#forend
	}
****************************************************/


using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PowerCellStudio
{
    public class WriteConfBaseCollectionsHandler
    {
        public static void Write(in CsWriter csWriter, in ExcelReader reader, in ConfigTypeInfo[] configTypeInfoList, in string confName)
		{
			var keys = new List<ConfigTypeInfo>();
			foreach (var info in configTypeInfoList)
			{
				if (info.isKey) keys.Add(info);
			}
			
            csWriter.WriteLine("public partial class " + confName + "Collections : ConfBaseCollections");
            csWriter.StartWriteBody();
            var assetPath = EditorSaveUtils.GetEditorPref(ConfigSettingWindow.SaveKey.assetFilePath, "Assets/ConfigAsset/");
            csWriter.StartWriteMethod(CsWriter.MethodSign.Public,
                    CsWriter.MethodSign.None, 
                    "",
                    confName + "Collections")
                .WriteLine($"_assetPath = \"{assetPath}{confName}Asset.bytes\";")
                .EndWriteMethod();

            csWriter.WriteField(CsWriter.FieldSign.Public, $"List<{confName}>", "rawData", $"new List<{confName}>()")
                .WriteField(CsWriter.FieldSign.Private, "ConfAsyncLoadHandle", "_loadHandle")
                .Space();

            csWriter.StartWriteMethod(CsWriter.MethodSign.Partial, CsWriter.MethodSign.None, "void", "OnLoaded");

            csWriter.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Override, "void", "LoadConfAsync<T>", "T handle")
                .StartWriteIf("_loadStatus == AssetLoadStatus.Loaded")
                .WriteLine("_refCount++;")
                .WriteLine("handle.Release();")
                .WriteLine("return;")
                .EndWriteIf()
                .WriteLine($"_loadHandle = handle;")
                .WriteLine($"_loadHandle.Completed += LoadHandler;")
                .WriteLine($"_loadStatus = AssetLoadStatus.Loading;")
                .WriteLine($"_loadHandle.LoadAsync<{confName}Data>(_assetPath);")
                .EndWriteMethod();

            csWriter.StartWriteMethod(CsWriter.MethodSign.Private, CsWriter.MethodSign.None, "void", "LoadHandler", "ConfBaseData configData")
                .StartWriteIf("configData == null")
                .WriteLine("_loadStatus = AssetLoadStatus.Unload;")
                .WriteLine("return;")
                .EndWriteIf()
                .WriteLine("_refCount = 1;")
                .WriteLine($"rawData = (configData as {confName}Data)?.source;")
                .WriteLine("MapData();")
                .WriteLine("_loadStatus = AssetLoadStatus.Loaded;")
                .WriteLine("OnLoaded();")
                .WriteLine("_loadHandle.Release();")
                .WriteLine("_loadHandle = null;")
                .EndWriteMethod();
            
            csWriter.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Override, "void", "Release")
                .WriteLine("if(_loadStatus != AssetLoadStatus.Loaded) return;")
                .WriteLine("_refCount--;")
                .StartWriteIf("_refCount > 0")
                .WriteLine("return;")
                .EndWriteIf()
                .WriteLine("rawData.Clear();")
                .WriteLine("_dictionary.Clear();")
                .WriteLine("_loadStatus = AssetLoadStatus.Unload;")
                .EndWriteMethod();

            if (keys.Count == 0) keys.Add(configTypeInfoList.FirstOrDefault());
            if (keys.Count == 1)
            {
                var isEnumKey = keys[0].typeName.Equals("string");
                csWriter.WriteField(CsWriter.FieldSign.Private,
                        $"Dictionary<{(isEnumKey ? (confName + "Key") : keys[0].typeName)}, {confName}>",
                        "_dictionary",
                        $"new Dictionary<{(isEnumKey ? (confName + "Key") : keys[0].typeName)}, {confName}>()")
                    .Space();

                csWriter.StartWriteMethod(CsWriter.MethodSign.Private, CsWriter.MethodSign.None, "void", "MapData")
                    .WriteLine("_dictionary.Clear();")
                    .WriteLine("if (rawData == null) return;")
                    .WriteLine("for (var i = 0; i < rawData.Count; i++)")
                    .StartWriteBody();
                if (isEnumKey)
                {
                    csWriter.WriteVar("keyValue", $"{confName}Key.Parse(rawData[i].{keys[0].fieldName})");
                }
                else
                {
                    csWriter.WriteVar("keyValue", $"rawData[i].{keys[0].fieldName}");
                }
                csWriter.StartWriteIf($"_dictionary.ContainsKey(keyValue)")
                    .WriteLine("ConfigLog.LogError($\"" + confName + " Id 重复，重复值=[{keyValue}]\");")
                    .WriteLine("continue;")
                    .EndWriteIf()
                    .WriteLine($"_dictionary[keyValue] = rawData[i];")
                    .EndWriteBody()
                    .EndWriteMethod();

                if (keys.Count == 1 && keys[0].typeName.Equals("string"))
                {
                    csWriter.StartWriteMethod(CsWriter.MethodSign.Public,
                            CsWriter.MethodSign.None,
                            $"{confName}",
                            "Get",
                            $"{confName}Key enumKey")
                        .StartWriteIf("_loadStatus != AssetLoadStatus.Loaded")
                        .WriteLine($"ConfigLog.LogError(\"{confName} is not loaded yet\");")
                        .WriteLine("return null;")
                        .EndWriteIf()
                        .WriteLine($"return _dictionary.TryGetValue(enumKey, out var conf) ? conf : null;")
                        .EndWriteMethod();
                }
                else
                {
                    csWriter.StartWriteMethod(CsWriter.MethodSign.Public,
                            CsWriter.MethodSign.None,
                            $"{confName}",
                            "Get",
                            $"{keys[0].typeName} {keys[0].fieldName}")
                        .StartWriteIf("_loadStatus != AssetLoadStatus.Loaded")
                        .WriteLine($"ConfigLog.LogError(\"{confName} is not loaded yet\");")
                        .WriteLine("return null;")
                        .EndWriteIf()
                        .WriteLine($"return _dictionary.TryGetValue({keys[0].fieldName}, out var conf) ? conf : null;")
                        .EndWriteMethod();
                }
            }
            else
            {
                csWriter.WriteField(CsWriter.FieldSign.Private,
                        $"Dictionary<{keys[0].typeName},  Dictionary<string, {confName}>>",
                        "_dictionary",
                        $"new Dictionary<{keys[0].typeName},  Dictionary<string, {confName}>>()")
                    .Space();

                csWriter.StartWriteMethod(CsWriter.MethodSign.Private,
                        CsWriter.MethodSign.None,
                        "void",
                        "MapData")
                    .WriteLine("_dictionary.Clear();")
                    .WriteLine("for (var i = 0; i < rawData.Count; i++)")
                    .StartWriteBody()
                    .StartWriteIf($"!_dictionary.TryGetValue(rawData[i].{keys[0].fieldName}, out var dic)")
                    .WriteLine($"dic = new Dictionary<string, {confName}>();")
                    .WriteLine($"_dictionary[rawData[i].{keys[0].fieldName}] = dic;")
                    .EndWriteIf();
                csWriter.WriteWithoutLine($"string key = $\"");
                for (int i = 1; i < keys.Count; i++)
                {
                    csWriter.WriteAppend("{")
                        .WriteAppend($"rawData[i].{keys[i].fieldName}")
                        .WriteAppend("}");
                    if (i < keys.Count - 1) csWriter.WriteAppend("&");
                    else csWriter.WriteAppend("\";\n");
                }

                csWriter.StartWriteIf($"dic.ContainsKey(key)")
                    .WriteLine($"ConfigLog.LogError($\"{confName} Id 重复，重复值= [{{rawData[i].{keys[0].fieldName}}}]-[{{key}}]\");")
                    .WriteLine("continue;")
                    .EndWriteIf();
                
                csWriter.WriteLine($"dic[key] = rawData[i];")
                    .EndWriteBody()
                    .EndWriteMethod();

                var paras = keys.Select(o => $"{o.typeName} {o.fieldName}").ToArray();
                csWriter.StartWriteMethod(CsWriter.MethodSign.Public,
                        CsWriter.MethodSign.None,
                        $"{confName}",
                        "Get",
                        paras)
                    .StartWriteIf("_loadStatus != AssetLoadStatus.Loaded")
                    .WriteLine($"ConfigLog.LogError(\"{confName} is not loaded yet\");")
                    .WriteLine("return null;")
                    .EndWriteIf()
                    .StartWriteIf($"!_dictionary.TryGetValue({keys[0].fieldName}, out var dic)")
                    .WriteLine("return null;")
                    .EndWriteIf();
                csWriter.WriteWithoutLine($"string key = $\"");
                for (int i = 1; i < keys.Count; i++)
                {
                    csWriter.WriteAppend("{")
                        .WriteAppend($"{keys[i].fieldName}")
                        .WriteAppend("}");
                    if (i < keys.Count - 1) csWriter.WriteAppend("&");
                    else csWriter.WriteAppend("\";\n");
                }
                csWriter.WriteLine("return dic.TryGetValue(key, out var conf) ? conf : null;");
                csWriter.EndWriteMethod();

                csWriter.StartWriteMethod(CsWriter.MethodSign.Public,
                        CsWriter.MethodSign.None,
                        $"{confName}[]",
                        "Get",
                        $"{keys[0].typeName} {keys[0].fieldName}")
                    .StartWriteIf("_loadStatus != AssetLoadStatus.Loaded")
                    .WriteLine($"ConfigLog.LogError(\"{confName} is not loaded yet\");")
                    .WriteLine("return null;")
                    .EndWriteIf()
                    .StartWriteIf($"!_dictionary.TryGetValue({keys[0].fieldName}, out var dic)")
                    .WriteLine("return null;")
                    .EndWriteIf()
                    .WriteLine("return dic.Values.ToArray();")
                    .EndWriteMethod();
            }
            csWriter.EndWriteBody();
            csWriter.Space();

            if (keys.Count == 1 && keys[0].typeName.Equals("string"))
            {
                WriteConfEnumKeys(csWriter, reader, keys[0], confName);
            }
		}
        
        private static void WriteConfEnumKeys(CsWriter csWriter, ExcelReader excelReader, ConfigTypeInfo keyInfo, string confName)
        {
            var p = excelReader.path.Replace("\\", "/");
            var excelPath = EditorSaveUtils.GetEditorPref(ConfigSettingWindow.SaveKey.excelPath, "");
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
                    ConfigLog.LogError($"配置的Key {valueString} 字段不能重复");
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