// using System.Collections.Generic;
// using System.IO;
// using UnityEditor;
// using UnityEngine;
// using UnityEngine.Localization.Settings;
//
// namespace PowerCellStudio
// {
//     public class AssetsBundleLocalizationAssetTable_Editor
//     {
//         // 在Resources文件夹下创建一个AssetsBundleLocalizationAssetTable.asset文件
//         [MenuItem("Tools/AssetsBundleLocalizationAssetTable", false, 2)]
//         public static void Create()
//         {
//             // 加载Localization的AssetTable，文件名ConstSetting.LocalizationAssetTable
//             var assetTables = LocalizationSettings.AssetDatabase.GetTable(ConstSetting.LocalizationAssetTable);
//             if (assetTables == null)
//             {
//                 AssetLog.LogError($"Can not find asset table: {ConstSetting.LocalizationAssetTable}");
//                 return;
//             }
//
//             var tableItems = new List<AssetsBundleLocalizationAssetTableItem>();
//             foreach (var keyNEntry in assetTables)
//             {
//                 var assetPath = AssetDatabase.GUIDToAssetPath(keyNEntry.Value.Address);
//                 if (string.IsNullOrEmpty(assetPath))
//                 {
//                     AssetLog.LogError($"Can not find asset path: {keyNEntry.Value.Address}");
//                     continue;
//                 }
//                 var item = new AssetsBundleLocalizationAssetTableItem
//                 {
//                     key = keyNEntry.Value.Key,
//                     defaultPath = assetPath,
//                 };
//                 tableItems.Add(item);
//             }
//             
//             var tableData = ScriptableObject.CreateInstance<AssetsBundleLocalizationAssetTable>();
//             tableData.items = tableItems;
//             
//             var path = Path.Combine("Assets", "Resources", ConstSetting.LocalizationAssetConfigFolder,
//                 ConstSetting.LocalizationAssetConfigName);
//             AssetDatabase.CreateAsset(tableData, path);
//             //保存创建的资源
//             AssetDatabase.SaveAssets();
//             //刷新界面
//             AssetDatabase.Refresh();
//         }
//     }
// }