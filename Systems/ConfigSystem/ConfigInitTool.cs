using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

namespace PowerCellStudio
{
    public static class ConfigInitTool
    {
        public static string assetFolderPath => $"{Application.streamingAssetsPath}/ConfigAsset/";
        public static string configAssetListName => "ConfigAssetList.txt";

        // private bool _inited = false;
        // /// <summary>
        // /// 初始化需要的配置表，会一直存在在内存中，直到游戏结束。
        // /// Init config tables that are needed and will exist in memory until the end of the game
        // /// </summary>
        // private ConfigGroup _initConfig;

        public static IEnumerator CopyConfigToPersistentDataPath()
        {
#if UNITY_EDITOR
            yield break;
#endif
            var saveKey = "ConfigFirstLoadComplete";
            var complete = PlayerPrefs.GetInt(saveKey, 0);
            if (complete > 0) // 已经完成过复制，直接返回
                yield break;

            // 从资源中加载所有的配置表二进制文件存放在$"{Application.persistentDataPath}/ConfigAsset/"目录下
            var folder = Path.Combine(Application.persistentDataPath, "ConfigAsset");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                // WebGL平台无法直接访问文件系统，暂不支持从资源中加载配置表二进制文件
                Debug.LogError("WebGL platform does not support loading config asset from resources.");
                yield break;
            }
            if (Application.platform == RuntimePlatform.Android)
            {
                var listFilePath = $"file://{assetFolderPath}{configAssetListName}";
                using var wepRequest = UnityEngine.Networking.UnityWebRequest.Get(listFilePath);
                wepRequest.downloadHandler = new UnityEngine.Networking.DownloadHandlerFile(Path.Combine(folder, configAssetListName));
                var asyncOp = wepRequest.SendWebRequest();
                yield return asyncOp;
                if (wepRequest.result != UnityEngine.Networking.UnityWebRequest.Result.Success)                {
                    Debug.LogError($"Failed to copy config asset list file from {listFilePath} to {folder}, error: {wepRequest.error}");
                    yield break;
                }
                var listFileContent = File.ReadAllText(Path.Combine(folder, configAssetListName), Encoding.UTF8);
                var assetFiles = listFileContent.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                wepRequest.Dispose();
                foreach (var assetFile in assetFiles)
                {
                    var assetPath = $"file://{assetFolderPath}{assetFile}";
                    var destPath = Path.Combine(folder, assetFile);
                    using var assetRequest = UnityEngine.Networking.UnityWebRequest.Get(assetPath);
                    assetRequest.downloadHandler = new UnityEngine.Networking.DownloadHandlerFile(destPath);
                    var assetAsyncOp = assetRequest.SendWebRequest();
                    yield return assetAsyncOp;
                    if (assetRequest.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"Failed to copy config asset file from {assetPath} to {destPath}, error: {assetRequest.error}");
                    }
                    assetRequest.Dispose();
                }
            }
            else
            {
                var listFilePath = Path.Combine(assetFolderPath, configAssetListName);
                // 其他平台可以直接访问文件系统
                var listFileContent = File.ReadAllText(listFilePath, Encoding.UTF8);
                var assetFiles = listFileContent.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                foreach (var assetFile in assetFiles)
                {
                    var assetPath = Path.Combine(assetFolderPath, assetFile);
                    var destPath = Path.Combine(folder, assetFile);
                    File.Copy(assetPath, destPath, true);
                }
            }

            PlayerPrefs.SetInt(saveKey, 1);
        }

        // public IEnumerator Init(OnLoadCompleted onInitCompleted)
        // {
        //     // 编辑器下直接assetFolderPath中加载配置表二进制文件，无需拷贝到持久化目录
        //     yield return null;
        //     // // 你可以使用 ConfigGroup 加载多个配置数据；
        //     // // You can use ConfigGroup to load multiple configuration data;
        //     // _initConfig = new ConfigGroup<CommonConfigLoader>(_guidanceConf, _rolePropConf); //(_baseTypeSampleConf, _customTypeSampleConf);
        //     // _initConfig.onLoadCompleted += OnInitConfLoadCompleted;
        //     // if(onInitCompleted != null) _initConfig.onLoadCompleted += onInitCompleted;
        //     // yield return _initConfig.LoadAll();
        // }
        //
        // private void OnInitConfLoadCompleted(AssetLoadStatus data)
        // {
        //     switch (data)
        //     {
        //         case AssetLoadStatus.Loading:
        //             break;
        //         case AssetLoadStatus.Unload:
        //         {
        //             var sb = new StringBuilder();
        //             foreach (var initConfigFailLoadConfig in _initConfig.failLoadConfigs)
        //             {
        //                 sb.Append(initConfigFailLoadConfig);
        //                 sb.Append("\n");
        //             }
        //             ConfigLogger.LogError($"Config Load Failed, Failed Configs: \n {sb}");
        //             break;
        //         }
        //         default:
        //             _inited = true;
        //             ConfigLogger.Log("Config Load successfully");
        //             // Do something
        //             // ...
        //             break;
        //     }
        // }
    }
}