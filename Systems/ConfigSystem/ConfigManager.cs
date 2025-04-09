using System.Collections;
using System.Text;

namespace PowerCellStudio
{
    public partial class ConfigManager: SingletonBase<ConfigManager>
    {
        private bool _inited = false;
        /// <summary>
        /// 初始化需要的配置表
        /// configData for Init.
        /// </summary>
        private ConfigGroup<CommonConfigLoader> _initConfig;

        public IEnumerator Init(OnLoadCompleted onInitCompleted)
        {
            // 你可以使用 ConfigGroup 加载多个配置数据；
            // You can use ConfigGroup to load multiple configuration data;
            // _initConfig = new ConfigGroup<CommonConfigLoader>(_guidanceConf, _rolePropConf); //(_baseTypeSampleConf, _customTypeSampleConf);
            // _initConfig.onLoadCompleted += OnInitConfLoadCompleted;
            // if(onInitCompleted != null) _initConfig.onLoadCompleted += onInitCompleted;
            // yield return _initConfig.LoadAll();
            yield return null;
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