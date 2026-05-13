using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace PowerCellStudio
{
    public partial class ConfigManager: SingletonBase<ConfigManager>
    {
        public static string assetLabel = "configasset";

        private bool _inited = false;
        /// <summary>
        /// 初始化需要的配置表
        /// configData for Init.
        /// </summary>
        private ConfigGroup _initConfig;

        public IEnumerator Init(OnLoadCompleted onInitCompleted)
        {
            // TODO 从资源中加载所有的配置表二进制文件存放在$"{Application.persistentDataPath}/ConfigAsset/"目录下
            var saveKey = "ConfigFirstLoadComplete";
            var complete = PlayerPrefs.GetInt(saveKey, 0);
            if (complete == 0)
            {
                // TODO 首次加载
                // 创建目录
                var folder = Path.Combine(Application.persistentDataPath, "ConfigAsset");
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                // 将bundle中的文件拷贝到持久化目录
                var loader = AssetUtils.SpawnLoader("InitConfigLoader");
                var wait = new YieldInstructionCompletionSource<IList<TextAsset>>();
                loader.LoadAllAsync<TextAsset>(assetLabel, assets =>
                {
                    foreach (var asset in assets)
                    {
                        var assetPath = Path.Combine(folder, Path.GetFileNameWithoutExtension(asset.name) + ".bytes");
                        File.WriteAllBytes(assetPath, asset.bytes);
                    }
                    wait.SetResult(assets);
                    AssetUtils.DeSpawnLoader(loader);
                });
                yield return wait;
                PlayerPrefs.SetInt(saveKey, 1);
            }
            // // 你可以使用 ConfigGroup 加载多个配置数据；
            // // You can use ConfigGroup to load multiple configuration data;
            // _initConfig = new ConfigGroup<CommonConfigLoader>(_guidanceConf, _rolePropConf); //(_baseTypeSampleConf, _customTypeSampleConf);
            // _initConfig.onLoadCompleted += OnInitConfLoadCompleted;
            // if(onInitCompleted != null) _initConfig.onLoadCompleted += onInitCompleted;
            // yield return _initConfig.LoadAll();
        }

        private void OnInitConfLoadCompleted(AssetLoadStatus data)
        {
            switch (data)
            {
                case AssetLoadStatus.Loading:
                    break;
                case AssetLoadStatus.Unload:
                {
                    var sb = new StringBuilder();
                    foreach (var initConfigFailLoadConfig in _initConfig.failLoadConfigs)
                    {
                        sb.Append(initConfigFailLoadConfig);
                        sb.Append("\n");
                    }
                    ConfigLog.LogError($"Config Load Failed, Failed Configs: \n {sb}");
                    break;
                }
                default:
                    _inited = true;
                    ConfigLog.Log("Config Load successfully");
                    // Do something
                    // ...
                    break;
            }
        }
    }
}