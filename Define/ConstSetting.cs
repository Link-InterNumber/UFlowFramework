using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PowerCellStudio
{
    public class ConstSetting
    {
        /// <summary>
        /// 路径分隔符
        /// </summary>
        public static readonly char[] PathSeparator = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        /// <summary>
        /// 分包配置文件夹
        /// </summary>
        public static readonly string BundleAssetConfigFolder = "AssetBundleData";

        /// <summary>
        /// 分包配置文件名
        /// </summary>
        public static readonly string BundleAssetConfigName = "AssetBundleData.asset";

        /// <summary>
        /// 设计分辨率范围
        /// </summary>
        public static readonly int[] Resolution = new[] {360, 720, 1080, 1440, 2160};
        
        /// <summary>
        /// 默认分辨率
        /// </summary>
        public static readonly Vector2Int DefaultResolution = new Vector2Int(1080, 1920);

        /// <summary>
        /// 设计UI画布尺寸
        /// </summary>
        public static readonly Vector2Int DefaultUISize = new Vector2Int(1080, 1920);

        public static readonly string FileEncryptionKey = "Catcatlittlecat";

        /// <summary>
        /// 默认语言
        /// </summary>
        public static readonly Language DefaultLanguage = Language.ChineseSimplified;

        /// <summary>
        /// 本地化字符串表
        /// </summary>
        public static readonly string LocalizationStringTable = "ThiefHero";
        
        /// <summary>
        /// 本地化资源表
        /// </summary>
        public static readonly string LocalizationAssetTable = "ThiefHeroAsset";

        /// <summary>
        /// 本地化语言对应字体
        /// </summary>
        public static readonly Dictionary<Language, string> LanguageFont = new Dictionary<Language, string>()
        {
            { Language.ChineseSimplified, "Assets/UFlowFramework/Fonts/ZiHunBianTaoTi.ttf"},
            { Language.English, "Assets/UFlowFramework/Fonts/ZiHunBianTaoTi.ttf" },
            { Language.ChineseTraditional, "Assets/UFlowFramework/Fonts/AlibabaPuHuiTi-2-85-Bold.ttf"}
        };

        /// <summary>
        /// 万分比整数基数
        /// </summary>
        public static readonly int MillionInt = 10000;
        
        /// <summary>
        /// 万分比长整数基数
        /// </summary>
        public static readonly long MillionLong = 10000;
        
        public enum ConfigSaveMode
        {
            ScriptableObject,
            Json,
            Binary
        }
        
        /// <summary>
        /// 配置表保存方式
        /// </summary>
        public static readonly ConfigSaveMode ConfigConfigSaveMode = ConfigSaveMode.Binary;
    }
}