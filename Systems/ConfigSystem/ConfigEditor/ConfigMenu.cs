#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.CSV;
using UnityEditor.Localization.Plugins.CSV.Columns;
using UnityEditor.Localization.Plugins.XLIFF.V20;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace PowerCellStudio
{
    public static class ConfigMenu
    {
        private static System.Security.Cryptography.MD5CryptoServiceProvider md5;

        // private static string _localizationCsvHeader =
        //     "Key,Id,Chinese (Simplified)(zh-Hans),Chinese (Traditional)(zh-Hant),English(en),Japanese(ja)\n";
        
        [MenuItem("Tools/Config/Create Cs Files", false, 100)]
        public static void CreateCsFiles()
        {
            try
            {
                var excelPath = EditorPrefs.GetString(ConfigSettingWindow.SaveKey.excelPath);
                if (string.IsNullOrEmpty(excelPath) || !Directory.Exists(excelPath))
                {
                    excelPath = EditorUtility.OpenFolderPanel("Select the folder of excel files", Environment.CurrentDirectory, "");
                }
                if (string.IsNullOrEmpty(excelPath))
                    return;
                if (!Directory.Exists(excelPath))
                {
                    EditorUtility.DisplayDialog("ConfigMenu", "Excel files path doesn't exist.", "OK");
                    return;
                }
                EditorPrefs.SetString(ConfigSettingWindow.SaveKey.excelPath, excelPath);
                var csFilePath = EditorPrefs.GetString(ConfigSettingWindow.SaveKey.csFilePath, "Assets/ConfigScript/");
                if (!Directory.Exists(csFilePath))
                {
                    Directory.CreateDirectory(csFilePath);
                }
                var filePaths = Directory.GetFiles(excelPath);
                var collectionList = new List<string>();
                EditorUtility.DisplayProgressBar("Create Cs Files", "Start Running", 0f);
                for (var i = 0; i < filePaths.Length; i++)
                {
                    var filePath = filePaths[i];
                    var fileName = Path.GetFileName(filePath);
                    if (fileName.StartsWith("~$") || Path.GetExtension(filePath) != ".xlsx") continue;
                    using var reader = new ExcelReader(filePath);
                    if(reader.fieldMap.Count == 0) continue;
                    var writer = new ConfigWriter(reader.path, reader.fileName, reader.fieldMap);
                    writer.GenerateCSString();
                    var code = writer.GetCSFileString();
                    collectionList.Add($"{reader.fileName}Collections");
                    EditorUtility.DisplayProgressBar("Create Cs Files", $"Running {reader.fileName}", 1f * i / filePaths.Length);
                    File.WriteAllText(csFilePath + reader.fileName + "Data.cs", code, Encoding.UTF8);
                    ConfigLog.Log($"Create Cs Files From [{reader.fileName}]");
                }
                EditorUtility.DisplayProgressBar("Create Cs Files", $"Running ConfigManager", 1f * (filePaths.Length -1) / filePaths.Length);
                var managerCode = ConfigWriter.GenerateManagerCSString(collectionList);
                File.WriteAllText(csFilePath + "ConfigManager.cs", managerCode, Encoding.UTF8);
            }
            catch (Exception e)
            {
                ConfigLog.LogError(e.ToString());
                throw;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }

        }

        [MenuItem("Tools/Config/Create Config Assets", false, 101)]
        public static void CreateConfigAsset()
        {
            try
            {
                var excelPath = EditorPrefs.GetString(ConfigSettingWindow.SaveKey.excelPath);
                if (!Directory.Exists(excelPath))
                {
                    EditorUtility.DisplayDialog("ConfigMenu", "Excel files path doesn't exist.", "OK");
                    return;
                }
                md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                Dictionary<string, string> historyMap = LoadHistoryFile(excelPath);

                var assetFilePath = EditorPrefs.GetString(ConfigSettingWindow.SaveKey.assetFilePath, "Assets/ConfigAsset/");
                if (!Directory.Exists(assetFilePath))
                {
                    Directory.CreateDirectory(assetFilePath);
                }
                var types = Assembly.GetAssembly(typeof(ConfBaseData)).GetTypes().Where(t => 
                    !t.IsAbstract &&
                    t.IsClass &&
                    t.IsSubclassOf(typeof(ConfBaseData))).ToArray();
                EditorUtility.DisplayProgressBar("Create Config Assets", "Start Running", 0f);
                for (var i = 0; i < types.Length; i++)
                {
                    var type = types[i];
                    EditorUtility.DisplayProgressBar("Create Config Assets", $"Running {type.Name}", 1f * i / types.Length);
                    var mathod = type.GetMethod("CreatAsset", BindingFlags.Static | BindingFlags.Public);
                    if (mathod == null) continue;
                    historyMap.TryGetValue(type.Name, out var oldMd5);
                    var md5String = (string) mathod.Invoke(null, new object[]{oldMd5});
                    historyMap[type.Name] = md5String;
                }
                SaveHistoryFile(excelPath, historyMap);
            }
            catch (Exception e)
            {
                ConfigLog.LogError(e.ToString());
                throw;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                md5.Dispose();
            }

        }
        
        [MenuItem("Tools/Config/Create Config Assets By Force", false, 102)]
        public static void CreateConfigAssetByForce()
        {
            try
            {
                var excelPath = EditorPrefs.GetString(ConfigSettingWindow.SaveKey.excelPath);
                if (!Directory.Exists(excelPath))
                {
                    EditorUtility.DisplayDialog("ConfigMenu", "Excel files path doesn't exist.", "OK");
                    return;
                }
                md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                // Dictionary<string, string> historyMap = LoadHistoryFile(excelPath);

                var assetFilePath = EditorPrefs.GetString(ConfigSettingWindow.SaveKey.assetFilePath, "Assets/ConfigAsset/");
                if (!Directory.Exists(assetFilePath))
                {
                    Directory.CreateDirectory(assetFilePath);
                }
                var types = Assembly.GetAssembly(typeof(ConfBaseData)).GetTypes().Where(t => 
                    !t.IsAbstract &&
                    t.IsClass &&
                    t.IsSubclassOf(typeof(ConfBaseData))).ToArray();
                EditorUtility.DisplayProgressBar("Create Config Assets", "Start Running", 0f);
                for (var i = 0; i < types.Length; i++)
                {
                    var type = types[i];
                    EditorUtility.DisplayProgressBar("Create Config Assets", $"Running {type.Name}", 1f * i / types.Length);
                    var mathod = type.GetMethod("CreatAsset", BindingFlags.Static | BindingFlags.Public);
                    if (mathod == null) continue;
                    // historyMap.TryGetValue(type.Name, out var oldMd5);
                    mathod.Invoke(null, new object[]{"-1"});
                    // historyMap[type.Name] = md5String;
                }
                // SaveHistoryFile(excelPath, historyMap);
            }
            catch (Exception e)
            {
                ConfigLog.LogError(e.ToString());
                throw;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                md5.Dispose();
            }

        }

        private static Dictionary<string, string> LoadHistoryFile(string excelPath)
        {
            var csvPath = Path.Combine(excelPath, "ConfigAsset.csv");
            Encoding encoding = Encoding.UTF8; //Encoding.ASCII;//
            Dictionary<string, string> dt = new Dictionary<string, string>();
            if (!File.Exists(csvPath))
            {
                return dt;
            }
            using FileStream fs = new FileStream(csvPath, FileMode.Open, FileAccess.Read);
            using StreamReader sr = new StreamReader(fs, encoding);
            string strLine = "";
            while ((strLine = sr.ReadLine()) != null)
            {
                if(strLine == "\n" || string.IsNullOrEmpty(strLine)) continue;
                var strArr = strLine.Split(',');
                dt.Add(strArr[0], strArr[1].Replace("\n", ""));
            }
            sr.Close();
            fs.Close();
            return dt;
        }

        private static void SaveHistoryFile(string excelPath, Dictionary<string, string> map)
        {
            if(!Directory.Exists(excelPath)) return;
            var csvPath = Path.Combine(excelPath, "ConfigAsset.csv");
            Encoding encoding = Encoding.UTF8; //Encoding.ASCII;//
            var text = new StringBuilder();
            foreach (var (key, value) in map)
            {
                text.Append($"{key},{value}\n");
            }

            using (StreamWriter sw = new StreamWriter(csvPath, false, encoding))
            {
                sw.Write(text.ToString());
                sw.Close();
            }
            // File.WriteAllText(csvPath ,text.ToString(), encoding);
            text.Clear();
        }
        
        public static string CalFileMD5(string file)
        {
            try
            {
                if (!File.Exists(file)) return string.Empty;
                var sb = new StringBuilder();
                byte[] bytes = null;

                using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    bytes = new byte[fs.Length];
                    fs.Read(bytes, 0, bytes.Length);
                    fs.Close();
                }
                
                byte[] retVal = md5.ComputeHash(bytes, 0, bytes.Length);
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }

                var md5String = sb.ToString();
                sb.Clear();
                return md5String;
            }
            catch (Exception ex)
            {
                ConfigLog.LogError(ex.ToString());
            }
            return string.Empty;
        }

        [MenuItem("Tools/Config/Delete Config Assets", false, 102)]
        public static void DeleteConfigAsset()
        {
            var assetPath = EditorPrefs.GetString(ConfigSettingWindow.SaveKey.assetFilePath, "Assets/ConfigAsset/");
            if (!Directory.Exists(assetPath))
            {
                return;
            }
            Selection.activeObject = null;
            var guids = AssetDatabase.FindAssets($"t:{(ConstSetting.ConfigConfigSaveMode == ConstSetting.ConfigSaveMode.ScriptableObject ? "ScriptableObject" : "TextAsset")} ConfAsset", new[] {assetPath}).ToArray();
            var paths = guids.Select(AssetDatabase.GUIDToAssetPath).ToArray();
            EditorUtility.DisplayProgressBar("Clear Config Assets", "Start Running", 0f);
            for (var i = 0; i < paths.Length; i++)
            {
                var path = paths[i];
                EditorUtility.DisplayProgressBar("Delete Config Assets", $"Delete Asset {path}", 1f * i / paths.Length);
                AssetDatabase.DeleteAsset(path);
            }
            var excelPath = EditorPrefs.GetString(ConfigSettingWindow.SaveKey.excelPath);
            SaveHistoryFile(excelPath, new Dictionary<string, string>());
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }
        
        private static ConfBaseData GetConfData(string path)
        {
            switch (ConstSetting.ConfigConfigSaveMode)
            {
                case ConstSetting.ConfigSaveMode.ScriptableObject:
                {
                    var data = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                    if (data == null)
                    {
                        ConfigLog.LogError($"Cannot find asset at path {path}");
                        return null;
                    }

                    return null;
                }
                case ConstSetting.ConfigSaveMode.Json:
                {
                    var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                    if (textAsset == null)
                    {
                        ConfigLog.LogError($"Cannot find asset at path {path}");
                        return null;
                    }
                    var json = EncryptUtils.AESDecrypt(textAsset.text, ConstSetting.FileEncryptionKey);
                    var typeName = Path.GetFileNameWithoutExtension(path);
                    // typeName = typeName.Replace("Assets/ConfigAsset/", "");
                    typeName = typeName.Remove(typeName.Length - 5, 5);
                    typeName = typeName + "Data";
                    var type = Assembly.GetAssembly(typeof(ConfBaseData)).GetTypes().FirstOrDefault(t => t.Name == typeName);
                    var data = JsonConvert.DeserializeObject(json, type) as ConfBaseData;
                    return data;
                }
                case ConstSetting.ConfigSaveMode.Binary:
                {
                    var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                    if (textAsset == null)
                    {
                        ConfigLog.LogError($"Cannot find asset at path {path}");
                        return null;
                    }
                    var bytes = EncryptUtils.AESDecrypt(textAsset.bytes, ConstSetting.FileEncryptionKey);
                    using MemoryStream stream = new MemoryStream(bytes);
                    BinaryFormatter formatter = new BinaryFormatter();
                    var deserializeObj = formatter.Deserialize(stream);
                    // var typeName = Path.GetFileNameWithoutExtension(path);
                    // typeName = typeName.Remove(typeName.Length - 5, 5);
                    // typeName = typeName + "Data";
                    // var type = Assembly.GetAssembly(typeof(ConfBaseData)).GetTypes().FirstOrDefault(t => t.Name == typeName);
                    var data = deserializeObj as ConfBaseData;
                    return data;
                }
                default:
                    break;
            }
            return null;
        }
        
        [MenuItem("Tools/Config/Create Localization csv", false, 103)]
        public static void CreateLocalizationCsv()
        {
            try
            {
                var assetPath = EditorPrefs.GetString(ConfigSettingWindow.SaveKey.assetFilePath, "Assets/ConfigAsset/");
                var assetType = "ScriptableObject";
                switch (ConstSetting.ConfigConfigSaveMode)
                {
                    case ConstSetting.ConfigSaveMode.ScriptableObject:
                        break;
                    case ConstSetting.ConfigSaveMode.Binary:
                    case ConstSetting.ConfigSaveMode.Json:
                        assetType = "TextAsset";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                var assets = AssetDatabase.FindAssets($"t:{assetType} ConfAsset", new []{assetPath} );
                var stringTable = LocalizationSettings.StringDatabase.GetTable(ConstSetting.LocalizationStringTable);
                var assetTable = LocalizationSettings.AssetDatabase.GetTable(ConstSetting.LocalizationAssetTable);
                if(!stringTable && !assetTable) return;
                for (var i = 0; i < assets.Length; i++)
                {
                    var guid = assets[i];
                    var conf = GetConfData(AssetDatabase.GUIDToAssetPath(guid));
                    if (conf == null) continue;
                    var sourceField =  conf.GetType().GetField("source", BindingFlags.Public | BindingFlags.Instance);
                    if (sourceField == null) continue;
                    var source = (IList) sourceField.GetValue(conf);
                    var dataCount = source.Count;
                    var confName = conf.GetType().Name;
                    for (var j = 0; j < dataCount; j++)
                    {
                        var data = source[j];
                        var fields = data.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                        foreach (var fieldInfo in fields)
                        {
                            if (fieldInfo.FieldType == typeof(LocalizationStringRef) && stringTable)
                            {
                                var localString = fieldInfo.GetValue(data) as LocalizationStringRef;
                                if (localString == null || string.IsNullOrEmpty(localString.localizationKey)) continue;
                                var entry = stringTable.AddEntry(localString.localizationKey, localString.rawString);
                                stringTable.SharedData.AddKey(entry.Key, entry.KeyId);
                            }
                            else if (fieldInfo.FieldType == typeof(LocalizationAssetRef) && assetTable)
                            {
                                var localAsset = fieldInfo.GetValue(data) as LocalizationAssetRef;
                                if (localAsset == null || string.IsNullOrEmpty(localAsset.localizationKey)) continue;
                                var entry = assetTable.AddEntry(localAsset.localizationKey, AssetDatabase.AssetPathToGUID(localAsset.rawString));
                                assetTable.SharedData.AddKey(entry.Key, entry.KeyId);
                            }
                            EditorUtility.DisplayProgressBar("Config", $"{confName} to Localization", 1f * j / dataCount);
                        }
                    }
                }
                
                // var excelPath = EditorPrefs.GetString(ConfigSettingWindow.SaveKey.excelPath);
                var csvPath = EditorPrefs.GetString(ConfigSettingWindow.SaveKey.localizationCSVPath, Path.Combine(Environment.CurrentDirectory, "ExcelFiles/Localization")); //Path.Combine(excelPath, "Localization");
                if (!Directory.Exists(csvPath))
                {
                    Directory.CreateDirectory(csvPath);
                }
                EditorUtility.SetDirty(stringTable);
                EditorUtility.SetDirty(assetTable);
                AssetDatabase.SaveAssetIfDirty(stringTable);
                AssetDatabase.SaveAssetIfDirty(assetTable);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayProgressBar("Config", "Export csv file", 0f);
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
                System.Diagnostics.Process.Start(csvPath);
            }
            catch (Exception e)
            {
                ConfigLog.LogError(e.ToString());
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif
