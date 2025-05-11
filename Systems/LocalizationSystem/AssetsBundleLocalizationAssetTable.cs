using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PowerCellStudio
{
    [Serializable]
    public class AssetsBundleLocalizationAssetTable: ScriptableObject
    {
        public List<AssetsBundleLocalizationAssetTableItem> items;
        
        [NonSerialized]
        private Dictionary<string, AssetsBundleLocalizationAssetTableItem> _mapItems;

        // 在Resources文件夹下创建一个AssetsBundleLocalizationAssetTable.asset文件
        [MenuItem("Tools/AssetsBundleLocalizationAssetTable", false, 2)]
        public static void Create()
        {
            // 加载Localization的AssetTable，文件名ConstSetting.LocalizationAssetTable
            var handler = LocalizationSettings.AssetDatabase.GetAllTables();
            handler.Completed += OnLoadedAllTables;

        }

        private static void OnLoadedAllTables(AsyncOperationHandle<IList<AssetTable>> obj)
        {
            var assetTables = obj.Result;
            if (assetTables == null)
            {
                AssetLog.LogError($"Can not find asset table: {ConstSetting.LocalizationAssetTable}");
                return;
            }
            Debug.LogWarning(assetTables.Count);
            var tableItems = new Dictionary<string, AssetsBundleLocalizationAssetTableItem>();
            var length = Enum.GetNames(typeof(Language)).Length;
            foreach (var table in assetTables)
            {
                Debug.LogWarning(table.LocaleIdentifier.ToString());

                // foreach (var keyNEntry in table)
                // {
                    // var assetPath = AssetDatabase.GUIDToAssetPath(keyNEntry.Value.Address);
                    // if (string.IsNullOrEmpty(assetPath))
                    // {
                    //     AssetLog.LogError($"Can not find asset path: {keyNEntry.Value.Address}");
                    //     continue;
                    // }
                    //
                    // var key = keyNEntry.Value.Address;
                    // if (tableItems.ContainsKey(key))
                    // {
                    //
                    // }
                    // else
                    // {
                    //     var item = new AssetsBundleLocalizationAssetTableItem
                    //     {
                    //         key = keyNEntry.Value.Address,
                    //         guid = new string[length],
                    //         path = new string[length]
                    //     };
                    //     item.guid[(int)table.LocaleIdentifier.] = keyNEntry.Value.Address;
                    //     item.path[(int)keyNEntry.Value.LocaleIdentifier] = assetPath;
                    // }

                    
                // }

            }
        }

        public void Init()
        {
            Map();
        }
        
        private void Map()
        {
            if (_mapItems != null) return;
            _mapItems = new Dictionary<string, AssetsBundleLocalizationAssetTableItem>();
            foreach (var item in items)
            {
                _mapItems.TryAdd(item.key, item);
            }
        }

        public string GetAssetPath(string key, Language language)
        {
            _mapItems.TryGetValue(key, out AssetsBundleLocalizationAssetTableItem item);
            if (item == null)
            {
                AssetLog.LogError($"Can not find asset table item: {key}");
                return string.Empty;
            }
            if (item.path.Length == 0)
            {
                AssetLog.LogError($"Asset table item path is empty: {key}");
                return string.Empty;
            }
            var index = (int)language;
            if (index < 0 || index >= item.path.Length)
            {
                AssetLog.LogError($"Asset table item path index out of range: {key}");
                return string.Empty;
            }
            var path = item.path[index];
            return path;
        }

        public string GetAssetGuid(string key, Language language)
        {
            _mapItems.TryGetValue(key, out AssetsBundleLocalizationAssetTableItem item);
            if (item == null)
            {
                AssetLog.LogError($"Can not find asset table item: {key}");
                return string.Empty;
            }
            if (item.guid.Length == 0)
            {
                AssetLog.LogError($"Asset table item guid is empty: {key}");
                return string.Empty;
            }
            var index = (int)language;
            if (index < 0 || index >= item.guid.Length)
            {
                AssetLog.LogError($"Asset table item guid index out of range: {key}");
                return string.Empty;
            }
            var guid = item.guid[index];
            return guid;
        }
    }

    [Serializable]
    public class AssetsBundleLocalizationAssetTableItem
    {
        public string key;
        public string[] guid;
        public string[] path;
    }
}