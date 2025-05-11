// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.IO;
// using UnityEngine;
// using UnityEngine.ResourceManagement.AsyncOperations;
//
// namespace PowerCellStudio
// {
//     [Serializable]
//     public class AssetsBundleLocalizationAssetTable: ScriptableObject
//     {
//         public List<AssetsBundleLocalizationAssetTableItem> items;
//         
//         [NonSerialized]
//         private Dictionary<string, AssetsBundleLocalizationAssetTableItem> _mapItems;
//
//         public static IEnumerator Load(AssetsBundleLocalizationAssetTable table)
//         {
//             var path = Path.Combine(ConstSetting.LocalizationAssetConfigFolder,
//                 Path.GetFileNameWithoutExtension(ConstSetting.LocalizationAssetConfigName));
//             var resourceRequest = Resources.LoadAsync<AssetsBundleLocalizationAssetTable>(path);
//             yield return resourceRequest;
//             table = resourceRequest.asset as AssetsBundleLocalizationAssetTable;
//             if (table != null) 
//                 table.Init();
//         }
//
//         public void Init()
//         {
//             Map();
//         }
//         
//         private void Map()
//         {
//             if (_mapItems != null) return;
//             _mapItems = new Dictionary<string, AssetsBundleLocalizationAssetTableItem>();
//             foreach (var item in items)
//             {
//                 _mapItems.TryAdd(item.defaultPath, item);
//             }
//         }
//
//         public bool TryGetLocalizationKey(string path, out string key)
//         {
//             _mapItems.TryGetValue(path, out AssetsBundleLocalizationAssetTableItem item);
//             if (item == null)
//             {
//                 key = string.Empty;
//                 return false;
//             }
//             key = item.key;
//             return !string.IsNullOrEmpty(item.key);
//         }
//
//         public bool TryLoadLocalizationAsset<T>(string defaultPath, Action<T> onLoaded)
//             where T : UnityEngine.Object
//         {
//             _mapItems.TryGetValue(defaultPath, out AssetsBundleLocalizationAssetTableItem item);
//             if (item == null)
//             {
//                 return false;
//             }
//             var key = item.key;
//             if (string.IsNullOrEmpty(key))
//                 return false;
//             var handler = LocalizationManager.instance.GetAssetAsync<T>(key);
//             handler.Completed += (handle) =>
//             {
//                 if (handle.Status != AsyncOperationStatus.Succeeded)
//                 {
//                     AssetLog.LogError($"Localization key:{key} load asset no found! Default path = {defaultPath}");
//                     return;
//                 }
//                 onLoaded?.Invoke(handle.Result);
//             };
//             return true;
//         }
//         
//     }
//
//     [Serializable]
//     public class AssetsBundleLocalizationAssetTableItem
//     {
//         public string key;
//         public string defaultPath;
//     }
// }