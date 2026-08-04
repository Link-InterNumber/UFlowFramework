using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace PowerCellStudio
{
    public class ConstSetting
    {
        /// <summary>
        /// 路径分隔符
        /// <para>Path separator characters</para>
        /// </summary>
        public static readonly char[] PathSeparator = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        /// <summary>
        /// 分包配置文件夹
        /// <para>Subpackage configuration folder</para>
        /// </summary>
        public static readonly string BundleAssetConfigFolder = "AssetBundleData";

        /// <summary>
        /// 分包配置文件名
        /// <para>Subpackage configuration file name</para>
        /// </summary>
        public static readonly string BundleAssetConfigName = "AssetBundleData";

        /// <summary>
        /// 设计分辨率范围
        /// <para>Design resolution range</para>
        /// </summary>
        public static readonly int[] Resolution = new[] { 360, 720, 1080, 1440, 2160 };

        /// <summary>
        /// 默认分辨率
        /// <para>Default resolution</para>
        /// </summary>
        public static readonly ResolutionLv DefaultResolutionLv = ResolutionLv.High;

        /// <summary>
        /// 设计UI画布尺寸，(宽,高)
        /// <para>Default UI canvas size (width, height)</para>
        /// </summary>
        public static readonly Vector2Int DefaultUISize = new Vector2Int(1920, 1080);

        /// <summary>
        /// 文件加密密钥
        /// <para>File encryption key</para>
        /// </summary>
        public static readonly string FileEncryptionKey = "Catcatlittlecat";

        /// <summary>
        /// 默认语言
        /// <para>Default language</para>
        /// </summary>
        public static readonly Language DefaultLanguage = Language.ChineseSimplified;

        /// <summary>
        /// 本地化字符串表
        /// <para>Localization string table</para>
        /// </summary>
        public static readonly string LocalizationStringTable = "ThiefHero";

        /// <summary>
        /// 本地化资源表
        /// <para>Localization asset table</para>
        /// </summary>
        public static readonly string LocalizationAssetTable = "ThiefHeroAsset";

        /// <summary>
        /// 本地化语言对应字体
        /// <para>Font mapping for each localization language</para>
        /// </summary>
        public static readonly Dictionary<Language, string> LanguageFont = new Dictionary<Language, string>()
        {
            { Language.ChineseSimplified, "Assets/UFlowFramework/Fonts/ZiHunBianTaoTi.ttf" },
            { Language.English, "Assets/UFlowFramework/Fonts/ZiHunBianTaoTi.ttf" },
            { Language.ChineseTraditional, "Assets/UFlowFramework/Fonts/AlibabaPuHuiTi-2-85-Bold.ttf" },
            { Language.Japanese, "Assets/UFlowFramework/Fonts/AlibabaPuHuiTi-2-85-Bold.ttf" },
        };
        
        public static readonly Dictionary<Language, string> LanguageTMPFont = new Dictionary<Language, string>()
        {
            { Language.ChineseSimplified, "Assets/UFlowFramework/Fonts/ZiHunBianTaoTiSDF.asset" },
            { Language.English, "Assets/UFlowFramework/Fonts/ZiHunBianTaoTiSDF.asset" },
            { Language.ChineseTraditional, "Assets/UFlowFramework/Fonts/ZiHunBianTaoTiSDF.asset" },
            { Language.Japanese, "Assets/UFlowFramework/Fonts/AlibabaPuHuiTi_2_85_Bold_SDF.asset" },
        };

        // /// <summary>
        // /// 配置保存模式
        // /// <para>Configuration save mode</para>
        // /// </summary>
        // public enum ConfigSaveMode { ScriptableObject, Json, Binary }

        // /// <summary>
        // /// 配置保存模式（默认二进制）
        // /// <para>Configuration save mode (default: Binary)</para>
        // /// </summary>
        // public static readonly ConfigSaveMode ConfigConfigSaveMode = ConfigSaveMode.Binary;
    }
}