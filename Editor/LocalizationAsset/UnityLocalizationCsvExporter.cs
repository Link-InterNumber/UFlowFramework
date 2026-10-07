using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.CSV;
using UnityEditor.Localization.Plugins.CSV.Columns;

namespace PowerCellStudio.Editor
{
    public static class UnityLocalizationCsvExporter
    {
        public static void Export()
        {
            try
            {
                var excelPath = EditorSaveUtils.GetEditorPref(ConfigSettingLogic.SaveKey.excelPath, "");
                var csvPath = Path.Combine(excelPath, ConfigSettingLogic.LocalizationFolderName);
                if (!Directory.Exists(csvPath))
                {
                    Directory.CreateDirectory(csvPath);
                }
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayProgressBar("Localization", "Export csv file", 0f);
                var date = DateTime.Now;
                var fileName = Path.Combine(csvPath, $"{date:yyyy-MM-dd-HH-mm-ss}.csv");//文件名
                using var sw = new StreamWriter(fileName, true, Encoding.UTF8);
                var columnMappings = new List<CsvColumns>();
                columnMappings.Add(new KeyIdColumns()
                {
                    IncludeId = false,
                    IncludeSharedComments = false
                });
                foreach (var locale in LocalizationEditorSettings.GetLocales())
                {
                    columnMappings.Add(new LocaleColumns()
                    {
                        LocaleIdentifier = locale.Identifier,
                        IncludeComments = false
                    });
                }
                Csv.Export(sw, LocalizationEditorSettings.GetStringTableCollection(ConstSetting.LocalizationStringTable), columnMappings);
                sw.Close();
                // var fullPath = Path.GetFullPath(csvPath);
                // System.Diagnostics.Process.Start(fullPath);
            }
            catch (Exception e)
            {
                ConfigLogger.LogError($"{e.Message}\n{e.StackTrace}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }
        }
    }
}