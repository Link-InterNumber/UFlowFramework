#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;

namespace PowerCellStudio.Editor
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
                var excelPath = EditorSaveUtils.GetEditorPref(ConfigSettingLogic.SaveKey.excelPath, "");
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
                EditorSaveUtils.SetEditorPref(ConfigSettingLogic.SaveKey.excelPath, excelPath);
                var csFileFold = EditorSaveUtils.GetEditorPref(ConfigSettingLogic.SaveKey.csFilePath, "Assets/ConfigScript/");
                var editorCsFold = Path.Combine(csFileFold, "Editor/");
                if (!Directory.Exists(csFileFold))
                {
                    Directory.CreateDirectory(csFileFold);
                }

                if (!Directory.Exists(editorCsFold))
                {
                    Directory.CreateDirectory(editorCsFold);
                }
                var filePaths = Directory.GetFiles(excelPath);
                var collectionList = new List<string>();
                EditorUtility.DisplayProgressBar("Create Cs Files", "Start Running", 0f);
                ResolversTypeBuffer.InitBuffer();
                for (var i = 0; i < filePaths.Length; i++)
                {
                    var filePath = filePaths[i];
                    var fileName = Path.GetFileName(filePath);
                    if (fileName.StartsWith("~$")) continue;
                    var extension = Path.GetExtension(filePath);
                    
                    IConfigReader reader = null;
                    if (extension == ".xlsx")
                        reader = new ExcelReader(filePath);
                    else if (extension == ".csv")
                        reader = new CsvReader(filePath);
                    
                    if(reader == null || reader.fieldMap.Count == 0) continue;
                    
                    var writer = new ConfigWriter();
                    writer.GenerateRuntimeCsString(reader);
                    var code = writer.GetCSFileString();
                    
                    writer.Clear();
                    writer.GenerateEditorCsString(reader);
                    var editorCode = writer.GetCSFileString();
                    
                    collectionList.Add($"{reader.fileName}Collections");
                    EditorUtility.DisplayProgressBar("Create Cs Files", $"Running {reader.fileName}", 1f * i / filePaths.Length);
                    var csFilePath = Path.Combine(csFileFold, $"{reader.fileName}Data.cs");
                    File.WriteAllText(csFilePath, code, Encoding.UTF8);
                    
                    var editorCsFilePath = Path.Combine(editorCsFold, $"{reader.fileName}Creator.cs");
                    File.WriteAllText(editorCsFilePath, editorCode, Encoding.UTF8);
                    ConfigLogger.Log($"Create Cs Files From [{reader.fileName}]");
                    reader.Dispose();
                }
                EditorUtility.DisplayProgressBar("Create Cs Files", $"Running ConfigManager", 1f * (filePaths.Length -1) / filePaths.Length);
                var managerCode = ConfigWriter.GenerateManagerCSString(collectionList);
                var managerFilePath = Path.Combine(csFileFold, "ConfigManager.cs");
                File.WriteAllText(managerFilePath, managerCode, Encoding.UTF8);
            }
            catch (Exception e)
            {
                ConfigLogger.LogError($"{e.Message}\n{e.StackTrace}");
            }
            finally
            {
                ResolversTypeBuffer.ClearBuffer();
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }

        }

        [MenuItem("Tools/Config/Create Config Assets", false, 101)]
        public static void CreateConfigAsset()
        {
            CreateConfigAssetsInternal(false);
        }

        [MenuItem("Tools/Config/Create Config Assets By Force", false, 102)]
        public static void CreateConfigAssetByForce()
        {
            CreateConfigAssetsInternal(true);
        }

        private static void CreateConfigAssetsInternal(bool force)
        {
            try
            {
                var excelPath = EditorSaveUtils.GetEditorPref(ConfigSettingLogic.SaveKey.excelPath, "");
                if (!Directory.Exists(excelPath))
                {
                    EditorUtility.DisplayDialog("ConfigMenu", "Excel files path doesn't exist.", "OK");
                    return;
                }
                md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                var historyMap = force ? null : LoadHistoryFile(excelPath);

                var assetFilePath = ConfigManager.assetFolderPath;
                if (!Directory.Exists(assetFilePath))
                {
                    Directory.CreateDirectory(assetFilePath);
                }
                var types = Assembly.GetAssembly(typeof(ConfCreator))
                    .GetTypes()
                    .Where(t => 
                        !t.IsAbstract &&
                        t.IsClass &&
                        t.IsSubclassOf(typeof(ConfCreator)))
                    .ToArray();
                EditorUtility.DisplayProgressBar("Create Config Assets", "Start Running", 0f);
                LocalizationConfigBuffer.PrepareBuffer();
                var paramsArray = new object[1];
                for (var i = 0; i < types.Length; i++)
                {
                    LocalizationConfigBuffer.ClearBuffer();
                    var type = types[i];
                    EditorUtility.DisplayProgressBar("Create Config Assets", $"Running {type.Name}", 1f * i / types.Length);
                    var mathod = type.GetMethod("CreatAsset", BindingFlags.Static | BindingFlags.Public);
                    if (mathod == null) continue;
                    var oldMd5 = "-1";
                    if (!force)
                    {
                        historyMap.TryGetValue(type.Name, out oldMd5);
                    }

                    paramsArray[0] = oldMd5;
                    var md5String = (string) mathod.Invoke(null, paramsArray);
                    if (!force)
                    {
                        historyMap[type.Name] = md5String;
                    }
                    if (!LocalizationConfigBuffer.hasBuffer) continue;
                    UnityLocalizationWriter.AddToLocalizationFile(LocalizationConfigBuffer.GetStringRefs());
                    UnityLocalizationWriter.AddToLocalizationFile(LocalizationConfigBuffer.GetAssetRefs());
                }
                if (!force)
                {
                    SaveHistoryFile(excelPath, historyMap);
                }
                UnityLocalizationCsvExporter.Export();
                // ConfigLocalizationCsvToAsset.Produce();
                // var csvDirectory = Path.Combine(excelPath, ConfigSettingLogic.LocalizationFolderName);
                // TODO 解析csv文件，生成本地化资源
                
                
                // 获取assetFilePath下的文件列表，保存为一个txt文件，命名为ConfigAssetList.txt，内容为每行一个文件名
                var sb = new StringBuilder();
                var assetFiles = Directory.GetFiles(assetFilePath).Select(Path.GetFileName);
                foreach (var assetFile in assetFiles)
                {
                    sb.AppendLine(assetFile);
                }
                
                // var configLocalizationDirectory = Path.Combine(assetFilePath, ConfigSettingLogic.LocalizationFolderName);
                // var configLocalizationAssets = Directory.GetFiles(configLocalizationDirectory, "*.csv").Select(Path.GetFileName);
                // foreach (var assets in configLocalizationAssets)
                // {
                //     sb.AppendLine($"{ConfigSettingLogic.LocalizationFolderName}/{assets}");
                // }
                var listFilePath = Path.Combine(assetFilePath, ConfigManager.configAssetListName);
                
                File.WriteAllText(listFilePath, sb.ToString(), Encoding.UTF8);
                LocalizationConfigBuffer.DisposeBuffer();
                UnityLocalizationWriter.ClearCache();
            }
            catch (Exception e)
            {
                ConfigLogger.LogError($"{e.Message}\n{e.StackTrace}");
            }
            finally
            { 
                md5.Dispose();
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
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

                using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
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
                ConfigLogger.LogError($"Calc MD5 failed, exception:{ex.Message}\n{ex.StackTrace}");
            }
            return string.Empty;
        }

        [MenuItem("Tools/Config/Delete Config Assets", false, 102)]
        public static void DeleteConfigAsset()
        {
            var assetPath = ConfigManager.assetFolderPath;
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
            var excelPath = EditorSaveUtils.GetEditorPref(ConfigSettingLogic.SaveKey.excelPath, "");
            SaveHistoryFile(excelPath, new Dictionary<string, string>());
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }
    }
}
#endif
