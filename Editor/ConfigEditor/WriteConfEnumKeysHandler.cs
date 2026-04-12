using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PowerCellStudio
{
    public class WriteConfEnumKeysHandler
    {
        private static void Write(in CsWriter csWriter, in ConfigTypeInfo keyInfo, in string confName, in ExcelReader excelReader)
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