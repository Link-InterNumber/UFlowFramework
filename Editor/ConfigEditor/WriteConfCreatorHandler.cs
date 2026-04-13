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
			if (md5 == oldMd5 && File.Exists("Assets/ConfigAsset/{{ConfName}}Asset.{{ExtensionName}}"))
			{
				return md5;
			}

			var asset = new {{ConfName}}Data();
			using (var reader = new ExcelReader(path))
			{
				var ws = reader.workbook.Worksheets[1];
				var rowCount = ws.Dimension.Rows;
				for (int raw = 4; raw <= rowCount; raw++)
				{
					var firstCell = ws.Cells[raw, {{MiniColumn}}].Value;
					if (firstCell == null || string.IsNullOrEmpty(firstCell.ToString())) continue;
					var fileName = "{{ConfName}}";

#for {{ParseStatements}}
					{{ParseStatement}}
#forend

					var data = new {{ConfName}}(
#for {{CtorArguments}}
						{{ArgumentValue}}
#forend
					);
					asset.source.Add(data);
				}
			}

			var bytes = SerializeUtils.SerializeToBinary(asset);
			bytes = EncryptUtils.AESEncrypt(bytes, ConstSetting.FileEncryptionKey);
			File.WriteAllBytes("Assets/ConfigAsset/{{ConfName}}Asset.{{ExtensionName}}", bytes);
			ConfigLog.Log("Config Asset Created => [{{ConfName}}]");
			return md5;
		}
		#endif
	}
****************************************************/

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PowerCellStudio
{
	public class WriteConfCreatorHandler
	{
		public static void Write(in CsWriter csWriter, in ExcelReader reader, in ConfigTypeInfo[] configTypeInfoList,
			in string confName)
		{
			csWriter.WriteLine($"public class {confName}Creator : ConfCreator");
			csWriter.StartWriteBody();

			csWriter.WriteLineWithoutTab("#if UNITY_EDITOR");
			WriteCreateAssetMethod(csWriter, reader, configTypeInfoList, confName);
			csWriter.WriteLineWithoutTab("#endif");

			csWriter.EndWriteBody();
		}

		private static void WriteCreateAssetMethod(CsWriter csWriter, ExcelReader reader,
			ConfigTypeInfo[] configTypeInfoList, string confName)
		{
			var excelFileName = reader.path.Replace("\\", "/").Split('/').LastOrDefault();
			var assetPath = EditorSaveUtils.GetEditorPref(ConfigSettingWindow.SaveKey.assetFilePath, "Assets/ConfigAsset/");
			var miniColumn = configTypeInfoList.Min(info => info.columns.Min());

			csWriter.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Static, "string", "CreatAsset", "string oldMd5")
				.WriteLine("var excelPath = EditorSaveUtils.GetEditorPref(ConfigSettingWindow.SaveKey.excelPath, \"\");")
				.WriteLine($"string path = excelPath + \"/{excelFileName}\";");

			csWriter.StartWriteIf("!File.Exists(path)")
				.WriteLine("ConfigLog.LogError(\"Cannot find file \" + path);")
				.WriteLine("return string.Empty;")
				.EndWriteIf();

			csWriter.WriteVar("md5", "ConfigMenu.CalFileMD5(path)");

			csWriter.StartWriteIf($"md5 == oldMd5 && File.Exists(\"{assetPath}{confName}Asset.bytes\")")
				.WriteLine("return md5;")
				.EndWriteIf();;

			csWriter.WriteVar("asset", $"new {confName}Data()")
				.WriteLine("using (var reader = new ExcelReader(path))");
                
			csWriter.StartWriteBody();

			csWriter.WriteVar("ws", "reader.workbook.Worksheets[1]")
				.WriteVar("rowCount", "ws.Dimension.Rows")
				.WriteLine("for (int raw = 4; raw <= rowCount; raw++)");
            
			csWriter.StartWriteBody();

			csWriter.WriteVar("firstCell", $"ws.Cells[raw, {miniColumn}].Value")
				.WriteLine("if (firstCell == null || string.IsNullOrEmpty(firstCell.ToString())) continue;")
				.WriteVar("fileName", $"\"{confName}\"");

			WriteParseStatements(csWriter, configTypeInfoList);
			WriteCreateDataStatement(csWriter, confName, configTypeInfoList);

			csWriter.WriteLine("asset.source.Add(data);");
			csWriter.EndWriteBody();
			csWriter.EndWriteBody();

			csWriter.WriteLine("var bytes = SerializeUtils.SerializeToBinary(asset);")
				.WriteLine("bytes = EncryptUtils.AESEncrypt(bytes, ConstSetting.FileEncryptionKey);")
				.WriteLine($"File.WriteAllBytes(\"{assetPath}{confName}Asset.bytes\", bytes);")
				.WriteLine($"ConfigLog.Log(\"Config Asset Created => [{confName}]\");")
				.WriteLine("return md5;")
				.EndWriteMethod();
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