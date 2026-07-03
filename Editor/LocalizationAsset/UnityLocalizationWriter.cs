using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace PowerCellStudio.Editor
{
    public static class UnityLocalizationWriter
    {
        private static StringTable stringTable;
        private static AssetTable assetTable;
        
        public static void ClearCache()
        {
            stringTable = null;
            assetTable = null;
        }
        
        public static void AddToLocalizationFile(List<LocalizationStringRef> stringRefs)
        {
            if (stringRefs == null || stringRefs.Count == 0)
                return;
            if (stringTable == null)
            {
                stringTable = LocalizationSettings.StringDatabase.GetTable(ConstSetting.LocalizationStringTable);
            }
            foreach (var stringRef in stringRefs)
            {
                var entry = stringTable.AddEntry(stringRef.localizationKey, stringRef.rawString);
                stringTable.SharedData.AddKey(entry.Key, entry.KeyId);
            }
            
            UnityEditor.EditorUtility.SetDirty(stringTable);
            UnityEditor.EditorUtility.SetDirty(stringTable.SharedData);
            
            // var excelPath = EditorSaveUtils.GetEditorPref(ConfigSettingLogic.SaveKey.excelPath, "");
            // var directory = Path.Combine(excelPath, ConfigSettingLogic.LocalizationFolderName);
            // if (!Directory.Exists(directory))
            // {
            //     Directory.CreateDirectory(directory);
            // }
            // var lan = Enum.GetNames(typeof(Language));
            // for (var i = 0; i < lan.Length; i++)
            // {
            //     var csvPath = Path.Combine(directory, configName.Replace("Creator", "String") + $"{lan[i]}.csv");
            //     if (!Directory.Exists(csvPath))
            //     {
            //         Directory.CreateDirectory(csvPath);
            //     }
            //
            //     var sb = new StringBuilder();
            //     sb.AppendLine($"Key,RawString,{lan[i]}");
            //     foreach (var stringRef in stringRefs)
            //     {
            //         if (i == 0)
            //             sb.AppendLine($"{stringRef.localizationKey},{stringRef.rawString},{stringRef.rawString}");
            //         else
            //             sb.AppendLine($"{stringRef.localizationKey},{stringRef.rawString},");
            //     }
            //     File.WriteAllText(csvPath, sb.ToString());
            // }
        }
        
        public static void AddToLocalizationFile(List<LocalizationAssetRef> assetRefs)
        {
            if (assetRefs == null || assetRefs.Count == 0)
                return;
            if (assetTable == null)
            {
                assetTable = LocalizationSettings.AssetDatabase.GetTable(ConstSetting.LocalizationAssetTable);
            }
            foreach (var assetRef in assetRefs)
            {
                var entry = assetTable.AddEntry(assetRef.localizationKey, UnityEditor.AssetDatabase.AssetPathToGUID(assetRef.rawString));
                assetTable.SharedData.AddKey(entry.Key, entry.KeyId);
            }
            
            UnityEditor.EditorUtility.SetDirty(assetTable);
            UnityEditor.EditorUtility.SetDirty(assetTable.SharedData);
            
            // var excelPath = EditorSaveUtils.GetEditorPref(ConfigSettingLogic.SaveKey.excelPath, "");
            // var directory = Path.Combine(excelPath, ConfigSettingLogic.LocalizationFolderName);
            // if (!Directory.Exists(directory))
            // {
            //     Directory.CreateDirectory(directory);
            // }
            // var lan = Enum.GetNames(typeof(Language));
            // for (var i = 0; i < lan.Length; i++)
            // {
            //     var csvPath = Path.Combine(directory, configName.Replace("Creator", "Asset") + $"{lan[i]}.csv");
            //     var sb = new StringBuilder();
            //     sb.AppendLine($"Key,RawString,{lan[i]}");
            //     foreach (var assetRef in assetRefs)
            //     {
            //         if (i == 0)
            //             sb.AppendLine($"{assetRef.localizationKey},{assetRef.rawString},{assetRef.rawString}");
            //         else
            //             sb.AppendLine($"{assetRef.localizationKey},{assetRef.rawString},");
            //     }
            //     File.WriteAllText(csvPath, sb.ToString());
            // }
        }
        
        private sealed class TempMark
        {
            public bool has;
        }
        
        public static void CollectTextsFromGameObject(string folderPath)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Path,Text");
            var mark = new TempMark();
            var stringList = new List<LocalizationStringRef>();
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                if (prefab != null)
                {
                    CollectTextsFromGameObjectHandler(prefab.transform, string.Empty, ref mark, ref stringList);
                    if (mark.has)
                    {
                        EditorUtility.SetDirty(prefab);
                    }
                }

                mark.has = false;
            }
            AddToLocalizationFile(stringList);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Export Complete", $"Exported text data to Localization", "OK");
        }

        private static void CollectTextsFromGameObjectHandler(Transform transform, string parentPath,
            ref TempMark mark, ref List<LocalizationStringRef> stringList)
        {
            string currentPath = string.IsNullOrEmpty(parentPath) ? transform.name : $"{parentPath}_{transform.name}";

            var textComponent = transform.GetComponent<TextEx>();
            if (textComponent != null && textComponent.staticText)
            {
                mark.has = true;
                textComponent.localizationKey = currentPath;
                stringList.Add(new LocalizationStringRef()
                {
                    localizationKey = currentPath,
                    rawString = textComponent.text
                });
            }
            var tmpComponent = transform.GetComponent<TextMeshProUGUIEx>();
            if (tmpComponent != null && tmpComponent.staticText)
            {
                mark.has = true;
                tmpComponent.localizationKey = currentPath;
                stringList.Add(new LocalizationStringRef()
                {
                    localizationKey = currentPath,
                    rawString = textComponent.text
                });
            }

            foreach (Transform child in transform)
            {
                CollectTextsFromGameObjectHandler(child, currentPath, ref mark, ref stringList);
            }
        }
    }
}