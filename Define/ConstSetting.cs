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
        /// 设计分辨率
        /// </summary>
        public static readonly int[] Resolution = new[] {360, 720, 1080, 1440, 2160};
        
        public static readonly Vector2Int DefaultResolution = new Vector2Int(1080, 1920);
        public static readonly Vector2Int DefaultUISize = new Vector2Int(1080, 1920);

        /// <summary>
        /// 默认语言
        /// </summary>
        public static readonly Language DefaultLanguage = Language.ChineseSimplified;
        
        public static readonly string FileEncryptionKey = "Catcatlittlecat";

        public static readonly string LocalizationStringTable = "ThiefHero";
        
        public static readonly string LocalizationAssetTable = "ThiefHeroAsset";
        
        /// <summary>
        /// 本地化资源配置文件夹
        /// </summary>
        public static readonly string LocalizationAssetConfigFolder = "AssetLocalizationData";

        /// <summary>
        /// 本地化资源配置文件名
        /// </summary>
        public static readonly string LocalizationAssetConfigName = "AssetLocalizationData.asset";

        public static readonly int MillionInt = 10000;
        
        public static readonly long MillionLong = 10000;
        
        public enum ConfigSaveMode
        {
            ScriptableObject,
            Json,
            Binary
        }
        
        public static readonly ConfigSaveMode ConfigConfigSaveMode = ConfigSaveMode.Binary;

        public static readonly Dictionary<Language, string> LanguageFont = new Dictionary<Language, string>()
        {
            { Language.ChineseSimplified, "Assets/UFlowFramework/Fonts/ZiHunBianTaoTi.ttf" },
            { Language.English, "Assets/UFlowFramework/Fonts/ZiHunBianTaoTi.ttf" },
            { Language.ChineseTraditional, "Assets/UFlowFramework/Fonts/AlibabaPuHuiTi-2-65-Medium.ttf" }
        };
    }
}