using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace PowerCellStudio
{
    public class AssetBundleIndex
    {
        private ChunkDataQueryer<string, ScriptableAssetBundleData> _assetPathMap;

        public static readonly string hasAssetBundleMapMovedKey = "hasAssetBundleMapMoved";
        
        public IEnumerator Init()
        { 
            var indexFilePath = Path.Combine(Application.persistentDataPath, ConstSetting.BundleAssetConfigFolder, $"{ConstSetting.BundleAssetConfigName}Index.bytes" );
            var dataFilePath = Path.Combine(Application.persistentDataPath, ConstSetting.BundleAssetConfigFolder, $"{ConstSetting.BundleAssetConfigName}Data.bytes" );
            // 创建目录
            var folder = Path.Combine(Application.persistentDataPath, ConstSetting.BundleAssetConfigFolder);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            // 初次启动，将streamingAssetsPath下的AssetBundleMap文件保存在Application.persistentDataPath
            var hasAssetBundleMapMoved = PlayerPrefs.GetInt(hasAssetBundleMapMovedKey, 0) > 0;
            if (!hasAssetBundleMapMoved)
            {
                var indexFilePathV0 =$"{Application.streamingAssetsPath}/{ConstSetting.BundleAssetConfigFolder}/{ConstSetting.BundleAssetConfigName}Index.bytes";
                var dataFilePathV0 =$"{Application.streamingAssetsPath}/{ConstSetting.BundleAssetConfigFolder}/{ConstSetting.BundleAssetConfigName}Data.bytes";
#if UNITY_ANDROID
                using (UnityWebRequest request = UnityWebRequest.Get("file://" + indexFilePathV0))
                {
                    request.downloadHandler = new DownloadHandlerFile(indexFilePath);
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        AssetLog.LogError(
                            "AssetsBundleManager initialization failed for coping AssetBundleMap file to persistentDataPath");
                        yield break;
                    }
                }
                using (UnityWebRequest request = UnityWebRequest.Get("file://" + dataFilePathV0))
                {
                    request.downloadHandler = new DownloadHandlerFile(dataFilePath);
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        AssetLog.LogError("AssetsBundleManager initialization failed for coping AssetBundleMap file to persistentDataPath");
                        yield break;
                    }
                }
#else
                File.WriteAllBytes(indexFilePath, File.ReadAllBytes(indexFilePathV0));
                File.WriteAllBytes(dataFilePath, File.ReadAllBytes(dataFilePathV0));
#endif
                PlayerPrefs.SetInt(hasAssetBundleMapMovedKey, 1);
                PlayerPrefs.Save();
            }
            
            // 从本地Application.persistentDataPath目录加载分包配置文件
            if (_assetPathMap == null)
                _assetPathMap = new ChunkDataQueryer<string, ScriptableAssetBundleData>();
            else _assetPathMap.Clear(null);
            yield return _assetPathMap.PrepareYieldInstruction(indexFilePath, dataFilePath, data => data.assetName);
        }

        public void ClearUnused()
        {
            if (_assetPathMap == null) return;
            _assetPathMap.TryClearUnused(null);
        }
        
        public string GetBundleNameByAsset(string path)
        {
            if (_assetPathMap == null) throw new Exception("AssetsBundleManager do not inited!!!");
            var assetData = _assetPathMap.Get(path, null);
            if (assetData == default)
            {
                AssetLog.LogError($"Can not find Bundle Name of [{path}]");
                return string.Empty;
            }
            return assetData.assetBundle;
        }
    }
}